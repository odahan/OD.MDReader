using System.IO;

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

    /// <summary>Gets the standard Markdown file extension.</summary>
    public const string MarkdownFileExtension = ".md";

    /// <summary>Gets the alternate Markdown file extension.</summary>
    public const string AlternateMarkdownFileExtension = ".markdown";

    /// <summary>Gets whether saved UTF-8 documents include a byte-order mark.</summary>
    public const bool EmitUtf8ByteOrderMark = false;

    /// <summary>
    /// Determines whether a path has a supported Markdown extension.
    /// </summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> when the path points to a Markdown document.</returns>
    public static bool IsMarkdownFilePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(MarkdownFileExtension, StringComparison.OrdinalIgnoreCase)
            || extension.Equals(
                AlternateMarkdownFileExtension,
                StringComparison.OrdinalIgnoreCase);
    }
}
