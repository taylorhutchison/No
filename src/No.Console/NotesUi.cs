using Spectre.Console;

namespace No;

internal sealed class NotesUi(NoteStore store)
{
    private readonly List<Note> _notes = store.Load();
    private string _path = store.FilePath;

    public void Run()
    {
        var tab = 0;
        var index = 0;
        var top = 0;
        var message = "";

        EnterScreen();
        try
        {
            while (true)
            {
                var visible = Visible(tab);
                index = Math.Clamp(index, 0, Math.Max(0, visible.Count - 1));
                var rows = Math.Max(3, Console.WindowHeight - 9);
                if (index < top) top = index;
                if (index >= top + rows) top = index - rows + 1;

                Draw(visible, tab, index, top, rows, message);
                message = "";

                var key = Console.ReadKey(true);
                var current = visible.Count > 0 ? visible[index] : null;

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow: index = Math.Max(0, index - 1); break;
                    case ConsoleKey.DownArrow: index++; break;
                    case ConsoleKey.Home: index = 0; break;
                    case ConsoleKey.End: index = int.MaxValue; break;
                    case ConsoleKey.Tab:
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        tab = 1 - tab;
                        index = 0;
                        top = 0;
                        break;
                    case ConsoleKey.Enter when current is not null:
                        Open(current);
                        break;
                    case ConsoleKey.Escape:
                        return;
                    case ConsoleKey.F5:
                        Refresh();
                        message = "Refreshed.";
                        break;
                    default:
                        switch (char.ToLowerInvariant(key.KeyChar))
                        {
                            case 'q':
                                return;
                            case 'f':
                                Refresh();
                                message = "Refreshed.";
                                break;
                            case 'n':
                                var title = Ask("Title:");
                                if (!string.IsNullOrWhiteSpace(title))
                                {
                                    var note = new Note { Title = title.Trim() };
                                    _notes.Add(note);
                                    store.Save(_notes);
                                    Open(note);
                                    tab = 0;
                                    index = 0;
                                    top = 0;
                                }
                                break;
                            case 'a' when current is not null:
                                current.Archived = !current.Archived;
                                store.Save(_notes);
                                message = current.Archived ? "Archived." : "Restored.";
                                break;
                            case 'd' when current is not null:
                                if (Confirm($"Delete '{current.Title}'?"))
                                {
                                    _notes.Remove(current);
                                    store.Save(_notes);
                                    message = "Deleted.";
                                }
                                break;
                            case 'r' when current is not null:
                                var renamed = Ask("New title:", current.Title);
                                if (!string.IsNullOrWhiteSpace(renamed))
                                {
                                    current.Title = renamed.Trim();
                                    current.Modified = DateTimeOffset.UtcNow;
                                    store.Save(_notes);
                                }
                                break;
                            case '/':
                                var term = Ask("Search:");
                                if (!string.IsNullOrWhiteSpace(term))
                                {
                                    var hits = NoteSearch.Find(visible, term.Trim());
                                    if (hits.Count == 0) message = "No matches.";
                                    else OpenFromHits(hits);
                                }
                                break;
                        }
                        break;
                }
            }
        }
        finally
        {
            ExitScreen();
        }
    }

    public int RunSearch(string term, bool includeArchived)
    {
        var hits = NoteSearch.Find(_notes.Where(n => includeArchived || !n.Archived), term);
        if (hits.Count == 0)
        {
            Console.Error.WriteLine("No matches.");
            return 1;
        }

        if (Console.IsOutputRedirected || Console.IsInputRedirected)
        {
            foreach (var hit in hits)
            {
                Console.WriteLine($"{hit.Note.Title}{(hit.Note.Archived ? " [archived]" : "")}\t{hit.Snippet}");
            }

            return 0;
        }

        OpenFromHits(hits);
        return 0;
    }

    private void OpenFromHits(List<SearchHit> hits)
    {
        const string cancel = "(cancel)";
        var choices = hits
            .Select((h, i) => $"{i + 1}. {Markup.Escape(h.Note.Title)}{(h.Note.Archived ? " [grey](archived)[/]" : "")}"
                + (h.Snippet.Length > 0 ? $" [grey]- {Markup.Escape(h.Snippet)}[/]" : ""))
            .Append(cancel)
            .ToList();

        Console.CursorVisible = true;
        var picked = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Open which note?").PageSize(15).AddChoices(choices));
        Console.CursorVisible = false;

        var chosen = choices.IndexOf(picked);
        if (chosen >= 0 && chosen < hits.Count)
        {
            Open(hits[chosen].Note);
        }
    }

    private void Open(Note note)
    {
        EnterScreen();
        try
        {
            var edited = Editor.Edit(note.Title, note.Body);
            if (edited is not null && edited != note.Body)
            {
                note.Body = edited;
                note.Modified = DateTimeOffset.UtcNow;
                store.Save(_notes);
            }
        }
        finally
        {
            ExitScreen();
        }
    }

    private void Refresh()
    {
        var latest = store.Load();
        _notes.Clear();
        _notes.AddRange(latest);
        _path = store.FilePath;
    }

    private List<Note> Visible(int tab) =>
        [.. _notes.Where(n => n.Archived == (tab == 1)).OrderByDescending(n => n.Modified)];

    private void Draw(List<Note> visible, int tab, int index, int top, int rows, string message)
    {
        var active = _notes.Count(n => !n.Archived);
        var archived = _notes.Count - active;

        string Tab(string name, int count, bool on) =>
            on ? $"[bold black on white] {name} ({count}) [/]" : $"[grey] {name} ({count}) [/]";

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine($"{Tab("Notes", active, tab == 0)} {Tab("Archive", archived, tab == 1)}");
        AnsiConsole.WriteLine();

        if (visible.Count == 0)
        {
            AnsiConsole.MarkupLine(tab == 0 ? "[grey]No notes. Press n to create one.[/]" : "[grey]Archive is empty.[/]");
        }
        else
        {
            var table = new Table().Border(TableBorder.None).AddColumn("Title").AddColumn("Modified");
            for (var i = top; i < Math.Min(visible.Count, top + rows); i++)
            {
                var n = visible[i];
                var title = n.Title.Length > 60 ? n.Title[..59] + "…" : n.Title;
                string Cell(string text) => i == index ? $"[invert]{Markup.Escape(text)}[/]" : Markup.Escape(text);
                table.AddRow(Cell(title), Cell(Format(n.Modified)));
            }

            AnsiConsole.Write(table);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(message.Length > 0 ? Markup.Escape(message) : " ");
        var archiveKey = tab == 0 ? "archive" : "restore";
        AnsiConsole.MarkupLine($"[grey]Up/Down move  Enter open  n new  a {archiveKey}  d delete  r rename  / search  Tab switch  f refresh  q quit[/]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(_path)}[/]");
    }

    private static string Format(DateTimeOffset value) => value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    private static string? Ask(string prompt, string? defaultValue = null)
    {
        Console.CursorVisible = true;
        try
        {
            var p = new TextPrompt<string>(prompt).AllowEmpty();
            if (defaultValue is not null) p.DefaultValue(defaultValue);
            return AnsiConsole.Prompt(p);
        }
        finally
        {
            Console.CursorVisible = false;
        }
    }

    private static bool Confirm(string prompt)
    {
        Console.CursorVisible = true;
        try
        {
            return AnsiConsole.Confirm(Markup.Escape(prompt), false);
        }
        finally
        {
            Console.CursorVisible = false;
        }
    }

    private static int _screenDepth;

    private static void EnterScreen()
    {
        if (_screenDepth++ == 0) Console.Write("\e[?1049h");
        Console.CursorVisible = false;
    }

    private static void ExitScreen()
    {
        if (--_screenDepth == 0)
        {
            Console.Write("\e[?1049l");
            Console.CursorVisible = true;
        }
    }
}
