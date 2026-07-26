# MDRead 1.4.1 — Complete Markdown Test File

This document demonstrates the Markdown features supported by MDRead through
Markdig. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod
tempor incididunt ut labore et dolore magna aliqua.

[Jump to the pipe table](#pipe-table) · [Open the project README](README.md)

Setext heading level 1
======================

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec odio.
Praesent libero. Sed cursus ante dapibus diam.

Setext heading level 2
----------------------

Sed nisi. Nulla quis sem at nibh elementum imperdiet. Duis sagittis ipsum.
Praesent mauris. Fusce nec tellus sed augue semper porta.

## ATX headings

### Heading level 3

#### Heading level 4

##### Heading level 5

###### Heading level 6

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Curabitur sodales
ligula in libero. Sed dignissim lacinia nunc.

---

## Paragraphs and line breaks

This is a paragraph containing Lorem ipsum dolor sit amet, consectetur
adipiscing elit. Curabitur tortor. Pellentesque nibh. Aenean quam. In scelerisque
sem at dolor.

This line ends with a backslash and therefore creates a hard line break.\
This text begins on the next rendered line.

This sentence
contains a soft line break that a browser normally displays as a space.

## Inline formatting

Lorem **ipsum bold**, dolor *sit amet italic*, and
***consectetur bold and italic***.

The same styles also work with underscores: __adipiscing bold__ and
_elit italic_.

Extended emphasis supplied by Markdig:

- ~~strikethrough lorem ipsum~~
- ~subscript~
- ^superscript^
- ++inserted text++
- ==marked text==
- <u>underlined text inserted by the MDRead toolbar</u>

Escaped punctuation remains literal: \*not italic\*, \# not a heading, and
\[not a link\].

## Blockquotes

> Lorem ipsum dolor sit amet, consectetur adipiscing elit.
>
> > Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.
>
> Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut
> aliquip ex ea commodo consequat.

## Unordered and nested lists

- Lorem ipsum dolor sit amet
- Consectetur adipiscing elit
  - Sed do eiusmod tempor
  - Incididunt ut labore
    - Et dolore magna aliqua
- Ut enim ad minim veniam

## Ordered and extended lists

1. Lorem ipsum dolor sit amet
2. Consectetur adipiscing elit
   1. Sed do eiusmod tempor
   2. Incididunt ut labore
3. Et dolore magna aliqua

Alpha list:

a. Lorem ipsum dolor sit amet
b. Consectetur adipiscing elit
c. Sed do eiusmod tempor

Roman list:

i. Lorem ipsum dolor sit amet
ii. Consectetur adipiscing elit
iii. Sed do eiusmod tempor

## Task list

- [x] Parse CommonMark content
- [x] Render pipe and grid tables
- [x] Preserve safe underline tags
- [ ] Read more Lorem ipsum

## Links and automatic links

An [inline link](https://example.com "Example website") and a
[reference-style link][lorem-reference] can contain Lorem ipsum text.

Automatic links:

- https://example.com/lorem
- <https://example.org/ipsum>
- <mailto:lorem@example.com>
- [A relative Markdown link to the README](README.md#supported-markdown)

[lorem-reference]: https://example.net "Lorem reference"

## Image

![MDRead 1.4.1 placeholder](https://placehold.co/800x240/1e1e1e/e8e8e8.png?text=MDRead+1.4.1 "Remote placeholder image")

*Lorem ipsum image caption written as an ordinary emphasized paragraph.*

## Inline code

Use `MDRead.exe "C:\Documents\lorem.md"` to open a document. Markdown characters
inside code remain literal: `**lorem** <u>ipsum</u>`.

## Fenced code blocks

```csharp
var lorem = "Lorem ipsum dolor sit amet";
Console.WriteLine(lorem);
```

```html
<section>
  <p>Lorem ipsum is displayed as code, not as HTML.</p>
</section>
```

An unterminated fence is deliberately not included because the rest of this
demonstration must continue rendering.

Indented code block:

    Lorem ipsum dolor sit amet;
    consectetur adipiscing elit;

## Pipe table

| Criterion | Description | Score | Weight |
|---|:---|---:|:---:|
| Lorem | Lorem ipsum dolor sit amet | 5 | 20 % |
| Ipsum | Consectetur adipiscing elit | 4 | 15 % |
| Dolor | Sed do eiusmod tempor | 3 | 10 % |
| Escaped pipe | A\|B and `x|y` | 2 | 5 % |
| Empty value |  | 1 | 0 % |

## Grid table

+----------------------+------------------------------------------+
| Item                 | Description                              |
+======================+==========================================+
| Lorem ipsum          | Dolor sit amet, consectetur adipiscing.  |
+----------------------+------------------------------------------+
| Sed do eiusmod       | Tempor incididunt ut labore et dolore.   |
+----------------------+------------------------------------------+

## Definition list

Lorem
:   Lorem ipsum dolor sit amet, consectetur adipiscing elit.

Ipsum
:   Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.

Dolor
:   Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.

## Footnotes

Lorem ipsum dolor sit amet has a short footnote.[^short]

Consectetur adipiscing elit has a longer note.[^long]

[^short]: Sed do eiusmod tempor incididunt.

[^long]: Ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis
    nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo
    consequat.

## Character entities

Copyright &copy; MDRead. Lorem &amp; ipsum, dolor &lt; sit amet &gt;.

## Raw HTML safety

Raw HTML is displayed as text for safety:

<span style="color:red" onclick="alert('unsafe')">Lorem ipsum cannot inject HTML.</span>

The exact `<u>` and `</u>` tags remain the sole exception because MDRead uses
them for its underline command.

## Final thematic break

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
incididunt ut labore et dolore magna aliqua.

***

End of the MDRead 1.4.1 Markdown test file.
