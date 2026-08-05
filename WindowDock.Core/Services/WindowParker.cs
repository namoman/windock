using WindowDock.Core.Models;
using WindowDock.Core.Native;

namespace WindowDock.Core.Services;

public sealed class WindowParker
{
    private readonly List<ParkedWindowInfo> _parkedWindows = [];
    private IntPtr _ownerHwnd = IntPtr.Zero;
    private readonly object _sync = new();

    public event EventHandler? ParkedWindowsChanged;

    public void SetOwnerWindow(IntPtr ownerHwnd)
    {
        _ownerHwnd = ownerHwnd;
    }

    public IReadOnlyList<ParkedWindowInfo> GetParkedWindows()
    {
        lock (_sync)
        {
            return _parkedWindows.ToList();
        }
    }

    public ParkResult Park(IntPtr hwnd)
    {
        if (!Win32Helper.IsWindowAlive(hwnd))
        {
            return new ParkResult
            {
                Kind = ParkResultKind.InvalidWindow,
                Message = "유효하지 않은 창입니다."
            };
        }

        var processId = Win32Helper.GetWindowProcessId(hwnd);
        if (processId == System.Diagnostics.Process.GetCurrentProcess().Id)
        {
            return new ParkResult
            {
                Kind = ParkResultKind.InvalidWindow,
                Message = "Windock 자체 창은 보관할 수 없습니다."
            };
        }

        lock (_sync)
        {
            if (_parkedWindows.Any(w => w.Handle == hwnd && w.IsParked))
            {
                return new ParkResult
                {
                    Kind = ParkResultKind.AlreadyParked,
                    Message = "이미 보관 중인 창입니다."
                };
            }
        }

        var windowInfo = BuildParkedInfo(hwnd);

        if (_ownerHwnd == IntPtr.Zero)
        {
            return new ParkResult
            {
                Kind = ParkResultKind.Failed,
                Message = "Owner 창이 초기화되지 않았습니다."
            };
        }

        var originalParent = Win32Helper.GetWindowParent(hwnd);
        var parkedWithOwner = TryParkWithOwner(hwnd, windowInfo, originalParent);
        if (parkedWithOwner != null)
        {
            AddParkedWindow(parkedWithOwner);
            return new ParkResult
            {
                Kind = ParkResultKind.Success,
                Window = parkedWithOwner
            };
        }

        var minimized = TryMinimizeFallback(hwnd, windowInfo, originalParent);
        if (minimized != null)
        {
            AddParkedWindow(minimized);
            return new ParkResult
            {
                Kind = ParkResultKind.MinimizedFallback,
                Message = "작업표시줄 숨김에 실패하여 최소화로 보관했습니다.",
                Window = minimized
            };
        }

        return new ParkResult
        {
            Kind = ParkResultKind.Failed,
            Message = "창을 보관할 수 없습니다. 관리자 권한이 필요할 수 있습니다."
        };
    }

    public bool Restore(IntPtr hwnd)
    {
        ParkedWindowInfo? parked;
        lock (_sync)
        {
            parked = _parkedWindows.FirstOrDefault(w => w.Handle == hwnd);
            if (parked is null)
            {
                return false;
            }
        }

        if (!Win32Helper.IsWindowAlive(hwnd))
        {
            RemoveParkedWindow(hwnd);
            return false;
        }

        var restored = false;

        if (parked.IsParked && !parked.UsedMinimizeFallback)
        {
            Win32Helper.SetWindowParent(hwnd, parked.OriginalParent);
            Win32Helper.SetWindowBounds(hwnd, parked.Rect);
            restored = Win32Helper.ShowWindow(hwnd, NativeMethods.SwRestore)
                || Win32Helper.ShowWindow(hwnd, NativeMethods.SwShow);
        }
        else if (parked.UsedMinimizeFallback)
        {
            Win32Helper.SetWindowBounds(hwnd, parked.Rect);
            restored = Win32Helper.ShowWindow(hwnd, NativeMethods.SwRestore);
        }

        if (restored)
        {
            Win32Helper.SetForeground(hwnd);
        }

        RemoveParkedWindow(hwnd);
        return restored;
    }

    public void RefreshAliveStatus()
    {
        List<ParkedWindowInfo> stale;
        lock (_sync)
        {
            stale = _parkedWindows.Where(w => !Win32Helper.IsWindowAlive(w.Handle)).ToList();
            foreach (var item in stale)
            {
                _parkedWindows.Remove(item);
            }
        }

        if (stale.Count > 0)
        {
            OnParkedWindowsChanged();
        }
    }

    public void RestoreAll()
    {
        foreach (var parked in GetParkedWindows())
        {
            Restore(parked.Handle);
        }
    }

    /// <summary>
    /// 이미 숨겨진 창을 현재 Owner 아래로 다시 붙여 숨김 목록에 올립니다.
    /// </summary>
    public bool AdoptHidden(ParkedWindowInfo info)
    {
        if (_ownerHwnd == IntPtr.Zero || !Win32Helper.IsWindowAlive(info.Handle))
        {
            return false;
        }

        lock (_sync)
        {
            if (_parkedWindows.Any(w => w.Handle == info.Handle && w.IsParked))
            {
                return true;
            }
        }

        if (info.UsedMinimizeFallback)
        {
            info.IsParked = true;
            AddParkedWindow(info);
            return true;
        }

        var originalParent = Win32Helper.GetWindowParent(info.Handle);
        if (!Win32Helper.SetWindowParent(info.Handle, _ownerHwnd))
        {
            return false;
        }

        _ = Win32Helper.ShowWindow(info.Handle, NativeMethods.SwHide);
        info.OriginalParent = originalParent == _ownerHwnd ? IntPtr.Zero : originalParent;
        info.IsParked = true;
        info.UsedMinimizeFallback = false;
        AddParkedWindow(info);
        return true;
    }

    public ParkResult ParkForegroundWindow(WindowEnumerator enumerator)
    {
        var hwnd = enumerator.GetForegroundWindowHandle();
        return Park(hwnd);
    }

    private static ParkedWindowInfo BuildParkedInfo(IntPtr handle)
    {
        var processId = Win32Helper.GetWindowProcessId(handle);
        return new ParkedWindowInfo
        {
            Handle = handle,
            Title = Win32Helper.GetWindowTitle(handle),
            ProcessName = GetProcessName(processId),
            ProcessId = processId,
            Rect = Win32Helper.GetWindowRect(handle),
            OriginalParent = Win32Helper.GetWindowParent(handle),
            ParkedAt = DateTime.Now,
            IsParked = false,
            UsedMinimizeFallback = false
        };
    }

    private ParkedWindowInfo? TryParkWithOwner(
        IntPtr handle,
        ParkedWindowInfo info,
        IntPtr originalParent)
    {
        if (!Win32Helper.SetWindowParent(handle, _ownerHwnd))
        {
            return null;
        }

        if (!Win32Helper.ShowWindow(handle, NativeMethods.SwHide))
        {
            Win32Helper.SetWindowParent(handle, originalParent);
            return null;
        }

        info.OriginalParent = originalParent;
        info.IsParked = true;
        info.UsedMinimizeFallback = false;
        return info;
    }

    private static ParkedWindowInfo? TryMinimizeFallback(
        IntPtr handle,
        ParkedWindowInfo info,
        IntPtr originalParent)
    {
        if (!Win32Helper.ShowWindow(handle, NativeMethods.SwMinimize))
        {
            return null;
        }

        info.OriginalParent = originalParent;
        info.IsParked = true;
        info.UsedMinimizeFallback = true;
        return info;
    }

    private void AddParkedWindow(ParkedWindowInfo info)
    {
        lock (_sync)
        {
            _parkedWindows.RemoveAll(w => w.Handle == info.Handle);
            _parkedWindows.Insert(0, info);
        }

        OnParkedWindowsChanged();
    }

    private void RemoveParkedWindow(IntPtr hwnd)
    {
        lock (_sync)
        {
            _parkedWindows.RemoveAll(w => w.Handle == hwnd);
        }

        OnParkedWindowsChanged();
    }

    private void OnParkedWindowsChanged() =>
        ParkedWindowsChanged?.Invoke(this, EventArgs.Empty);

    private static string GetProcessName(int processId)
    {
        try
        {
            return System.Diagnostics.Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            return $"pid:{processId}";
        }
    }
}
