using System.Text.Json.Serialization;

namespace No;

public sealed class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.UtcNow;

    // Derived from which list the note is stored under in the file.
    [JsonIgnore]
    public bool Archived { get; set; }
}

public sealed class NoteFile
{
    // Only meaningful in the home-directory file: where the notes actually live.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Path { get; set; }

    // Only meaningful in the home-directory file: command used by the external editor.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Editor { get; set; }

    public List<Note> Notes { get; set; } = [];

    public List<Note> Archive { get; set; } = [];
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(NoteFile))]
internal sealed partial class NoteJsonContext : JsonSerializerContext;
