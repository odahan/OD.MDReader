# MDRead

MDRead is a Windows desktop application for reading and editing Markdown files as styled HTML5 documents.

## Features

- Reads, edits, and creates Markdown files.
- Opens `.md` and `.markdown` files and displays them as a clean HTML page.
- Opens Markdown files dropped from File Explorer or compatible applications.
- Uses a dark theme by default; a light theme can be selected at any time.
- Supports command-line opening: `MDRead --Dark path\\to\\document.md`.
- Provides an **Edit mode** with Markdown source on top and a live HTML preview below.
- Includes an editor toolbar for headings, bold, italic, links, lists, inline code, and quotes.
- Saves Markdown in place or with **Save as**.
- Exports a standalone HTML5 document with all CSS embedded.
- Closes with `Esc` when used in read-only mode.

## Requirements

MDRead requires the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/consumer/) to display Markdown previews. It is preinstalled on Windows 11 and on most up-to-date Windows 10 devices. If it is missing, MDRead displays a prompt with a link to the official Microsoft installation page.

## Getting started

Build the project with the .NET 8 SDK on Windows, then run `MDRead.exe`.

- Use **File → Open** to select an existing Markdown file.
- Start MDRead with a file path to open it directly from a command line:

```text
MDRead.exe "C:\\Documents\\notes.md"
```

- Use **File → New** to create a new Markdown document, then save it as a `.md` file.

If you do not already use another Markdown reader or editor, associating `.md` files with MDRead in Windows makes opening them from File Explorer more convenient.

## Supported Markdown

MDRead supports headings, paragraphs, bold and italic text, links, images, unordered lists, quotes, inline code, fenced code blocks, horizontal rules, and Markdown tables with column alignment.

## License

MDRead is distributed under the private-use license in [LICENSE](LICENSE).
