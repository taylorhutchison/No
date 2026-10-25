using No;

var searching = false;
var all = false;
var terms = new List<string>();

foreach (var arg in args)
{
    if (arg is "--all") all = true;
    else if (arg is "--search" or "-s") searching = true;
    else if (searching) terms.Add(arg);
    else return Usage();
}

var ui = new NotesUi(new NoteStore());

if (!searching)
{
    ui.Run();
    return 0;
}

var term = string.Join(' ', terms).Trim();
return term.Length == 0 ? Usage() : ui.RunSearch(term, all);

static int Usage()
{
    Console.Error.WriteLine("Usage: no [--search <term> [--all]]");
    return 2;
}
