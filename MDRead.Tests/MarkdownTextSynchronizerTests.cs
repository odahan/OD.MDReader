using MDRead.Markdown;

namespace MDRead.Tests;

/// <summary>
/// Verifies text normalization and source-block matching used by manual synchronization.
/// </summary>
public sealed class MarkdownTextSynchronizerTests
{
    [Fact]
    public void NormalizeMarkdown_RemovesFormattingAccentsAndPunctuation()
    {
        var normalized = MarkdownTextSynchronizer.NormalizeMarkdown(
            "## L'été — c'est **déjà** l'heure !");

        Assert.Equal("letecestdejalheure", normalized);
    }

    [Fact]
    public void CreateAnchor_UsesTheMarkdownBlockAtTheCaret()
    {
        const string markdown = "# First\n\nSecond **section**.\n\nThird section.";
        var caret = markdown.IndexOf("section", StringComparison.Ordinal);

        var anchor = MarkdownTextSynchronizer.CreateAnchor(markdown, caret);

        Assert.NotNull(anchor);
        Assert.Equal("secondsection", anchor.Text);
        Assert.Equal("first", anchor.Previous);
        Assert.Equal("thirdsection", anchor.Next);
    }

    [Fact]
    public void FindMatchingBlock_UsesNeighbouringTextToResolveDuplicates()
    {
        const string markdown = "# First\n\nRepeated text.\n\n# Second\n\nRepeated text.\n\nFinal text.";
        var anchor = new TextSynchronizationAnchor(
            "repeatedtext",
            "second",
            "finaltext");

        var block = MarkdownTextSynchronizer.FindMatchingBlock(markdown, anchor);

        Assert.NotNull(block);
        Assert.Equal(markdown.IndexOf("Repeated text.", 20, StringComparison.Ordinal), block.Start);
    }

    [Fact]
    public void FindMatchingBlock_ReturnsNullForAmbiguousTextWithoutContext()
    {
        const string markdown = "Repeated text.\n\nRepeated text.";
        var anchor = new TextSynchronizationAnchor("repeatedtext", null, null);

        var block = MarkdownTextSynchronizer.FindMatchingBlock(markdown, anchor);

        Assert.Null(block);
    }
}
