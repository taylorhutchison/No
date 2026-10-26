namespace No;

internal sealed record SearchHit(Note Note, string Snippet);

internal static class NoteSearch
{
    public static List<SearchHit> Find(IEnumerable<Note> notes, string term)
    {
        var hits = new List<SearchHit>();
        foreach (var note in notes)
        {
            var inBody = note.Body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            var inTitle = note.Title.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (inBody < 0 && !inTitle)
            {
                continue;
            }

            hits.Add(new SearchHit(note, inBody >= 0 ? Snippet(note.Body, inBody, term.Length) : ""));
        }

        return [.. hits.OrderByDescending(h => h.Note.Modified)];
    }

    private static string Snippet(string text, int index, int length)
    {
        var start = Math.Max(0, index - 30);
        var end = Math.Min(text.Length, index + length + 50);
        var slice = string.Join(' ', text[start..end].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return (start > 0 ? "…" : "") + slice + (end < text.Length ? "…" : "");
    }
}
