using System.Text.Json;

namespace No;

internal sealed class NoteStore
{
    private static readonly string HomeFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".no", "notes.json");

    /// <summary>The file notes are actually read from and written to.</summary>
    public string FilePath => ActivePath(Read(HomeFilePath));

    public List<Note> Load()
    {
        var home = Read(HomeFilePath);
        var file = home.Path is { Length: > 0 } alt ? Read(alt) : home;
        foreach (var note in file.Archive)
        {
            note.Archived = true;
        }

        return [.. file.Notes, .. file.Archive];
    }

    public void Save(List<Note> notes)
    {
        var home = Read(HomeFilePath);
        if (home.Path is { Length: > 0 } alt)
        {
            Write(alt, ToFile(notes));
        }
        else
        {
            var file = ToFile(notes);
            file.Editor = home.Editor;
            Write(HomeFilePath, file);
        }
    }

    /// <summary>The configured external editor command, if any.</summary>
    public string? Editor => Read(HomeFilePath).Editor;

    public void SetEditor(string? command)
    {
        var home = Read(HomeFilePath);
        home.Editor = string.IsNullOrWhiteSpace(command) ? null : command.Trim();
        Write(HomeFilePath, home);
    }

    /// <summary>Points the home file at a new location, or back at itself when null.</summary>
    public string SetPath(string? raw)
    {
        var notes = Load();
        var editor = Read(HomeFilePath).Editor;

        if (raw is null)
        {
            var local = ToFile(notes);
            local.Editor = editor;
            Write(HomeFilePath, local);
            return HomeFilePath;
        }

        var target = Normalize(raw);
        if (target != FilePath)
        {
            // Merge into any existing file so nothing is lost; the target wins on matching ids.
            var destination = Read(target);
            var known = destination.Notes.Concat(destination.Archive).Select(n => n.Id).ToHashSet();
            var merged = new List<Note>(destination.Archive.Select(n => { n.Archived = true; return n; }));
            merged.InsertRange(0, destination.Notes);
            merged.AddRange(notes.Where(n => !known.Contains(n.Id)));
            Write(target, ToFile(merged));
        }

        Write(HomeFilePath, new NoteFile { Path = target, Editor = editor });
        return target;
    }

    private static string ActivePath(NoteFile home) => home.Path is { Length: > 0 } alt ? alt : HomeFilePath;

    private static string Normalize(string raw)
    {
        var path = Environment.ExpandEnvironmentVariables(raw.Trim());
        if (path == "~" || path.StartsWith("~/") || path.StartsWith("~\\"))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..].TrimStart('/', '\\'));
        }

        path = Path.GetFullPath(path);
        var isFolder = Directory.Exists(path) || raw.EndsWith('/') || raw.EndsWith('\\');
        return isFolder ? Path.Combine(path, "notes.json") : path;
    }

    private static NoteFile ToFile(List<Note> notes) => new()
    {
        Notes = [.. notes.Where(n => !n.Archived)],
        Archive = [.. notes.Where(n => n.Archived)],
    };

    private static NoteFile Read(string path)
    {
        if (!File.Exists(path))
        {
            return new NoteFile();
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize(stream, NoteJsonContext.Default.NoteFile) ?? new NoteFile();
    }

    private static void Write(string path, NoteFile file)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Write then move so a crash never leaves a half-written file.
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        {
            JsonSerializer.Serialize(stream, file, NoteJsonContext.Default.NoteFile);
        }

        File.Move(temp, path, overwrite: true);
    }
}
