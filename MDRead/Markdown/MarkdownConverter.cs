using MDRead.Constants;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MDRead.Markdown;

/// <summary>
/// Converts the Markdown subset supported by MDRead into safe HTML.
/// </summary>
internal static class MarkdownConverter
{
    private static readonly Regex HeadingPattern =
        CreatePattern(MarkdownPatterns.Heading);
    private static readonly Regex UnorderedItemPattern =
        CreatePattern(MarkdownPatterns.UnorderedItem);
    private static readonly Regex OrderedItemPattern =
        CreatePattern(MarkdownPatterns.OrderedItem);
    private static readonly Regex ImagePattern =
        CreatePattern(MarkdownPatterns.Image);
    private static readonly Regex LinkPattern =
        CreatePattern(MarkdownPatterns.Link);
    private static readonly Regex InlineCodePattern =
        CreatePattern(MarkdownPatterns.InlineCode);
    private static readonly Regex BoldPattern =
        CreatePattern(MarkdownPatterns.Bold);
    private static readonly Regex StrikethroughPattern =
        CreatePattern(MarkdownPatterns.Strikethrough);
    private static readonly Regex UnderlinePattern =
        CreatePattern(MarkdownPatterns.Underline);
    private static readonly Regex EmphasisPattern =
        CreatePattern(MarkdownPatterns.Emphasis);

    /// <summary>
    /// Converts Markdown source into an HTML fragment.
    /// </summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <returns>The generated HTML fragment.</returns>
    public static string ToHtml(string markdown)
    {
        var html = new StringBuilder();
        var paragraph = new StringBuilder();
        var lines = NormalizeLineEndings(markdown).Split(DocumentConstants.LineFeed);
        string? openListTag = null;
        var isInsideCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraph.Length == 0)
            {
                return;
            }

            html.Append(HtmlTemplates.ParagraphOpen)
                .Append(ToInlineHtml(paragraph.ToString().Trim()))
                .AppendLine(HtmlTemplates.ParagraphClose);
            paragraph.Clear();
        }

        void CloseList()
        {
            if (openListTag is null)
            {
                return;
            }

            html.AppendLine(string.Format(
                HtmlTemplates.ClosingTagFormat,
                openListTag));
            openListTag = null;
        }

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            var line = rawLine.TrimEnd();

            if (line.StartsWith(MarkdownSyntax.CodeBlockOpen.Trim(), StringComparison.Ordinal))
            {
                FlushParagraph();
                CloseList();
                html.AppendLine(
                    isInsideCodeBlock
                        ? HtmlTemplates.CodeBlockClose
                        : HtmlTemplates.CodeBlockOpen);
                isInsideCodeBlock = !isInsideCodeBlock;
                continue;
            }

            if (isInsideCodeBlock)
            {
                html.AppendLine(WebUtility.HtmlEncode(rawLine));
                continue;
            }

            if (lineIndex + 1 < lines.Length &&
                TryGetTableDefinition(
                    line,
                    lines[lineIndex + 1].TrimEnd(),
                    out var tableHeaders,
                    out var tableAlignments))
            {
                FlushParagraph();
                CloseList();
                AppendTable(html, lines, ref lineIndex, tableHeaders, tableAlignments);
                continue;
            }

            var heading = HeadingPattern.Match(line);
            if (heading.Success)
            {
                FlushParagraph();
                CloseList();
                html.AppendLine(string.Format(
                    HtmlTemplates.HeadingFormat,
                    heading.Groups[1].Length,
                    ToInlineHtml(heading.Groups[2].Value)));
                continue;
            }

            if (UnorderedItemPattern.IsMatch(line))
            {
                AppendListItem(
                    html,
                    ref openListTag,
                    HtmlTemplates.UnorderedListTag,
                    UnorderedItemPattern.Replace(line, string.Empty),
                    FlushParagraph,
                    CloseList);
                continue;
            }

            if (OrderedItemPattern.IsMatch(line))
            {
                AppendListItem(
                    html,
                    ref openListTag,
                    HtmlTemplates.OrderedListTag,
                    OrderedItemPattern.Replace(line, string.Empty),
                    FlushParagraph,
                    CloseList);
                continue;
            }

            if (line.StartsWith(HtmlTemplates.BlockquotePrefix, StringComparison.Ordinal))
            {
                FlushParagraph();
                CloseList();
                html.AppendLine(string.Format(
                    HtmlTemplates.BlockquoteFormat,
                    ToInlineHtml(line[HtmlTemplates.BlockquotePrefix.Length..])));
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                CloseList();
                continue;
            }

            if (line is HtmlTemplates.HyphenHorizontalRule
                or HtmlTemplates.AsteriskHorizontalRule)
            {
                FlushParagraph();
                CloseList();
                html.AppendLine(HtmlTemplates.HorizontalRule);
                continue;
            }

            if (paragraph.Length > 0)
            {
                paragraph.Append(' ');
            }

            paragraph.Append(line);
        }

        FlushParagraph();
        CloseList();

        if (isInsideCodeBlock)
        {
            html.AppendLine(HtmlTemplates.CodeBlockClose);
        }

        return html.ToString();
    }

    private static Regex CreatePattern(string pattern) =>
        new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string NormalizeLineEndings(string markdown) =>
        markdown.Replace(
            DocumentConstants.WindowsLineEnding,
            DocumentConstants.LineFeedString,
            StringComparison.Ordinal);

    private static void AppendListItem(
        StringBuilder html,
        ref string? openListTag,
        string listTag,
        string content,
        Action flushParagraph,
        Action closeList)
    {
        flushParagraph();

        if (openListTag != listTag)
        {
            closeList();
            html.AppendLine(string.Format(
                HtmlTemplates.OpeningTagFormat,
                listTag));
            openListTag = listTag;
        }

        html.AppendLine(string.Format(
            HtmlTemplates.ListItemFormat,
            ToInlineHtml(content)));
    }

    /// <summary>
    /// Attempts to read a Markdown table header and its delimiter row.
    /// </summary>
    /// <param name="headerLine">The potential table header row.</param>
    /// <param name="delimiterLine">The potential table delimiter row.</param>
    /// <param name="headers">The parsed header cells.</param>
    /// <param name="alignments">The alignment requested for each column.</param>
    /// <returns><see langword="true"/> when the two lines define a table.</returns>
    private static bool TryGetTableDefinition(
        string headerLine,
        string delimiterLine,
        out string[] headers,
        out string?[] alignments)
    {
        headers = Array.Empty<string>();
        alignments = Array.Empty<string?>();

        if (!TrySplitTableRow(headerLine, out var headerCells) ||
            !TrySplitTableRow(delimiterLine, out var delimiterCells) ||
            headerCells.Count != delimiterCells.Count)
        {
            return false;
        }

        var columnAlignments = new string?[delimiterCells.Count];
        for (var columnIndex = 0; columnIndex < delimiterCells.Count; columnIndex++)
        {
            if (!TryGetColumnAlignment(
                    delimiterCells[columnIndex],
                    out columnAlignments[columnIndex]))
            {
                return false;
            }
        }

        headers = headerCells.ToArray();
        alignments = columnAlignments;
        return true;
    }

    /// <summary>
    /// Adds a table and all of its following data rows to the HTML output.
    /// </summary>
    /// <param name="html">The HTML output builder.</param>
    /// <param name="lines">The source lines.</param>
    /// <param name="lineIndex">The current header-line index.</param>
    /// <param name="headers">The table header cells.</param>
    /// <param name="alignments">The alignment requested for each column.</param>
    private static void AppendTable(
        StringBuilder html,
        string[] lines,
        ref int lineIndex,
        string[] headers,
        string?[] alignments)
    {
        html.AppendLine("<table>");
        html.AppendLine("<thead>");
        AppendTableRow(html, headers, "th", alignments);
        html.AppendLine("</thead>");
        html.AppendLine("<tbody>");

        lineIndex += 2;
        while (lineIndex < lines.Length &&
               TrySplitTableRow(lines[lineIndex].TrimEnd(), out var cells))
        {
            AppendTableRow(
                html,
                NormalizeTableCells(cells, headers.Length),
                "td",
                alignments);
            lineIndex++;
        }

        html.AppendLine("</tbody>");
        html.AppendLine("</table>");
        lineIndex--;
    }

    /// <summary>
    /// Adds one HTML table row.
    /// </summary>
    /// <param name="html">The HTML output builder.</param>
    /// <param name="cells">The table cell contents.</param>
    /// <param name="cellTag">The HTML tag to use for each cell.</param>
    /// <param name="alignments">The alignment requested for each column.</param>
    private static void AppendTableRow(
        StringBuilder html,
        IReadOnlyList<string> cells,
        string cellTag,
        IReadOnlyList<string?> alignments)
    {
        html.Append("<tr>");

        for (var columnIndex = 0; columnIndex < cells.Count; columnIndex++)
        {
            html.Append('<').Append(cellTag);

            if (alignments[columnIndex] is { } alignment)
            {
                html.Append(" style=\"text-align: ")
                    .Append(alignment)
                    .Append('\"');
            }

            html.Append('>')
                .Append(ToInlineHtml(cells[columnIndex]))
                .Append("</")
                .Append(cellTag)
                .Append('>');
        }

        html.AppendLine("</tr>");
    }

    /// <summary>
    /// Splits a Markdown table row into cells while preserving escaped and code-span pipes.
    /// </summary>
    /// <param name="line">The potential table row.</param>
    /// <param name="cells">The parsed and trimmed cells.</param>
    /// <returns><see langword="true"/> when the line contains table-cell separators.</returns>
    private static bool TrySplitTableRow(string line, out List<string> cells)
    {
        cells = [];
        var cell = new StringBuilder();
        var isInsideCodeSpan = false;
        var hasSeparator = false;

        for (var characterIndex = 0; characterIndex < line.Length; characterIndex++)
        {
            var character = line[characterIndex];
            if (character == '\\' &&
                characterIndex + 1 < line.Length &&
                line[characterIndex + 1] == '|')
            {
                cell.Append('|');
                characterIndex++;
                continue;
            }

            if (character == '`')
            {
                isInsideCodeSpan = !isInsideCodeSpan;
            }

            if (character == '|' && !isInsideCodeSpan)
            {
                cells.Add(cell.ToString().Trim());
                cell.Clear();
                hasSeparator = true;
                continue;
            }

            cell.Append(character);
        }

        if (!hasSeparator)
        {
            return false;
        }

        cells.Add(cell.ToString().Trim());

        if (cells[0].Length == 0)
        {
            cells.RemoveAt(0);
        }

        if (cells.Count > 0 && cells[^1].Length == 0)
        {
            cells.RemoveAt(cells.Count - 1);
        }

        return cells.Count > 0;
    }

    /// <summary>
    /// Determines the alignment represented by a Markdown table delimiter cell.
    /// </summary>
    /// <param name="delimiter">The delimiter cell to inspect.</param>
    /// <param name="alignment">The matching CSS text alignment, if any.</param>
    /// <returns><see langword="true"/> when the cell is a valid table delimiter.</returns>
    private static bool TryGetColumnAlignment(string delimiter, out string? alignment)
    {
        alignment = null;
        var trimmedDelimiter = delimiter.Trim();
        var startsWithColon = trimmedDelimiter.StartsWith(':');
        var endsWithColon = trimmedDelimiter.EndsWith(':');
        var hyphenStart = startsWithColon ? 1 : 0;
        var hyphenEnd = trimmedDelimiter.Length - (endsWithColon ? 1 : 0);

        if (hyphenEnd - hyphenStart < 3 ||
            !trimmedDelimiter[hyphenStart..hyphenEnd].All(character => character == '-'))
        {
            return false;
        }

        alignment = (startsWithColon, endsWithColon) switch
        {
            (true, true) => "center",
            (false, true) => "right",
            (true, false) => "left",
            _ => null
        };
        return true;
    }

    /// <summary>
    /// Truncates extra table cells and supplies missing cells for a table row.
    /// </summary>
    /// <param name="cells">The parsed table cells.</param>
    /// <param name="columnCount">The number of columns in the header.</param>
    /// <returns>The cells normalized to the header column count.</returns>
    private static string[] NormalizeTableCells(
        IReadOnlyList<string> cells,
        int columnCount)
    {
        var normalizedCells = Enumerable.Repeat(string.Empty, columnCount).ToArray();
        for (var columnIndex = 0;
             columnIndex < columnCount && columnIndex < cells.Count;
             columnIndex++)
        {
            normalizedCells[columnIndex] = cells[columnIndex];
        }

        return normalizedCells;
    }

    private static string ToInlineHtml(string text)
    {
        var html = WebUtility.HtmlEncode(text);
        html = ImagePattern.Replace(html, HtmlTemplates.ImageReplacement);
        html = LinkPattern.Replace(html, HtmlTemplates.LinkReplacement);
        html = InlineCodePattern.Replace(html, HtmlTemplates.InlineCodeReplacement);
        html = BoldPattern.Replace(html, HtmlTemplates.BoldReplacement);
        html = StrikethroughPattern.Replace(
            html,
            HtmlTemplates.StrikethroughReplacement);
        html = UnderlinePattern.Replace(html, HtmlTemplates.UnderlineReplacement);
        return EmphasisPattern.Replace(html, HtmlTemplates.EmphasisReplacement);
    }
}
