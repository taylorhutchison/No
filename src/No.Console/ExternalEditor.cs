using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace No;

internal static class ExternalEditor
{
    /// <summary>Edits the text in an external editor; null when the editor reports failure.</summary>
    public static string? Edit(string body, string? configured)
    {
        var parts = Split(Resolve(configured));
        var temp = Path.Combine(Path.GetTempPath(), $"no-{Guid.NewGuid():N}.md");
        File.WriteAllText(temp, body);

        try
        {
            using var process = Start(parts, temp);
            process.WaitForExit();
            return process.ExitCode == 0 ? File.ReadAllText(temp).Replace("\r\n", "\n") : null;
        }
        finally
        {
            File.Delete(temp);
        }
    }

    private static Process Start(List<string> parts, string file)
    {
        try
        {
            return Launch(parts, file);
        }
        catch (Win32Exception) when (OperatingSystem.IsWindows())
        {
            // Commands like "code" are .cmd shims that only cmd.exe can resolve.
            return Launch(["cmd.exe", "/d", "/c", .. parts], file);
        }
    }

    private static Process Launch(List<string> parts, string file)
    {
        var info = new ProcessStartInfo(parts[0]) { UseShellExecute = false };
        foreach (var arg in parts.Skip(1).Append(file))
        {
            info.ArgumentList.Add(arg);
        }

        return Process.Start(info) ?? throw new InvalidOperationException("Could not start the editor.");
    }

    private static string Resolve(string? configured)
    {
        foreach (var candidate in new[]
        {
            configured,
            Environment.GetEnvironmentVariable("VISUAL"),
            Environment.GetEnvironmentVariable("EDITOR"),
        })
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        if (OperatingSystem.IsWindows()) return "notepad";
        if (OperatingSystem.IsMacOS()) return "open -W -t";
        return OnPath("nano") ? "nano" : "vi";
    }

    private static bool OnPath(string program) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => File.Exists(Path.Combine(dir, program)));

    // Splits on whitespace, keeping double-quoted sections together.
    private static List<string> Split(string command)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        foreach (var c in command)
        {
            if (c == '"') quoted = !quoted;
            else if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0) { parts.Add(current.ToString()); current.Clear(); }
            }
            else current.Append(c);
        }

        if (current.Length > 0) parts.Add(current.ToString());
        return parts;
    }
}
