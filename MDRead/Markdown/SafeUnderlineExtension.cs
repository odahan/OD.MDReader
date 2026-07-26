using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MDRead.Markdown;

/// <summary>
/// Preserves MDRead's underline tags while keeping every other raw HTML tag escaped.
/// </summary>
internal sealed class SafeUnderlineExtension : IMarkdownExtension
{
    /// <inheritdoc />
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.InlineParsers.Insert(0, new SafeUnderlineInlineParser());
    }

    /// <inheritdoc />
    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Replace<HtmlInlineRenderer>(
                new SafeHtmlInlineRenderer());
        }
    }

    /// <summary>
    /// Renders only the exact underline tags as HTML and escapes all other inline HTML.
    /// </summary>
    private sealed class SafeHtmlInlineRenderer : HtmlObjectRenderer<HtmlInline>
    {
        /// <inheritdoc />
        protected override void Write(HtmlRenderer renderer, HtmlInline htmlInline)
        {
            if (htmlInline.Tag is "<u>" or "</u>")
            {
                renderer.Write(htmlInline.Tag);
                return;
            }

            renderer.WriteEscape(htmlInline.Tag);
        }
    }

    /// <summary>
    /// Recognizes only MDRead's exact opening and closing underline tags.
    /// </summary>
    private sealed class SafeUnderlineInlineParser : InlineParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SafeUnderlineInlineParser"/> class.
        /// </summary>
        public SafeUnderlineInlineParser()
        {
            OpeningCharacters = ['<'];
        }

        /// <inheritdoc />
        public override bool Match(
            InlineProcessor processor,
            ref StringSlice slice)
        {
            var tag = slice.Match("<u>")
                ? "<u>"
                : slice.Match("</u>")
                    ? "</u>"
                    : null;

            if (tag is null)
            {
                return false;
            }

            var start = slice.Start;
            slice.Start += tag.Length;
            processor.Inline = new HtmlInline(tag)
            {
                Span = new SourceSpan(start, slice.Start - 1)
            };
            return true;
        }
    }
}
