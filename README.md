# MDRead

MDRead is a Windows desktop application for reading and editing Markdown files as styled HTML5 documents.

## Features

- Opens `.md` and `.markdown` files and displays them as a clean HTML page.
- Uses a dark theme by default; a light theme can be selected at any time.
- Supports command-line opening: `MDRead --Dark path\\to\\document.md`.
- Provides an **Edit mode** with Markdown source on top and a live HTML preview below.
- Includes an editor toolbar for headings, bold, italic, links, lists, inline code, and quotes.
- Saves Markdown in place or with **Save as**.
- Exports a standalone HTML5 document with all CSS embedded.
- Closes with `Esc` when used in read-only mode.

## Getting started

Build the project with the .NET 8 SDK on Windows, then run `MDRead.exe`. Open a file from **File → Open** or pass its path on the command line.

```text
MDRead --Dark "C:\\Documents\\notes.md"
```

## Supported Markdown

MDRead supports headings, paragraphs, bold and italic text, links, images, unordered lists, quotes, inline code, fenced code blocks, and horizontal rules.

## License

MDRead is distributed under the private-use license in [LICENSE](LICENSE).
