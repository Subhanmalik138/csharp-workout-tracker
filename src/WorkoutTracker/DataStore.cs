using System.Text.Json;

namespace WorkoutTracker;

public interface IWorkoutStore
{
    WorkoutData Load();
    void Save(WorkoutData data);
}

/// <summary>
/// JSON-file store with atomic writes: the document is written to a temp file
/// in the same directory and then moved over the real file, so a crash
/// mid-write can never leave a half-written database behind.
/// </summary>
public sealed class JsonWorkoutStore : IWorkoutStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public JsonWorkoutStore(string path) => _path = path;

    public WorkoutData Load()
    {
        if (!File.Exists(_path))
            return new WorkoutData();
        return JsonSerializer.Deserialize<WorkoutData>(File.ReadAllText(_path)) ?? new WorkoutData();
    }

    public void Save(WorkoutData data)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(data, Options));
        File.Move(tmp, _path, overwrite: true);
    }
}
