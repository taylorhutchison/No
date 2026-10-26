<div align="center">

# `no`

**Notes, from the terminal. Fast.**

A tiny note-taking app that works the way you already take notes in Notepad:
dump everything in, then *find* what you need.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)
![Platforms](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)
![Storage](https://img.shields.io/badge/storage-single%20JSON%20file-orange)

</div>

---

## Why `no`?

Most note apps ask you to organize first and write second. `no` keeps it simple:

- **Instant start.** Open it and you are looking at your notes. No sync, no splash screen, no setup.
- **Two lists, nothing more.** *Notes* and *Archive*.
- **A title, then type.** New notes need only a title, then you are in a free-form editor.
- **Find, don't file.** Search across every title and body from the command line without opening anything.
- **Your data stays yours.** Everything lives in one readable JSON file you can sync, back up, or grep.

## Quick start

```powershell
no                      # open the notes list
no --search "invoice"   # search without opening the app
no --version            # print the version
```

## The notes list

```
 Notes (3)  Archive (1)

  Title                          Modified
  Meeting notes - sprint 42      2026-10-25 14:02
  Server runbook                 2026-10-24 09:15
  Ideas                          2026-10-20 18:40

Up/Down move  Enter open  e external  n new  a archive  d delete  r rename  / search  Tab switch  f refresh  q quit
C:\Users\you\.no\notes.json
```

Notes are sorted by most recently modified, and the path of the file in use is always shown at the bottom.

| Key | Action |
| --- | --- |
| `Up` / `Down` | Move the selection |
| `Home` / `End` | Jump to first / last note |
| `Tab` or `Left` / `Right` | Switch between **Notes** and **Archive** |
| `Enter` | Open the note in the built-in editor |
| `e` | Open the note in an [external editor](#external-editor) |
| `n` | New note (asks for a title, then opens the editor) |
| `a` | Archive the note, or restore it when viewing the Archive |
| `d` | Delete the note (asks for confirmation) |
| `r` | Rename the note |
| `/` | Search the current list, then pick a result to open |
| `f` or `F5` | Refresh from disk (handy when several windows are open) |
| `q` or `Esc` | Quit |

Archiving never requires opening the note.

## The editor

A small, dependable text editor that behaves the same on every platform. Multi-line, free-form, as many new lines as you like.

| Key | Action |
| --- | --- |
| Arrow keys | Move the cursor |
| `Home` / `End` | Start / end of the line |
| `Ctrl+Home` / `Ctrl+End` | Start / end of the note |
| `PageUp` / `PageDown` | Move a screen at a time |
| `Enter` | New line |
| `Backspace` / `Delete` | Remove a character, joining lines when needed |
| `Tab` | Insert two spaces |
| `Ctrl+S` | Save |
| `Esc` | Close. If there are unsaved changes you are asked: `y` save, `n` discard, any other key keeps editing |

The title bar shows `*` when there are unsaved changes.

## Searching

Search never needs the editor. Pass `--search` (or `-s`) and a term:

```powershell
no --search kubernetes          # active notes only
no -s "release checklist"       # multi-word terms work without quotes too
no --search budget --all        # include archived notes
```

- Matching is **case-insensitive** and checks both **titles and bodies**.
- Each result shows a short **snippet** around the match.
- In a normal terminal you get a **picker** to open a result straight away.
- When output is piped or redirected, results print as plain `title<TAB>snippet` lines, so it composes with other tools.
- The exit code is `0` when something matched and `1` when nothing did.

Inside the app, press `/` to search the list you are looking at.

## External editor

Prefer your own editor? Press `e` on any note. The note is written to a temporary file, your editor opens, and when you close it the changes are saved back.

```powershell
no --set-editor "code --wait"   # VS Code
no --set-editor "subl -w"       # Sublime Text
no --set-editor "nano"          # any terminal editor
no --clear-editor               # back to the default
```

If no editor is configured, `no` uses `$VISUAL`, then `$EDITOR`, then a platform default:

| Platform | Default |
| --- | --- |
| Windows | `notepad` |
| macOS | `open -W -t` |
| Linux | `nano`, falling back to `vi` |

> **Tip:** GUI editors must be told to wait, otherwise they return immediately and nothing is saved. That is what `--wait` / `-w` does. A non-zero exit code from the editor is treated as a cancel.

## Where your notes live

By default everything is stored in `~/.no/notes.json` (your home folder, on every OS).

```json
{
  "notes": [
    {
      "id": "337ce68d-cc04-4101-a461-63fc6eb36a0d",
      "title": "Meeting notes - sprint 42",
      "body": "Agenda\n- review\n- planning\n",
      "created": "2026-10-25T20:50:35+00:00",
      "modified": "2026-10-25T20:50:41+00:00"
    }
  ],
  "archive": []
}
```

Archived notes simply move from `notes` to `archive`. Saves are written to a temporary file first and then swapped in, so a crash can never leave a half-written file.

### Storing notes somewhere else (OneDrive, Dropbox, ...)

Point `no` at any file or folder and your existing notes are **copied (merged) there** automatically:

```powershell
no --set-path "$env:OneDrive\Notes\"     # a folder: notes.json is added for you
no --set-path "~/Dropbox/notes.json"     # or an exact file; ~ and %VARS% are expanded
no --clear-path                          # bring notes back to the home folder
```

The home `notes.json` stays in place and just records the new location (`"path": "..."`), so `no` always knows where to look. If the destination already contains notes, they are kept and yours are added alongside them.

Using several windows or machines? Press `f` to refresh and pick up changes made elsewhere. Note that the last save wins, so refresh before editing if another window may have changed things.

## Command reference

| Command | Description |
| --- | --- |
| `no` | Open the interactive notes list |
| `no --search <term> [--all]` | Search notes (`-s` is shorthand); `--all` includes the archive |
| `no --set-path <file-or-folder>` | Store notes in a different location (existing notes are merged in) |
| `no --clear-path` | Move notes back to `~/.no/notes.json` |
| `no --set-editor "<command>"` | Set the external editor command |
| `no --clear-editor` | Reset the external editor to the default |
| `no --version` | Print the version (`-v` is shorthand) |

## Install

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) to build it.

**1. Publish a fast, self-contained build** into `~/.no/bin`:

```powershell
./publish.ps1
```

This is the same as:

```powershell
dotnet publish src/No.Console -c Release -r win-x64 --self-contained true -o $env:USERPROFILE\.no\bin
```

Use a different runtime identifier for other platforms, for example `linux-x64`, `linux-arm64`, or `osx-arm64`. The project is configured for a **single-file, ReadyToRun** build, so startup is quick.

**2. Add it to your `PATH`** (once):

```powershell
$bin = "$env:USERPROFILE\.no\bin"
[Environment]::SetEnvironmentVariable("Path", "$([Environment]::GetEnvironmentVariable('Path','User'));$bin", "User")
```

On macOS / Linux:

```bash
echo 'export PATH="$HOME/.no/bin:$PATH"' >> ~/.bashrc
```

Open a new terminal and run `no`.

To update, close any running `no` and publish again.

## Build from source

```powershell
dotnet build src/No.slnx
dotnet run --project src/No.Console
```

## Built with

- [.NET 10](https://dotnet.microsoft.com/)
- [Spectre.Console](https://spectreconsole.net/) for the terminal UI
