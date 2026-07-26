namespace MDRead.Constants;

/// <summary>
/// Provides document-format and encoding constants.
/// </summary>
internal static class DocumentConstants
{
    /// <summary>Gets the line-feed character used by the editor and converter.</summary>
    public const char LineFeed = '\n';

    /// <summary>Gets the line-feed string used by the editor and converter.</summary>
    public const string LineFeedString = "\n";

    /// <summary>Gets the Windows line-ending sequence.</summary>
    public const string WindowsLineEnding = "\r\n";

    /// <summary>Gets the HTML file extension.</summary>
    public const string HtmlFileExtension = ".html";

    /// <summary>Gets whether saved UTF-8 documents include a byte-order mark.</summary>
    public const bool EmitUtf8ByteOrderMark = false;
}
