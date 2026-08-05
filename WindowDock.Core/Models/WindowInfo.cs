namespace WindowDock.Core.Models;

public sealed class WindowInfo
{
    public required IntPtr Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required int ProcessId { get; init; }
    public required string ClassName { get; init; }
    public required WindowRect Rect { get; init; }
    public required bool IsVisible { get; init; }
}
