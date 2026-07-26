using MDRead.Constants;

namespace MDRead.Tests;

/// <summary>
/// Verifies that formatting commands delegate the correct Markdown fragments.
/// </summary>
public sealed class MainWindowViewModelFormattingTests
{
    [Fact]
    public void HeadingAndListCommands_RequestExpectedLinePrefixes()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.ApplyHeading1Command.Execute(null);
        viewModel.ApplyHeading2Command.Execute(null);
        viewModel.ApplyHeading3Command.Execute(null);
        viewModel.ApplyHeading4Command.Execute(null);
        viewModel.InsertUnorderedListCommand.Execute(null);
        viewModel.InsertOrderedListCommand.Execute(null);
        viewModel.InsertQuoteCommand.Execute(null);

        Assert.Equal(
            [
                MarkdownSyntax.Heading1,
                MarkdownSyntax.Heading2,
                MarkdownSyntax.Heading3,
                MarkdownSyntax.Heading4,
                MarkdownSyntax.UnorderedListItem,
                MarkdownSyntax.OrderedListItem,
                MarkdownSyntax.Quote
            ],
            context.Editor.PrefixRequests);
    }

    [Fact]
    public void InlineCommands_RequestExpectedWrappers()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.ApplyBoldCommand.Execute(null);
        viewModel.ApplyItalicCommand.Execute(null);
        viewModel.ApplyUnderlineCommand.Execute(null);
        viewModel.ApplyStrikethroughCommand.Execute(null);
        viewModel.InsertLinkCommand.Execute(null);
        viewModel.InsertImageCommand.Execute(null);
        viewModel.ApplyInlineCodeCommand.Execute(null);
        viewModel.InsertCodeBlockCommand.Execute(null);
        viewModel.InsertHorizontalRuleCommand.Execute(null);

        Assert.Equal(
            [
                (MarkdownSyntax.Bold, MarkdownSyntax.Bold),
                (MarkdownSyntax.Italic, MarkdownSyntax.Italic),
                (MarkdownSyntax.UnderlineOpen, MarkdownSyntax.UnderlineClose),
                (MarkdownSyntax.Strikethrough, MarkdownSyntax.Strikethrough),
                (MarkdownSyntax.LinkOpen, MarkdownSyntax.LinkClose),
                (MarkdownSyntax.ImageOpen, MarkdownSyntax.ImageClose),
                (MarkdownSyntax.InlineCode, MarkdownSyntax.InlineCode),
                (MarkdownSyntax.CodeBlockOpen, MarkdownSyntax.CodeBlockClose),
                (MarkdownSyntax.HorizontalRule, string.Empty)
            ],
            context.Editor.WrapRequests);
    }
}
