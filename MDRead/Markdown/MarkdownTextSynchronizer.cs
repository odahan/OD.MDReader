using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Globalization;
using System.IO;

namespace MDRead.Markdown;

/// <summary>
/// Associates rendered preview elements with their original Markdown character ranges.
/// </summary>
internal static class MarkdownTextSynchronizer
{
    /// <summary>
    /// Gets the physical source line at the caret, independent of editor word wrapping.
    /// </summary>
    public static MarkdownSourceRange GetLineRange(string source, int caret)
    {
        caret = Math.Clamp(caret, 0, source.Length);
        if (caret > 0 && caret < source.Length
            && source[caret - 1] == '\r' && source[caret] == '\n')
        {
            caret--;
        }

        var start = caret;
        while (start > 0 && source[start - 1] is not ('\r' or '\n'))
        {
            start--;
        }

        var end = caret;
        while (end < source.Length && source[end] is not ('\r' or '\n'))
        {
            end++;
        }

        return new MarkdownSourceRange(start, end);
    }

    /// <summary>
    /// Renders preview HTML with source ranges on blocks and selectable inline content.
    /// </summary>
    public static string RenderPreview(
        MarkdownDocument document,
        MarkdownPipeline pipeline,
        string source)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new MappedCodeBlockRenderer(source));
        renderer.ObjectWriteBefore += (_, item) =>
        {
            if (!IsValidRange(item.Span, source))
            {
                return;
            }

            if (item is Block and not MarkdownDocument or LinkInline { IsImage: true })
            {
                AddRange(item.GetAttributes(), item.Span);
            }
            else if (renderer.EnableHtmlForInline && IsTextRun(item))
            {
                var span = item.Span;
                var isExact = item is LiteralInline literal
                    && source.AsSpan(span.Start, span.Length).SequenceEqual(literal.Content.AsSpan());
                if (item is CodeInline code && span.Length > 2 * code.DelimiterCount)
                {
                    var contentOffset = span.Start + code.DelimiterCount;
                    var content = source.AsSpan(contentOffset, span.Length - 2 * code.DelimiterCount);
                    var offset = content.IndexOf(code.ContentSpan, StringComparison.Ordinal);
                    if (offset >= 0 && !code.ContentSpan.IsEmpty)
                    {
                        span = new SourceSpan(contentOffset + offset, contentOffset + offset + code.ContentSpan.Length - 1);
                        isExact = true;
                    }
                }

                OpenTextRun(renderer, span, isExact);
            }
        };
        renderer.ObjectWriteAfter += (_, item) =>
        {
            if (renderer.EnableHtmlForInline && IsTextRun(item) && IsValidRange(item.Span, source))
            {
                renderer.Write("</span>");
            }
        };

        renderer.Render(document);
        return writer.ToString();
    }

    private static bool IsTextRun(MarkdownObject item) =>
        item is LiteralInline or CodeInline or HtmlEntityInline or AutolinkInline;

    private static bool IsValidRange(SourceSpan span, string source) =>
        span.Start >= 0 && span.End >= span.Start && span.End < source.Length;

    private static void AddRange(HtmlAttributes attributes, SourceSpan span)
    {
        attributes.AddProperty("data-md-start", span.Start.ToString(CultureInfo.InvariantCulture));
        attributes.AddProperty("data-md-end", (span.End + 1).ToString(CultureInfo.InvariantCulture));
    }

    private static void OpenTextRun(HtmlRenderer renderer, SourceSpan span, bool isExact)
    {
        var attributes = new HtmlAttributes();
        AddRange(attributes, span);
        attributes.AddProperty("data-md-text", isExact ? "exact" : "token");
        renderer.Write("<span").WriteAttributes(attributes).Write(">");
    }

    /// <summary>
    /// Maps each rendered code line separately while preserving its whitespace and language class.
    /// </summary>
    private sealed class MappedCodeBlockRenderer(string source) : HtmlObjectRenderer<CodeBlock>
    {
        /// <inheritdoc />
        protected override void Write(HtmlRenderer renderer, CodeBlock block)
        {
            renderer.EnsureLine();
            renderer.Write("<pre><code").WriteAttributes(block).Write(">");

            for (var index = 0; index < block.Lines.Count; index++)
            {
                var slice = block.Lines.Lines[index].Slice;
                var span = new SourceSpan(slice.Start, slice.End);
                var isMapped = ReferenceEquals(slice.Text, source) && IsValidRange(span, source);
                if (isMapped)
                {
                    OpenTextRun(renderer, span, isExact: true);
                }

                renderer.WriteEscape(slice);
                if (isMapped)
                {
                    renderer.Write("</span>");
                }

                renderer.WriteLine();
            }

            renderer.WriteLine("</code></pre>");
        }
    }
}

/// <summary>
/// Represents a half-open range of UTF-16 positions in the Markdown source.
/// </summary>
internal readonly record struct MarkdownSourceRange(int Start, int End);
