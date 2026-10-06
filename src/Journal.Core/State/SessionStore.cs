using System.Text.Json;

namespace Journal.Core.State;

/// <summary>Remembers which journals were open across a Restart.</summary>
public sealed class SessionStore
{
    private readonly string _path;

    public SessionStore(string stateFolder)
    {
        _path = Path.Combine(stateFolder, "session.json");
    }

    public void Save(IReadOnlyCollection<string> openJournalNames)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(openJournalNames));
    }

    /// <summary>Returns the saved journal names and clears them so they are only restored once.</summary>
    public IReadOnlyList<string> Take()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_path);
            File.Delete(_path);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
