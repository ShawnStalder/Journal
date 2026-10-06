using System.Text.Json;

namespace Journal.Core.State;

public sealed class AppSettings
{
    public bool IsDarkTheme { get; set; } = true;

    /// <summary>True once the app has offered start-with-Windows, so a later opt-out is respected.</summary>
    public bool HasConfiguredAutoStart { get; set; }
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public SettingsStore(string stateFolder)
    {
        _path = Path.Combine(stateFolder, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
    }
}
