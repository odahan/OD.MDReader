using System.Globalization;
using System.Net;
using System.Text;

namespace MDRead.Markdown;

/// <summary>
/// Locates equivalent rendered-text blocks in Markdown source and HTML preview content.
/// </summary>
internal static class MarkdownTextSynchronizer
{
    /// <summary>
    /// Creates an anchor for the Markdown block containing a caret position.
    /// </summary>
    /// <param name="markdown">The complete Markdown source.</param>
    /// <param name="caretPosition">The zero-based caret position in the source.</param>
    /// <returns>A normalized text anchor, or <see langword="null"/> when none is available.</returns>
    public static TextSynchronizationAnchor? CreateAnchor(
        string markdown,
        int caretPosition)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var blocks = CreateComparableBlocks(markdown);
        var matchingBlockIndex = blocks.FindIndex(
            block => caretPosition >= block.Block.Start
                && caretPosition <= block.Block.End);

        if (matchingBlockIndex < 0)
        {
            return null;
        }

        return CreateAnchor(blocks, matchingBlockIndex);
    }

    /// <summary>
    /// Finds the Markdown block that best matches a rendered-text anchor.
    /// </summary>
    /// <param name="markdown">The complete Markdown source.</param>
    /// <param name="anchor">The normalized anchor extracted from the preview.</param>
    /// <returns>The matching Markdown block, or <see langword="null"/> when the result is absent or ambiguous.</returns>
    public static MarkdownTextBlock? FindMatchingBlock(
        string markdown,
        TextSynchronizationAnchor anchor)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(anchor);

        var blocks = CreateComparableBlocks(markdown);
        var matches = blocks
            .Select((block, index) => new { Block = block, Index = index })
            .Where(candidate => candidate.Block.NormalizedText == anchor.Text)
            .ToList();

        if (matches.Count == 1)
        {
            return matches[0].Block.Block;
        }

        if (matches.Count == 0)
        {
            return null;
        }

        var highestScore = matches.Max(candidate => GetContextScore(
            blocks,
            candidate.Index,
            anchor));
        var bestMatches = matches
            .Where(candidate => GetContextScore(blocks, candidate.Index, anchor) == highestScore)
            .ToList();

        return highestScore > 0 && bestMatches.Count == 1
            ? bestMatches[0].Block.Block
            : null;
    }

    /// <summary>
    /// Converts Markdown into a compact key representing its visible rendered text.
    /// </summary>
    /// <param name="markdown">The Markdown fragment to normalize.</param>
    /// <returns>A key containing only lower-case letters and digits.</returns>
    public static string NormalizeMarkdown(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        return NormalizeVisibleText(ExtractTextFromHtml(MarkdownConverter.ToHtml(markdown)));
    }

    /// <summary>
    /// Converts visible text into a compact, formatting-insensitive comparison key.
    /// </summary>
    /// <param name="text">The visible text to normalize.</param>
    /// <returns>A key containing only lower-case letters and digits.</returns>
    public static string NormalizeVisibleText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var normalized = text.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (IsLetterOrNumber(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static List<ComparableMarkdownBlock> CreateComparableBlocks(string markdown) =>
        GetBlocks(markdown)
            .Select(block => new ComparableMarkdownBlock(
                block,
                NormalizeMarkdown(block.Text)))
            .Where(block => !string.IsNullOrEmpty(block.NormalizedText))
            .ToList();

    private static TextSynchronizationAnchor? CreateAnchor(
        IReadOnlyList<ComparableMarkdownBlock> blocks,
        int index)
    {
        var block = blocks[index];
        return string.IsNullOrEmpty(block.NormalizedText)
            ? null
            : new TextSynchronizationAnchor(
                block.NormalizedText,
                GetPreviousText(blocks, index),
                GetNextText(blocks, index));
    }

    private static int GetContextScore(
        IReadOnlyList<ComparableMarkdownBlock> blocks,
        int index,
        TextSynchronizationAnchor anchor)
    {
        var score = 0;
        if (anchor.Previous is not null && anchor.Previous == GetPreviousText(blocks, index))
        {
            score++;
        }

        if (anchor.Next is not null && anchor.Next == GetNextText(blocks, index))
        {
            score++;
        }

        return score;
    }

    private static string? GetPreviousText(
        IReadOnlyList<ComparableMarkdownBlock> blocks,
        int index) => index > 0 ? blocks[index - 1].NormalizedText : null;

    private static string? GetNextText(
        IReadOnlyList<ComparableMarkdownBlock> blocks,
        int index) => index < blocks.Count - 1 ? blocks[index + 1].NormalizedText : null;

    private static bool IsLetterOrNumber(char character) =>
        CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.LetterNumber
            or UnicodeCategory.OtherNumber;

    private static string ExtractTextFromHtml(string html)
    {
        var text = new StringBuilder(html.Length);
        var index = 0;

        while (index < html.Length)
        {
            if (html[index] == '<')
            {
                index = SkipHtmlTag(html, index + 1);
                continue;
            }

            if (html[index] == '&')
            {
                var entityEnd = html.IndexOf(';', index + 1);
                if (entityEnd >= 0)
                {
                    text.Append(WebUtility.HtmlDecode(html[index..(entityEnd + 1)]));
                    index = entityEnd + 1;
                    continue;
                }
            }

            text.Append(html[index]);
            index++;
        }

        return text.ToString();
    }

    private static int SkipHtmlTag(string html, int index)
    {
        char? quote = null;
        while (index < html.Length)
        {
            var character = html[index++];
            if (quote is not null)
            {
                if (character == quote)
                {
                    quote = null;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (character == '>')
            {
                break;
            }
        }

        return index;
    }

    private static IReadOnlyList<MarkdownTextBlock> GetBlocks(string markdown)
    {
        var lines = GetLines(markdown);
        var blocks = new List<MarkdownTextBlock>();

        for (var index = 0; index < lines.Count;)
        {
            if (string.IsNullOrWhiteSpace(lines[index].Text))
            {
                index++;
                continue;
            }

            var startIndex = index;
            if (IsFence(lines[index].Text, out var fence))
            {
                index++;
                while (index < lines.Count && !IsClosingFence(lines[index].Text, fence))
                {
                    index++;
                }

                if (index < lines.Count)
                {
                    index++;
                }
            }
            else if (IsHeading(lines[index].Text)
                || IsListItem(lines[index].Text)
                || IsTableRow(lines[index].Text))
            {
                index++;
            }
            else if (IsQuote(lines[index].Text))
            {
                while (index < lines.Count && IsQuote(lines[index].Text))
                {
                    index++;
                }
            }
            else
            {
                index++;
                while (index < lines.Count
                    && !string.IsNullOrWhiteSpace(lines[index].Text)
                    && !IsFence(lines[index].Text, out _)
                    && !IsHeading(lines[index].Text)
                    && !IsListItem(lines[index].Text)
                    && !IsQuote(lines[index].Text)
                    && !IsTableRow(lines[index].Text))
                {
                    index++;
                }
            }

            var start = lines[startIndex].Start;
            var end = lines[index - 1].End;
            blocks.Add(new MarkdownTextBlock(
                start,
                end,
                startIndex,
                markdown[start..end]));
        }

        return blocks;
    }

    private static List<MarkdownLine> GetLines(string markdown)
    {
        var lines = new List<MarkdownLine>();
        var start = 0;

        while (start <= markdown.Length)
        {
            var lineFeedIndex = markdown.IndexOf('\n', start);
            var fullEnd = lineFeedIndex >= 0 ? lineFeedIndex + 1 : markdown.Length;
            var end = lineFeedIndex >= 0 ? lineFeedIndex : markdown.Length;
            if (end > start && markdown[end - 1] == '\r')
            {
                end--;
            }

            lines.Add(new MarkdownLine(start, end, markdown[start..end]));
            if (lineFeedIndex < 0)
            {
                break;
            }

            start = fullEnd;
        }

        return lines;
    }

    private static bool IsFence(string line, out string fence)
    {
        var trimmed = line.TrimStart();
        fence = trimmed.StartsWith("```") ? "```"
            : trimmed.StartsWith("~~~") ? "~~~"
            : string.Empty;
        return fence.Length > 0;
    }

    private static bool IsClosingFence(string line, string fence) =>
        line.TrimStart().StartsWith(fence, StringComparison.Ordinal);

    private static bool IsHeading(string line) =>
        line.TrimStart().StartsWith('#');

    private static bool IsListItem(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- ")
            || trimmed.StartsWith("* ")
            || trimmed.StartsWith("+ ")
            || (trimmed.Length > 2
                && char.IsDigit(trimmed[0])
                && trimmed.IndexOfAny(['.', ')']) > 0);
    }

    private static bool IsTableRow(string line) => line.Contains('|');

    private static bool IsQuote(string line) => line.TrimStart().StartsWith('>');

    private sealed record ComparableMarkdownBlock(
        MarkdownTextBlock Block,
        string NormalizedText);

    private sealed record MarkdownLine(int Start, int End, string Text);
}

/// <summary>
/// Identifies a block by its normalized text and optional immediate context.
/// </summary>
/// <param name="Text">The normalized text of the block.</param>
/// <param name="Previous">The normalized text of the preceding block, when available.</param>
/// <param name="Next">The normalized text of the following block, when available.</param>
internal sealed record TextSynchronizationAnchor(
    string Text,
    string? Previous,
    string? Next);

/// <summary>
/// Describes a logical Markdown block and its location in the source editor.
/// </summary>
/// <param name="Start">The inclusive character position of the block.</param>
/// <param name="End">The exclusive character position of the block.</param>
/// <param name="StartLine">The zero-based source line containing the start of the block.</param>
/// <param name="Text">The Markdown source contained in the block.</param>
internal sealed record MarkdownTextBlock(int Start, int End, int StartLine, string Text);
