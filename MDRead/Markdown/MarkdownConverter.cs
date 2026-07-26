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

        foreach (var rawLine in lines)
        {
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
