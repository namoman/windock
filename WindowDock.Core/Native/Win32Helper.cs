using System.Runtime.InteropServices;
using System.Text;
using WindowDock.Core.Models;

namespace WindowDock.Core.Native;

internal static class NativeMethods
{
    public const int GwlHwndParent = -8;
    public const uint WmGetIcon = 0x007F;
    public const int IconSmall = 0;
    public const int IconBig = 1;

    public const int SwHide = 0;
    public const int SwShow = 5;
    public const int SwMinimize = 6;
    public const int SwRestore = 9;

    public const uint SwpShowWindow = 0x0040;
    public static readonly IntPtr HwndTop = new(0);

    public const uint GwOwner = 4;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}

public static class Win32Helper
{
    public static bool IsWindowAlive(IntPtr hwnd) =>
        hwnd != IntPtr.Zero && NativeMethods.IsWindow(hwnd);

    public static string GetWindowTitle(IntPtr hwnd)
    {
        var length = NativeMethods.GetWindowTextLength(hwnd);
        if (length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    public static string GetClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        _ = NativeMethods.GetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    public static WindowRect GetWindowRect(IntPtr hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            return default;
        }

        return new WindowRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public static IntPtr GetWindowParent(IntPtr hwnd)
    {
        var parent = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlHwndParent);
        return parent == IntPtr.Zero ? IntPtr.Zero : parent;
    }

    public static bool SetWindowParent(IntPtr hwnd, IntPtr parent)
    {
        _ = NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlHwndParent, parent);
        return Marshal.GetLastWin32Error() == 0 || NativeMethods.IsWindow(hwnd);
    }

    public static bool ShowWindow(IntPtr hwnd, int command) =>
        NativeMethods.ShowWindow(hwnd, command);

    public static bool SetForeground(IntPtr hwnd) =>
        NativeMethods.SetForegroundWindow(hwnd);

    public static bool SetWindowBounds(IntPtr hwnd, WindowRect rect) =>
        NativeMethods.SetWindowPos(
            hwnd,
            NativeMethods.HwndTop,
            rect.Left,
            rect.Top,
            rect.Width,
            rect.Height,
            NativeMethods.SwpShowWindow);

    public static int GetWindowProcessId(IntPtr hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return (int)processId;
    }

    public static IntPtr GetWindowIcon(IntPtr hwnd, bool large)
    {
        var icon = NativeMethods.SendMessage(
            hwnd,
            NativeMethods.WmGetIcon,
            new IntPtr(large ? NativeMethods.IconBig : NativeMethods.IconSmall),
            IntPtr.Zero);

        if (icon != IntPtr.Zero)
        {
            return icon;
        }

        icon = NativeMethods.SendMessage(
            hwnd,
            NativeMethods.WmGetIcon,
            new IntPtr(NativeMethods.IconSmall),
            new IntPtr(1));

        return icon;
    }

    public static bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey) =>
        NativeMethods.RegisterHotKey(hwnd, id, modifiers, virtualKey);

    public static bool UnregisterHotKey(IntPtr hwnd, int id) =>
        NativeMethods.UnregisterHotKey(hwnd, id);
}
