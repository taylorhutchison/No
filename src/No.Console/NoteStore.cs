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
        var file = JsonSerializer.Deserialize(stream, NoteJsonContext.Default.NoteFile) ?? new NoteFile();
        foreach (var note in file.Archive)
        {
            note.Archived = true;
        }

        return [.. file.Notes, .. file.Archive];
    }

    public void Save(List<Note> notes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        // Write then move so a crash never leaves a half-written file.
        var temp = FilePath + ".tmp";
        using (var stream = File.Create(temp))
        {
            var file = new NoteFile
            {
                Notes = [.. notes.Where(n => !n.Archived)],
                Archive = [.. notes.Where(n => n.Archived)],
            };
            JsonSerializer.Serialize(stream, file, NoteJsonContext.Default.NoteFile);
        }

        File.Move(temp, FilePath, overwrite: true);
    }
}
