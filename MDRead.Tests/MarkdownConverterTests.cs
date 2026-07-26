using MDRead.Markdown;

namespace MDRead.Tests;

/// <summary>
/// Verifies supported block and inline Markdown conversion.
/// </summary>
public sealed class MarkdownConverterTests
{
    [Fact]
    public void ToHtml_EncodesUnsafeTextAndPreservesParagraphLines()
    {
        var html = MarkdownConverter.ToHtml("Hello <world>\r\ncontinued");

        Assert.Contains("Hello &lt;world&gt;", html);
        Assert.Contains("continued", html);
        Assert.DoesNotContain("<world>", html);
    }

    [Fact]
    public void ToHtml_ConvertsHeadingsAndHorizontalRules()
    {
        var html = MarkdownConverter.ToHtml("# Title\n\n### Section\n---\n***");

        Assert.Contains("<h1 id=\"title\">Title</h1>", html);
        Assert.Contains("<h3 id=\"section\">Section</h3>", html);
        Assert.Equal(2, CountOccurrences(html, "<hr />"));
    }

    [Fact]
    public void ToHtml_ConvertsAndClosesBothListTypes()
    {
        var html = MarkdownConverter.ToHtml("- One\n- Two\n\n1. First\n2. Second");

        Assert.Contains("<ul>", html);
        Assert.Contains("<li>One</li>", html);
        Assert.Contains("<li>Two</li>", html);
        Assert.Contains("</ul>", html);
        Assert.Contains("<ol>", html);
        Assert.Contains("<li>First</li>", html);
        Assert.Contains("<li>Second</li>", html);
        Assert.Contains("</ol>", html);
    }

    [Fact]
    public void ToHtml_ConvertsBlockquotesAndFencedCode()
    {
        var html = MarkdownConverter.ToHtml(
            "> Quoted **text**\n```\n<tag>\n```");

        Assert.Contains("<blockquote>", html);
        Assert.Contains("Quoted <strong>text</strong>", html);
        Assert.Contains("</blockquote>", html);
        Assert.Contains("<pre><code>&lt;tag&gt;", html);
        Assert.Contains("</code></pre>", html);
    }

    [Fact]
    public void ToHtml_ClosesAnUnterminatedCodeBlock()
    {
        var html = MarkdownConverter.ToHtml("```\nvalue");

        Assert.Contains("<pre><code>value", html);
        Assert.Contains("</code></pre>", html);
    }

    [Fact]
    public void ToHtml_ConvertsAllSupportedInlineFormatting()
    {
        var html = MarkdownConverter.ToHtml(
            "**bold** *italic* `code` ~~old~~ <u>under</u> " +
            "[site](https://example.com) ![logo](logo.png)");

        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
        Assert.Contains("<code>code</code>", html);
        Assert.Contains("<del>old</del>", html);
        Assert.Contains("<u>under</u>", html);
        Assert.Contains("<a href=\"https://example.com\">site</a>", html);
        Assert.Contains("<img src=\"logo.png\" alt=\"logo\" />", html);
    }

    [Fact]
    public void ToHtml_HandlesUnderscoreBoldAndEmphasis()
    {
        var html = MarkdownConverter.ToHtml("__bold__ _italic_");

        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public void ToHtml_ConvertsTablesWithColumnAlignment()
    {
        var html = MarkdownConverter.ToHtml(
            "| Critère | Note /5 | Poids | Contribution |\n" +
            "|---|---:|:---:|---:|\n" +
            "| Adéquation audience | 5 | 20 % | 20 |\n" +
            "| Démontrabilité | 1,5 | 10 % | 3 |");

        Assert.Contains("<table>", html);
        Assert.Contains("<th>Critère</th>", html);
        Assert.Contains("<th style=\"text-align: right;\">Note /5</th>", html);
        Assert.Contains("<th style=\"text-align: center;\">Poids</th>", html);
        Assert.Contains("<td>Adéquation audience</td>", html);
        Assert.Contains("<td style=\"text-align: right;\">1,5</td>", html);
        Assert.Contains("<td style=\"text-align: center;\">10 %</td>", html);
        Assert.Contains("</table>", html);
    }

    [Fact]
    public void ToHtml_ConvertsGridTables()
    {
        var html = MarkdownConverter.ToHtml(
            "+----------------------+------------------------------------------+\n" +
            "| Item                 | Description                              |\n" +
            "+======================+==========================================+\n" +
            "| Lorem ipsum          | Dolor sit amet, consectetur adipiscing.  |\n" +
            "+----------------------+------------------------------------------+");

        Assert.Contains("<table>", html);
        Assert.Contains("<th>Item</th>", html);
        Assert.Contains("<th>Description</th>", html);
        Assert.Contains("<td>Lorem ipsum</td>", html);
        Assert.Contains("<td>Dolor sit amet, consectetur adipiscing.</td>", html);
    }

    [Fact]
    public void ToHtml_ConvertsExtendedEmphasis()
    {
        var html = MarkdownConverter.ToHtml(
            "~~old~~ ~sub~ ^sup^ ++inserted++ ==marked==");

        Assert.Contains("<del>old</del>", html);
        Assert.Contains("<sub>sub</sub>", html);
        Assert.Contains("<sup>sup</sup>", html);
        Assert.Contains("<ins>inserted</ins>", html);
        Assert.Contains("<mark>marked</mark>", html);
    }

    [Fact]
    public void ToHtml_HandlesEscapedPipesAndMissingTableCells()
    {
        var html = MarkdownConverter.ToHtml(
            "| Name | Description |\n" +
            "| --- | --- |\n" +
            "| A\\|B | `x|y` |\n" +
            "| Empty |");

        Assert.Contains("<td>A|B</td>", html);
        Assert.Contains("<td><code>x|y</code></td>", html);
        Assert.Contains("<td>Empty</td>", html);
        Assert.Contains("<td></td>", html);
    }

    [Fact]
    public void ToHtml_LeavesPipeTextAsParagraphWithoutTableDelimiter()
    {
        var html = MarkdownConverter.ToHtml("| Not | a table |");

        Assert.Contains("<p>| Not | a table |</p>", html);
        Assert.DoesNotContain("<table>", html);
    }

    [Fact]
    public void ToHtml_EscapesRawHtmlButAllowsToolbarUnderline()
    {
        var html = MarkdownConverter.ToHtml(
            "<script>alert('x')</script> <img src=x onerror=alert(1)> " +
            "<u>safe</u> [unsafe](javascript:alert('x'))");

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.DoesNotContain("href=\"javascript:", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html);
        Assert.Contains("<u>safe</u>", html);
    }

    [Fact]
    public void ToHtml_DoesNotInterpretMarkdownOrUnderlineInsideCode()
    {
        var html = MarkdownConverter.ToHtml("`*literal* <u>code</u>`");

        Assert.Contains(
            "<code>*literal* &lt;u&gt;code&lt;/u&gt;</code>",
            html);
        Assert.DoesNotContain("<em>literal</em>", html);
    }

    [Fact]
    public void ToHtml_PreservesRelativeMarkdownLinks()
    {
        var html = MarkdownConverter.ToHtml(
            "[Next chapter](chapters/next.md#summary)");

        Assert.Contains(
            "<a href=\"chapters/next.md#summary\">Next chapter</a>",
            html);
    }

    [Fact]
    public void ToHtml_ConvertsSelectedMarkdigExtensions()
    {
        var html = MarkdownConverter.ToHtml(
            "- [x] Done\n\n" +
            "Visit https://example.com.\n\n" +
            "Term\n:   Definition\n\n" +
            "Text with a note.[^1]\n\n" +
            "[^1]: Footnote");

        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("checked=\"checked\"", html);
        Assert.Contains("<a href=\"https://example.com\">https://example.com</a>", html);
        Assert.Contains("<dl>", html);
        Assert.Contains("<dt>Term</dt>", html);
        Assert.Contains("<dd>Definition</dd>", html);
        Assert.Contains("class=\"footnotes\"", html);
        Assert.Contains("Footnote", html);
    }

    private static int CountOccurrences(string value, string expected)
    {
        var count = 0;
        var startIndex = 0;

        while ((startIndex = value.IndexOf(
                   expected,
                   startIndex,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += expected.Length;
        }

        return count;
    }
}
