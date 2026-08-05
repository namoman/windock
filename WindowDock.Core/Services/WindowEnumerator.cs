using System.Diagnostics;
using WindowDock.Core.Models;
using WindowDock.Core.Native;

namespace WindowDock.Core.Services;

public sealed class WindowEnumerator
{
    private static readonly HashSet<string> ExcludedClassNames = new(StringComparer.Ordinal)
    {
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Progman",
        "WorkerW",
        "Button",
        "DV2ControlHost",
        "MsgrIMEWindowClass",
        "IME",
        "Windows.UI.Core.CoreWindow"
    };

    public IReadOnlyList<WindowInfo> GetOpenWindows(IntPtr excludeHwnd = default)
    {
        var windows = new List<WindowInfo>();
        var currentProcessId = Process.GetCurrentProcess().Id;

        NativeMethods.EnumWindows(
            (hwnd, _) =>
            {
                if (excludeHwnd != IntPtr.Zero && hwnd == excludeHwnd)
                {
                    return true;
                }

                if (!NativeMethods.IsWindowVisible(hwnd))
                {
                    return true;
                }

                var owner = NativeMethods.GetWindow(hwnd, NativeMethods.GwOwner);
                if (owner != IntPtr.Zero && NativeMethods.IsWindowVisible(owner))
                {
                    return true;
                }

                var title = Win32Helper.GetWindowTitle(hwnd);
                if (string.IsNullOrWhiteSpace(title))
                {
                    return true;
                }

                var className = Win32Helper.GetClassName(hwnd);
                if (ExcludedClassNames.Contains(className))
                {
                    return true;
                }

                var processId = Win32Helper.GetWindowProcessId(hwnd);
                if (processId == currentProcessId)
                {
                    return true;
                }

                windows.Add(new WindowInfo
                {
                    Handle = hwnd,
                    Title = title,
                    ProcessName = GetProcessName(processId),
                    ProcessId = processId,
                    ClassName = className,
                    Rect = Win32Helper.GetWindowRect(hwnd),
                    IsVisible = true
                });

                return true;
            },
            IntPtr.Zero);

        return windows
            .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public WindowInfo? TryGetWindowInfo(IntPtr hwnd)
    {
        if (!Win32Helper.IsWindowAlive(hwnd))
        {
            return null;
        }

        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            return null;
        }

        var title = Win32Helper.GetWindowTitle(hwnd);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var processId = Win32Helper.GetWindowProcessId(hwnd);
        return new WindowInfo
        {
            Handle = hwnd,
            Title = title,
            ProcessName = GetProcessName(processId),
            ProcessId = processId,
            ClassName = Win32Helper.GetClassName(hwnd),
            Rect = Win32Helper.GetWindowRect(hwnd),
            IsVisible = true
        };
    }

    /// <summary>
    /// 제목이 있는 숨김(비가시) 최상위 창을 열거합니다. 이전 세션에서 Park 후 앱이 종료된 고아 창 회수용.
    /// </summary>
    public IReadOnlyList<WindowInfo> GetHiddenTitledWindows()
    {
        var windows = new List<WindowInfo>();
        var currentProcessId = Process.GetCurrentProcess().Id;

        NativeMethods.EnumWindows(
            (hwnd, _) =>
            {
                if (!Win32Helper.IsWindowAlive(hwnd) || NativeMethods.IsWindowVisible(hwnd))
                {
                    return true;
                }

                var title = Win32Helper.GetWindowTitle(hwnd);
                if (string.IsNullOrWhiteSpace(title))
                {
                    return true;
                }

                var className = Win32Helper.GetClassName(hwnd);
                if (ExcludedClassNames.Contains(className))
                {
                    return true;
                }

                var processId = Win32Helper.GetWindowProcessId(hwnd);
                if (processId == currentProcessId)
                {
                    return true;
                }

                windows.Add(new WindowInfo
                {
                    Handle = hwnd,
                    Title = title,
                    ProcessName = GetProcessName(processId),
                    ProcessId = processId,
                    ClassName = className,
                    Rect = Win32Helper.GetWindowRect(hwnd),
                    IsVisible = false
                });

                return true;
            },
            IntPtr.Zero);

        return windows;
    }

    public IntPtr GetForegroundWindowHandle() =>
        NativeMethods.GetForegroundWindow();

    private static string GetProcessName(int processId)
    {
        try
        {
            return Process.GetProcessById(processId).ProcessName;
        }
        catch
        {
            return $"pid:{processId}";
        }
    }
}
