# MDRead

MDRead is a Windows desktop application for reading and editing Markdown files as styled HTML5 documents.

## Features

- Reads, edits, and creates Markdown files.
- Opens `.md` and `.markdown` files and displays them as a clean HTML page.
- Opens Markdown files dropped from File Explorer or compatible applications.
- Uses a dark theme by default; a light theme can be selected at any time.
- Supports command-line opening: `MDRead --Dark path\\to\\document.md`.
- Opens on the **HTML** preview tab. Switch to **Markdown** to edit the source.
- Applies the selected theme to the tab bar and marks the active tab in blue.
- Includes an editor toolbar for headings, bold, italic, links, lists, inline code, and quotes.
- Saves Markdown in place or with **Save as**.
- Exports a standalone HTML5 document with all CSS embedded.
- Closes with `Esc` from the **HTML** tab.

## Requirements

MDRead requires the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/consumer/) to display Markdown previews. It is preinstalled on Windows 11 and on most up-to-date Windows 10 devices. If it is missing, MDRead displays a prompt with a link to the official Microsoft installation page.

## Getting started

Download and run `MDRead.exe` from the [GitHub releases](https://github.com/odahan/OD.MDReader/releases).

- Use **File → Open** to select an existing Markdown file.
- Start MDRead with a file path to open it directly from a command line:

```text
MDRead.exe "C:\\Documents\\notes.md"
```

- Use **File → New** to create a new Markdown document, then save it as a `.md` file.

If you do not already use another Markdown reader or editor, associating `.md` files with MDRead in Windows makes opening them from File Explorer more convenient.

## Synchronize the preview and source

The synchronization button is the first control in each tab's toolbar.

- In **Markdown**, place the caret on a source line and click **Sync HTML** to locate its rendered content.
- In **HTML**, select text with the mouse and click **Sync Markdown** to locate the corresponding source, including a partial selection within a sentence.

The target tab opens, scrolls to the match, and highlights it for 2.5 seconds. Repeated passages are distinguished by their source positions.

## Publish from source

Install the .NET 10 SDK on Windows and double-click `Publish.BAT` in File Explorer.
It runs `Publish.ps1` and keeps the console open so you can read the result.
The script writes a self-contained Windows x64 executable to `publish\MDRead.exe`.

## Supported Markdown

MDRead uses the
[Markdig 1.3.2 NuGet package](https://www.nuget.org/packages/Markdig/1.3.2)
for CommonMark-compatible Markdown processing. It supports headings, paragraphs,
emphasis, links, images, lists, quotes, inline and fenced code, horizontal rules,
pipe and grid tables, task lists, automatic links, definition lists, footnotes,
heading identifiers, strikethrough, and other extended emphasis. Raw HTML is
escaped for safety, except for the `<u>` tag inserted by the editor toolbar.

Open [TESTFILE.MD](TESTFILE.MD) in MDRead for a comprehensive, ready-to-use
demonstration of the supported syntax and rendering.

## Line breaks and copying preview text

The preview uses WebView2's standard copy behavior. Apostrophes (`'` and `’`)
are ordinary text and are preserved by Markdown conversion.

In Markdown, a literal `\n` is text, not a line break. Use a blank line between
paragraphs:

```markdown
L'agent corrige le code.

Cette automatisation aide l'équipe.
```

To insert a line break within one paragraph, end the source line with two spaces
or a single backslash, followed by an actual newline:

```markdown
L'agent corrige le code.\
Cette automatisation aide l'équipe.
```

An ordinary source newline within a paragraph is a soft break and can appear as
a space in the preview and copied text. MDRead does not automatically interpret
literal escape sequences such as `\n`, since they can be intentional content,
especially in code examples and paths.

## Link behavior

- Fragment links such as `[Table](#pipe-table)` scroll directly to the
  corresponding heading in the current document. They work in both the MDRead
  preview and exported HTML.
- Web and email links are passed to Windows only after confirmation in MDRead.
- Relative local links are resolved from the directory containing the open
  Markdown file. This includes links to other `.md` and `.markdown` documents.
- Potentially dangerous local file types are blocked before Windows can open
  them.

## License

MDRead is distributed under the private-use license in [LICENSE](LICENSE).
