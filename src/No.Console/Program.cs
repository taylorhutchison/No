using No;

var searching = false;
var all = false;
var terms = new List<string>();
var store = new NoteStore();

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    if (arg is "--all") all = true;
    else if (arg is "--search" or "-s") searching = true;
    else if (arg is "--set-path" && !searching)
    {
        if (i + 1 >= args.Length || args.Length != 2) return Usage();
        Console.WriteLine($"Notes file: {store.SetPath(args[i + 1])}");
        return 0;
    }
    else if (arg is "--clear-path" && !searching && args.Length == 1)
    {
        store.SetPath(null);
        Console.WriteLine($"Notes file: {store.FilePath}");
        return 0;
    }
    else if (arg is "--set-editor" && !searching)
    {
        if (i + 1 >= args.Length || args.Length != 2) return Usage();
        store.SetEditor(args[i + 1]);
        Console.WriteLine($"External editor: {store.Editor}");
        return 0;
    }
    else if (arg is "--clear-editor" && !searching && args.Length == 1)
    {
        store.SetEditor(null);
        Console.WriteLine("External editor reset to the default.");
        return 0;
    }
    else if (searching) terms.Add(arg);
    else return Usage();
}

var ui = new NotesUi(store);

if (!searching)
{
    ui.Run();
    return 0;
}

var term = string.Join(' ', terms).Trim();
return term.Length == 0 ? Usage() : ui.RunSearch(term, all);

static int Usage()
{
    Console.Error.WriteLine("Usage: no [--search <term> [--all]] | --set-path <file-or-folder> | --clear-path | --set-editor \"<command>\" | --clear-editor");
    return 2;
}
