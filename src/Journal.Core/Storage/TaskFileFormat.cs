using System.Text.Json;
using Journal.Core.Models;

namespace Journal.Core.Storage;

/// <summary>Reads and writes the JSON stored in a journal's .tsk file.</summary>
public static class TaskFileFormat
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(IEnumerable<JournalTask> tasks)
    {
        return JsonSerializer.Serialize(new TaskFile { Tasks = tasks.ToList() }, Options);
    }

    public static List<JournalTask> Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var file = JsonSerializer.Deserialize<TaskFile>(json, Options);
            return file?.Tasks ?? [];
        }
        catch (JsonException exception)
        {
            throw new JournalException($"The task file is not valid: {exception.Message}");
        }
    }

    private sealed class TaskFile
    {
        public List<JournalTask> Tasks { get; set; } = [];
    }
}
