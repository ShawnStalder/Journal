# Journal

## Purpose

A system-tray journal for tracking daily work, tasks, longer-term projects and miscellaneous notes. Each **journal** is a
container of:

- **Text notes** - rich text (bold, italic, underline, colors, highlights, lists, headings, code blocks, tables), saved as `.html`.
- **SQL notes** - syntax-highlighted, saved as `.sql`. They are stored and copied only; the application never executes SQL.
- **Diagram notes** - Mermaid diagrams with a live preview, templates, syntax help, pan/zoom and copy as SVG or PNG, saved as `.mmd`.
- **Tasks** - a checklist with the date and time each task was completed, saved in `JournalTasks.tsk`.
- **Images** - `.png`, `.bmp` and `.jpg` files, shown as thumbnails that open in their own window.

The tray menu offers **New Journal**, **Open Journal**, **Theme** (dark/light), **Restart** and **Exit**. Journals open
in their own windows and refresh automatically when files change on disk.

## Architecture

See [CLAUDE.md](CLAUDE.md) for the solution architecture and key decisions.

## Tech stack and key dependencies

- .NET 10, C#
- WPF shell with Blazor (`BlazorWebView`, WebView2) for the UI
- `HtmlSanitizer` (Ganss.Xss) to make saved HTML safe to render
- highlight.js (bundled, BSD-3-Clause) for syntax highlighting of code blocks in text notes
- Mermaid (bundled, MIT) for rendering diagram notes
- xUnit for tests
- Storage: files on the network share `\\dbsfp1.dbs.local\users\sstalder\My Documents\Journals\`
- Runtime requirement: the Microsoft Edge WebView2 Runtime (included with Windows 11)

## Local setup, build and run

Prerequisites: .NET 10 SDK, Windows 10 (19041) or later.

```
dotnet build
dotnet test
dotnet run --project src/Journal.App
```

By default journals are stored on the network share above, so you must be able to reach it. To develop or test
without touching the share, point the app at a local folder before launching:

```
set JOURNAL_ROOT_PATH=C:\Temp\JournalTest
dotnet run --project src/Journal.App
```

Per-user state (theme, restart session, error log) is kept in `%LOCALAPPDATA%\Journal`.

## Ownership

Owned by the DBS Software Development department (assumed - confirm and update). Questions or escalation should start with
that department's manager.
