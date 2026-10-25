using System.Text;

namespace No;

internal static class Editor
{
    /// <summary>Returns the edited text when saved, or null when closed without saving.</summary>
    public static string? Edit(string title, string body)
    {
        var lines = body.Replace("\r\n", "\n").Split('\n').ToList();
        int row = 0, col = 0, top = 0, left = 0;
        var dirty = false;
        var status = "";

        while (true)
        {
            var width = Math.Max(Console.WindowWidth, 10);
            var height = Math.Max(Console.WindowHeight, 4);
            var view = height - 2;

            if (row < top) top = row;
            if (row >= top + view) top = row - view + 1;
            if (col < left) left = col;
            if (col >= left + width - 1) left = col - width + 2;

            Draw(title, lines, top, left, view, width, row, col, status, dirty);
            status = "";

            var key = Console.ReadKey(true);
            var ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

            if (ctrl && key.Key == ConsoleKey.S)
            {
                return string.Join('\n', lines);
            }

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    if (!dirty)
                    {
                        return null;
                    }

                    Draw(title, lines, top, left, view, width, row, col, "Save changes? (q = save, n = discard, any other key = keep editing)", dirty);
                    var answer = Console.ReadKey(true).Key;
                    if (answer == ConsoleKey.Q) return string.Join('\n', lines);
                    if (answer == ConsoleKey.N) return null;
                    break;

                case ConsoleKey.LeftArrow:
                    if (col > 0) col--;
                    else if (row > 0) { row--; col = lines[row].Length; }
                    break;

                case ConsoleKey.RightArrow:
                    if (col < lines[row].Length) col++;
                    else if (row < lines.Count - 1) { row++; col = 0; }
                    break;

                case ConsoleKey.UpArrow:
                    if (row > 0) row--;
                    col = Math.Min(col, lines[row].Length);
                    break;

                case ConsoleKey.DownArrow:
                    if (row < lines.Count - 1) row++;
                    col = Math.Min(col, lines[row].Length);
                    break;

                case ConsoleKey.PageUp:
                    row = Math.Max(0, row - view);
                    col = Math.Min(col, lines[row].Length);
                    break;

                case ConsoleKey.PageDown:
                    row = Math.Min(lines.Count - 1, row + view);
                    col = Math.Min(col, lines[row].Length);
                    break;

                case ConsoleKey.Home:
                    if (ctrl) row = 0;
                    col = 0;
                    break;

                case ConsoleKey.End:
                    if (ctrl) row = lines.Count - 1;
                    col = lines[row].Length;
                    break;

                case ConsoleKey.Enter:
                    lines.Insert(row + 1, lines[row][col..]);
                    lines[row] = lines[row][..col];
                    row++;
                    col = 0;
                    dirty = true;
                    break;

                case ConsoleKey.Backspace:
                    if (col > 0)
                    {
                        lines[row] = lines[row].Remove(col - 1, 1);
                        col--;
                        dirty = true;
                    }
                    else if (row > 0)
                    {
                        col = lines[row - 1].Length;
                        lines[row - 1] += lines[row];
                        lines.RemoveAt(row);
                        row--;
                        dirty = true;
                    }
                    break;

                case ConsoleKey.Delete:
                    if (col < lines[row].Length)
                    {
                        lines[row] = lines[row].Remove(col, 1);
                        dirty = true;
                    }
                    else if (row < lines.Count - 1)
                    {
                        lines[row] += lines[row + 1];
                        lines.RemoveAt(row + 1);
                        dirty = true;
                    }
                    break;

                case ConsoleKey.Tab:
                    lines[row] = lines[row].Insert(col, "  ");
                    col += 2;
                    dirty = true;
                    break;

                default:
                    if (!ctrl && !char.IsControl(key.KeyChar))
                    {
                        lines[row] = lines[row].Insert(col, key.KeyChar.ToString());
                        col++;
                        dirty = true;
                    }
                    break;
            }
        }
    }

    private static void Draw(string title, List<string> lines, int top, int left, int view, int width,
        int row, int col, string status, bool dirty)
    {
        var sb = new StringBuilder();
        sb.Append("\e[?25l\e[H");

        var header = $" {title}{(dirty ? " *" : "")}";
        sb.Append("\e[7m").Append(Fit(header, width - 1).PadRight(width - 1)).Append("\e[0m\e[K\n");

        for (var i = 0; i < view; i++)
        {
            var index = top + i;
            if (index < lines.Count && left < lines[index].Length)
            {
                var line = lines[index];
                sb.Append(line, left, Math.Min(line.Length - left, width - 1));
            }

            sb.Append("\e[K\n");
        }

        var footer = status.Length > 0 ? status : " Ctrl+S save   Esc close";
        sb.Append("\e[7m").Append(Fit(footer, width - 1).PadRight(width - 1)).Append("\e[0m\e[K");

        sb.Append($"\e[{row - top + 2};{col - left + 1}H\e[?25h");
        Console.Write(sb.ToString());
    }

    private static string Fit(string text, int width) => text.Length <= width ? text : text[..width];
}
