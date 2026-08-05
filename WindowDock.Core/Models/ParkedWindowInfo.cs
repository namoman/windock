namespace WindowDock.Core.Models;

public sealed class ParkedWindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public WindowRect Rect { get; set; }
    public IntPtr OriginalParent { get; set; }
    public DateTime ParkedAt { get; set; }
    public bool IsParked { get; set; }
    public bool UsedMinimizeFallback { get; set; }

}
