using MDRead.Markdown;

namespace MDRead.Tests;

/// <summary>
/// Verifies supported block and inline Markdown conversion.
/// </summary>
public sealed class MarkdownConverterTests
{
    [Fact]
    public void ToHtml_EncodesUnsafeTextAndCombinesParagraphLines()
    {
        var html = MarkdownConverter.ToHtml("Hello <world>\r\ncontinued");

        Assert.Equal("<p>Hello &lt;world&gt; continued</p>\r\n", html);
    }

    [Fact]
    public void ToHtml_ConvertsHeadingsAndHorizontalRules()
    {
        var html = MarkdownConverter.ToHtml("# Title\n\n### Section\n---\n***");

        Assert.Equal(
            "<h1>Title</h1>\r\n<h3>Section</h3>\r\n<hr>\r\n<hr>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_ConvertsAndClosesBothListTypes()
    {
        var html = MarkdownConverter.ToHtml("- One\n* Two\n\n1. First\n2. Second");

        Assert.Equal(
            "<ul>\r\n<li>One</li>\r\n<li>Two</li>\r\n</ul>\r\n" +
            "<ol>\r\n<li>First</li>\r\n<li>Second</li>\r\n</ol>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_ConvertsBlockquotesAndFencedCode()
    {
        var html = MarkdownConverter.ToHtml(
            "> Quoted **text**\n```\n<tag>\n```");

        Assert.Equal(
            "<blockquote><p>Quoted <strong>text</strong></p></blockquote>\r\n" +
            "<pre><code>\r\n&lt;tag&gt;\r\n</code></pre>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_ClosesAnUnterminatedCodeBlock()
    {
        var html = MarkdownConverter.ToHtml("```\nvalue");

        Assert.Equal(
            "<pre><code>\r\nvalue\r\n</code></pre>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_ConvertsAllSupportedInlineFormatting()
    {
        var html = MarkdownConverter.ToHtml(
            "**bold** *italic* `code` ~~old~~ <u>under</u> " +
            "[site](https://example.com) ![logo](logo.png)");

        Assert.Equal(
            "<p><strong>bold</strong> <em>italic</em> <code>code</code> " +
            "<del>old</del> <u>under</u> " +
            "<a href=\"https://example.com\">site</a> " +
            "<img src=\"logo.png\" alt=\"logo\"></p>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_HandlesUnderscoreBoldAndEmphasis()
    {
        var html = MarkdownConverter.ToHtml("__bold__ _italic_");

        Assert.Equal(
            "<p><strong>bold</strong> <em>italic</em></p>\r\n",
            html);
    }
}
