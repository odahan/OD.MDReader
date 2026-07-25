using System.Text.Json;
using System.IO;

namespace MDRead;

internal sealed class UserSettings
{
    public string? LastDirectory { get; set; }
    public List<string> RecentFiles { get; set; } = [];
    public double EditorHeight { get; set; }
    public bool IsDark { get; set; } = true;
    public bool EditMode { get; set; }

    private static string PathName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Olivier Dahan", "MDRead", "settings.json");

    public static UserSettings Load()
    {
        try { return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(PathName)) ?? new UserSettings(); }
        catch { return new UserSettings(); }
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(PathName)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(PathName, JsonSerializer.Serialize(this));
        }
        catch { /* Persisting settings is best-effort; never crash the app over it. */ }
    }
}
