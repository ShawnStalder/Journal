# Journal - Architecture

This file is additive to the enterprise-managed standards (C#/.NET and SQL); it does not restate them.

## Solution layout

| Project | Role |
| --- | --- |
| `src/Journal.Core` | Platform-neutral logic: models, file-backed stores, file watcher, HTML sanitizer, SQL highlighter, settings/session state. No UI references. |
| `src/Journal.App` | WPF shell (`net10.0-windows`) hosting Blazor pages in `BlazorWebView`. Tray icon, windows, theme, thumbnails, Razor components, `wwwroot` (CSS and JS). |
| `tests/Journal.Tests` | xUnit tests for `Journal.Core`. Tests use a temporary folder and a fixed `TimeProvider`. |

## On-disk format (the contract with the user's data)

```
<Root>\<Journal Name>\
    JournalTasks.tsk     JSON: { "tasks": [ { id, title, isCompleted, createdOn, completedOn } ] }
    Entries\             text notes, one .html file each
    SQL\                 SQL notes, one .sql file each
    Diagrams\            Mermaid diagram notes, one .mmd file each
    Images\              .png / .bmp / .jpg / .jpeg
```

- Archived journals live in `<Root>\Archive\<Journal Name>\` (same layout). A journal is identified everywhere by its
  path relative to the root (`Name` or `Archive\Name`), so stores, watchers and windows need no special cases.
  `Archive` is a reserved name and is excluded from the normal journal list. The journal page must dispose its file
  watcher before moving the folder, because the watcher holds it open.
- Note files are named `yyyy-MM-dd_HHmmss_Title.ext`. The timestamp drives newest-first ordering and the title is shown
  in the UI. Files that do not follow the pattern (added by hand) still load, using the file's last-write time.
  Editing a note can change its title, which renames the file but keeps the timestamp (`INoteStore.RenameNote`).
- The root is `JournalLocations.DefaultRootPath` (a UNC path, deliberately hard-coded). The `JOURNAL_ROOT_PATH`
  environment variable overrides it for development and tests only.
- `.tsk` writes re-read the file, apply one change and replace it via a temp file, so edits made by another process are
  not lost.

## Key components

- **Stores** (`INoteStore`, `ITaskStore`, `IImageStore`, `IJournalCatalog`): small single-purpose classes over the file
  system, registered as singletons in `App.xaml.cs`. They throw `JournalException` for problems the user can fix;
  file-system errors (`IOException`, `UnauthorizedAccessException`) bubble up.
- **`SafeComponentBase`**: Razor pages inherit it and wrap store calls in `RunSafelyAsync`, which turns those expected
  exceptions into a banner. Do not catch `Exception` broadly.
- **`WindowManager`** (`IWindowManager`): the only place that creates windows. One window per journal, plus single
  New/Open windows and image viewers. Components ask it to open things; they never create windows.
- **`BlazorWindow`**: a WPF `Window` whose content is a `BlazorWebView` rendering `AppShell`, which hosts the requested
  page via `DynamicComponent` and applies the theme.
- **`JournalWatcher`**: `FileSystemWatcher` with debouncing. A journal page reloads on change; watcher errors are treated
  as "something changed".
- **Rich text**: `contenteditable` plus `document.execCommand` in `wwwroot/js/journal.js`. Saved HTML is sanitized
  (`NoteHtmlSanitizer`) before it is displayed or loaded back into the editor.
- **Tables in text notes**: plain `<table>` HTML, so nothing changes on disk and the sanitizer needs no special rules.
  `wwwroot/js/tables.js` does the editing (insert, add/delete row and column, header toggle, cell shading, Tab between
  cells). Browsers have no row/column commands, so each change is made on a copy of the table and put back with
  `insertHTML`, which keeps it one undo step. `journal.js` calls `table.handleKey` and `table.handlePaste` from its editor
  listeners and sets an `in-table` class on `.rte` that enables the toolbar's table tools. Pasted tables (Excel, Word,
  web) are reduced to structure and text. Merged cells display but are not supported by the row/column commands.
- **Code blocks in text notes**: stored as `<pre><code class="language-xxx">` with plain text only (no highlighting markup).
  The editor inserts and cleans them in `journal.js` (`rte.insertCodeBlock`, `cleanCodeBlocks`; Enter inserts a newline,
  Tab inserts spaces, Ctrl+Enter leaves the block, paste is plain text). Highlighting and the Copy button are added at
  display time by `code.highlight` using the bundled highlight.js in `wwwroot/lib/highlight` (see its NOTICE.txt).
  Language ids live in `CodeLanguages` (Core) and are the only classes `NoteHtmlSanitizer` allows; add a language there,
  in the `languageLabels` map in `journal.js`, and make sure the bundle includes its grammar.
- **SQL editor**: a transparent `<textarea>` over a highlighted `<pre>`. `SqlHighlighter` produces the HTML. No external
  editor libraries, so the app works offline.
- **Diagram notes**: Mermaid source stored as `.mmd` in `Diagrams`. `DiagramView` (display, pan/zoom, Copy SVG/PNG) and
  `DiagramEditor` (source textarea with debounced live preview, template picker) both delegate to `wwwroot/js/diagram.js`,
  which lazy-loads the bundled `wwwroot/lib/mermaid/mermaid.min.js` (see its NOTICE.txt) on first use and renders one
  diagram at a time. Mermaid runs with `securityLevel: 'strict'` and HTML labels off (needed for PNG export); its SVG
  is inserted as-is, so it does not go through `NoteHtmlSanitizer`. Copies are always rendered with the light theme.
  Saving a diagram that does not parse needs a second Save. Templates and the Help panel share `DiagramTemplates` (Core);
  add a diagram type there once and both pick it up.
- **Thumbnails**: `ThumbnailProvider` decodes with WPF imaging, caches by path and timestamp, and limits concurrent
  reads to protect the network share. Browsers cannot read the share directly, so thumbnails are served as data URIs.
- **Tray and lifetime**: `ShutdownMode=OnExplicitShutdown`; a named mutex enforces a single instance. Restart saves the
  open journal names (`SessionStore`) and starts a new process, which waits on the mutex until the old one exits.

## Things to know before changing code

- WebView2 needs the Windows SDK projection, so the app project targets `net10.0-windows10.0.19041.0`.
- The app is per-monitor DPI aware via `app.manifest`; `WFO0003` is suppressed because WinForms is only used for the
  tray `NotifyIcon`.
- Keyboard focus must be given to the inner `WebView2` control (see `BlazorWindow`), not the `BlazorWebView` wrapper,
  or autofocus works in the DOM but typing does not reach the page.
- Dropping a file onto the page would navigate the browser away, so `journal.js` cancels window-level drag/drop.
- New behavior in `Journal.Core` needs unit tests. UI behavior is verified by running the app.
