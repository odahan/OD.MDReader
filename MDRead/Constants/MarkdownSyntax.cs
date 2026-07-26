namespace MDRead.Constants;

/// <summary>
/// Provides Markdown fragments inserted by editor commands.
/// </summary>
internal static class MarkdownSyntax
{
    /// <summary>Gets the placeholder used when the editor selection is empty.</summary>
    public const string PlaceholderText = "text";

    /// <summary>Gets the level-one heading prefix.</summary>
    public const string Heading1 = "# ";

    /// <summary>Gets the level-two heading prefix.</summary>
    public const string Heading2 = "## ";

    /// <summary>Gets the level-three heading prefix.</summary>
    public const string Heading3 = "### ";

    /// <summary>Gets the level-four heading prefix.</summary>
    public const string Heading4 = "#### ";

    /// <summary>Gets the bold delimiter.</summary>
    public const string Bold = "**";

    /// <summary>Gets the italic delimiter.</summary>
    public const string Italic = "*";

    /// <summary>Gets the opening underline tag.</summary>
    public const string UnderlineOpen = "<u>";

    /// <summary>Gets the closing underline tag.</summary>
    public const string UnderlineClose = "</u>";

    /// <summary>Gets the strikethrough delimiter.</summary>
    public const string Strikethrough = "~~";

    /// <summary>Gets the opening link fragment.</summary>
    public const string LinkOpen = "[";

    /// <summary>Gets the closing link fragment.</summary>
    public const string LinkClose = "](https://)";

    /// <summary>Gets the opening image fragment.</summary>
    public const string ImageOpen = "![description](";

    /// <summary>Gets the closing image fragment.</summary>
    public const string ImageClose = ")";

    /// <summary>Gets the unordered-list prefix.</summary>
    public const string UnorderedListItem = "- ";

    /// <summary>Gets the ordered-list prefix.</summary>
    public const string OrderedListItem = "1. ";

    /// <summary>Gets the inline-code delimiter.</summary>
    public const string InlineCode = "`";

    /// <summary>Gets the opening fenced-code fragment.</summary>
    public const string CodeBlockOpen = "```\n";

    /// <summary>Gets the closing fenced-code fragment.</summary>
    public const string CodeBlockClose = "\n```";

    /// <summary>Gets the blockquote prefix.</summary>
    public const string Quote = "> ";

    /// <summary>Gets the horizontal-rule fragment.</summary>
    public const string HorizontalRule = "\n---\n";
}
