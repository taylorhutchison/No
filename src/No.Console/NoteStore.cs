using System.Text.Json;

namespace No;

internal sealed class NoteStore
{
    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".no", "notes.json");

    public List<Note> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        using var stream = File.OpenRead(FilePath);
        return JsonSerializer.Deserialize(stream, NoteJsonContext.Default.ListNote) ?? [];
    }

    public void Save(List<Note> notes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        // Write then move so a crash never leaves a half-written file.
        var temp = FilePath + ".tmp";
        using (var stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, notes, NoteJsonContext.Default.ListNote);
        }

        File.Move(temp, FilePath, overwrite: true);
    }
}
