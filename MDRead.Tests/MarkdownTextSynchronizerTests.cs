using MDRead.Markdown;

namespace MDRead.Tests;

/// <summary>
/// Verifies that preview offsets identify the source even when rendered text is repeated or formatted.
/// </summary>
public sealed class MarkdownTextSynchronizerTests
{
    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(4, 0, 3)]
    [InlineData(6, 5, 11)]
    [InlineData(12, 12, 12)]
    [InlineData(17, 13, 17)]
    public void GetLineRange_UsesPhysicalLines(int caret, int start, int end)
    {
        Assert.Equal(new MarkdownSourceRange(start, end),
            MarkdownTextSynchronizer.GetLineRange("One\r\nSecond\n\nLast", caret));
    }

    [Fact]
    public void ToPreviewHtml_MapsDuplicateTextToDistinctLines()
    {
        const string source = "Same\r\nSame\r\n\r\n- **Same**";
        var html = MarkdownConverter.ToPreviewHtml(source);

        Assert.Contains("data-md-start=\"0\" data-md-end=\"4\" data-md-text=\"exact\">Same", html);
        Assert.Contains("data-md-start=\"6\" data-md-end=\"10\" data-md-text=\"exact\">Same", html);
        Assert.Contains("data-md-start=\"18\" data-md-end=\"22\" data-md-text=\"exact\">Same", html);
    }

    [Fact]
    public void ToPreviewHtml_MapsEachFencedCodeLineAndPreservesEscaping()
    {
        const string source = "```xml\r\n<x>\r\n<x>\r\n```";
        var html = MarkdownConverter.ToPreviewHtml(source);

        Assert.Contains("class=\"language-xml\"", html);
        Assert.Contains("data-md-start=\"8\" data-md-end=\"11\" data-md-text=\"exact\">&lt;x&gt;", html);
        Assert.Contains("data-md-start=\"13\" data-md-end=\"16\" data-md-text=\"exact\">&lt;x&gt;", html);
    }

    [Fact]
    public void ToPreviewHtml_MapsFormattedContentWithoutMappingImageAltMarkup()
    {
        const string source = "**déjà** &amp; `x` ![alt](image.png)";
        var html = MarkdownConverter.ToPreviewHtml(source);

        Assert.Contains("data-md-start=\"2\" data-md-end=\"6\" data-md-text=\"exact\">déjà", html);
        Assert.Contains("data-md-start=\"9\" data-md-end=\"14\" data-md-text=\"token\">&amp;", html);
        Assert.Contains("data-md-start=\"16\" data-md-end=\"17\" data-md-text=\"exact\"><code>x</code>", html);
        Assert.Contains("alt=\"alt\"", html);
        Assert.DoesNotContain("data-md-", MarkdownConverter.ToHtml(source));
    }
}
