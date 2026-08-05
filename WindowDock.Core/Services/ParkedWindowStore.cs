using System.Text.Json;
using WindowDock.Core.Models;

namespace WindowDock.Core.Services;

public sealed class ParkedWindowStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _storePath;

    public ParkedWindowStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "Windock");
        Directory.CreateDirectory(directory);
        _storePath = Path.Combine(directory, "parked-windows.json");
    }

    public void SaveMetadata(IEnumerable<ParkedWindowInfo> windows)
    {
        var snapshot = windows
            .Select(w => new ParkedWindowSnapshot
            {
                Title = w.Title,
                ProcessName = w.ProcessName,
                ProcessId = w.ProcessId,
                Rect = w.Rect,
                ParkedAt = w.ParkedAt,
                UsedMinimizeFallback = w.UsedMinimizeFallback
            })
            .ToList();

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        File.WriteAllText(_storePath, json);
    }

    public IReadOnlyList<ParkedWindowSnapshot> LoadMetadata()
    {
        if (!File.Exists(_storePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_storePath);
            return JsonSerializer.Deserialize<List<ParkedWindowSnapshot>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public sealed class ParkedWindowSnapshot
    {
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public WindowRect Rect { get; set; }
        public DateTime ParkedAt { get; set; }
        public bool UsedMinimizeFallback { get; set; }
    }
}
