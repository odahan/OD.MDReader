using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MDRead.Markdown;

/// <summary>
/// Converts Markdown documents into safe HTML by using a configured Markdig pipeline.
/// </summary>
internal static class MarkdownConverter
{
    private static readonly MarkdownPipeline Pipeline = CreatePipeline();

    /// <summary>
    /// Converts Markdown source into an HTML fragment.
    /// </summary>
    /// <param name="markdown">The Markdown source.</param>
    /// <returns>The generated HTML fragment.</returns>
    public static string ToHtml(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var document = Markdig.Markdown.Parse(markdown, Pipeline);
        SanitizeLinks(document);
        return Markdig.Markdown.ToHtml(document, Pipeline);
    }

    /// <summary>
    /// Removes executable and unsupported URI schemes from links and images.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    private static void SanitizeLinks(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (!IsSafeTarget(link.Url))
            {
                link.Url = string.Empty;
                link.GetDynamicUrl = null;
            }
        }
    }

    /// <summary>
    /// Determines whether a Markdown target can safely be rendered as an HTML URL.
    /// </summary>
    /// <param name="target">The target to validate.</param>
    /// <returns><see langword="true"/> for relative targets and approved URI schemes.</returns>
    private static bool IsSafeTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target)
            || !Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return true;
        }

        return uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == Uri.UriSchemeMailto
            || uri.IsFile;
    }

    /// <summary>
    /// Creates the immutable Markdown pipeline shared by all preview and export operations.
    /// </summary>
    /// <returns>The configured safe Markdown pipeline.</returns>
    private static MarkdownPipeline CreatePipeline()
    {
        var builder = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseGridTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseAutoIdentifiers()
            .UseDefinitionLists()
            .UseFootnotes()
            .UseEmphasisExtras()
            .UseListExtras()
            .DisableHtml();

        builder.Extensions.Add(new SafeUnderlineExtension());
        return builder.Build();
    }
}
