namespace WindowDock.Core.Models;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool HideTaskbarWhileActive { get; set; }
    public int AnimationSpeed { get; set; } = 1;
    public int DockOpacity { get; set; } = 60;
}
