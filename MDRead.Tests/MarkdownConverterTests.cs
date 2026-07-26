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

    [Fact]
    public void ToHtml_ConvertsTablesWithColumnAlignment()
    {
        var html = MarkdownConverter.ToHtml(
            "| Critère | Note /5 | Poids | Contribution |\n" +
            "|---|---:|:---:|---:|\n" +
            "| Adéquation audience | 5 | 20 % | 20 |\n" +
            "| Démontrabilité | 1,5 | 10 % | 3 |");

        Assert.Equal(
            "<table>\r\n<thead>\r\n" +
            "<tr><th>Crit&#232;re</th><th style=\"text-align: right\">Note /5</th>" +
            "<th style=\"text-align: center\">Poids</th>" +
            "<th style=\"text-align: right\">Contribution</th></tr>\r\n" +
            "</thead>\r\n<tbody>\r\n" +
            "<tr><td>Ad&#233;quation audience</td><td style=\"text-align: right\">5</td>" +
            "<td style=\"text-align: center\">20 %</td>" +
            "<td style=\"text-align: right\">20</td></tr>\r\n" +
            "<tr><td>D&#233;montrabilit&#233;</td><td style=\"text-align: right\">1,5</td>" +
            "<td style=\"text-align: center\">10 %</td>" +
            "<td style=\"text-align: right\">3</td></tr>\r\n" +
            "</tbody>\r\n</table>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_HandlesEscapedPipesAndMissingTableCells()
    {
        var html = MarkdownConverter.ToHtml(
            "| Name | Description |\n" +
            "| --- | --- |\n" +
            "| A\\|B | `x|y` |\n" +
            "| Empty |");

        Assert.Equal(
            "<table>\r\n<thead>\r\n<tr><th>Name</th><th>Description</th></tr>\r\n" +
            "</thead>\r\n<tbody>\r\n" +
            "<tr><td>A|B</td><td><code>x|y</code></td></tr>\r\n" +
            "<tr><td>Empty</td><td></td></tr>\r\n" +
            "</tbody>\r\n</table>\r\n",
            html);
    }

    [Fact]
    public void ToHtml_LeavesPipeTextAsParagraphWithoutTableDelimiter()
    {
        var html = MarkdownConverter.ToHtml("| Not | a table |");

        Assert.Equal("<p>| Not | a table |</p>\r\n", html);
    }
}
