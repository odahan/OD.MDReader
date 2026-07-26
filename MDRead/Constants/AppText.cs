namespace MDRead.Constants;

/// <summary>
/// Provides reusable application messages, titles, filters, and file names.
/// </summary>
internal static class AppText
{
    /// <summary>Gets the application name.</summary>
    public const string ApplicationName = "MDRead";

    /// <summary>Gets the window title used for a new document.</summary>
    public const string UntitledWindowTitle = "MDRead — Untitled";

    /// <summary>Gets the window title format for an open document.</summary>
    public const string WindowTitleFormat = "MDRead — {0}";

    /// <summary>Gets the initial status message.</summary>
    public const string ReadyStatus = "Open a Markdown file to begin.";

    /// <summary>Gets the status message for a new document.</summary>
    public const string NewDocumentStatus = "New document.";

    /// <summary>Gets the status format displayed after saving.</summary>
    public const string SavedStatusFormat = "Saved: {0}";

    /// <summary>Gets the status format displayed after exporting.</summary>
    public const string ExportedStatusFormat = "Exported: {0}";

    /// <summary>Gets the placeholder displayed when no recent files exist.</summary>
    public const string NoRecentFiles = "(No recent files)";

    /// <summary>Gets the Markdown open-dialog filter.</summary>
    public const string OpenMarkdownFilter = "Markdown files|*.md;*.markdown|All files|*.*";

    /// <summary>Gets the Markdown save-dialog filter.</summary>
    public const string SaveMarkdownFilter = "Markdown files|*.md|All files|*.*";

    /// <summary>Gets the HTML export-dialog filter.</summary>
    public const string ExportHtmlFilter = "HTML files|*.html";

    /// <summary>Gets the suggested name for a new Markdown file.</summary>
    public const string DefaultMarkdownFileName = "document.md";

    /// <summary>Gets the suggested name for an exported HTML file.</summary>
    public const string DefaultHtmlFileName = "document.html";

    /// <summary>Gets the default HTML document title.</summary>
    public const string DefaultDocumentTitle = "MDRead document";

    /// <summary>Gets the prompt shown before replacing an unsaved document.</summary>
    public const string ConfirmDiscardChanges =
        "Save your Markdown changes before continuing?";

    /// <summary>Gets the prompt shown before closing an unsaved document.</summary>
    public const string ConfirmSaveBeforeClosing =
        "Save your Markdown changes before closing?";

    /// <summary>Gets the preview initialization failure format.</summary>
    public const string PreviewInitializationFailedFormat =
        "The preview engine (WebView2 runtime) could not start.\n\n{0}";

    /// <summary>Gets the prompt displayed when the WebView2 runtime is unavailable.</summary>
    public const string WebView2RuntimeMissing =
        "WebView2 is required to display the preview, but it is not installed.\n\nWould you like to open the official Microsoft download page?";

    /// <summary>Gets the error displayed when the WebView2 download page cannot be opened.</summary>
    public const string WebView2DownloadPageFailedFormat =
        "Could not open the official Microsoft WebView2 download page.\n\n{0}";

    /// <summary>Gets the message shown when startup cannot complete.</summary>
    public const string StartupFailedFormat =
        "MDRead could not start and will close.\n\nDetails for support:\n{0}";

    /// <summary>Gets the message shown when an unexpected application error occurs.</summary>
    public const string UnexpectedErrorFormat =
        "MDRead encountered an unexpected error and will close.\n\nDetails for support:\n{0}";

    /// <summary>Gets the diagnostic details included in unexpected-error messages.</summary>
    public const string ErrorDiagnosticFormat =
        "Type: {0}\nCode: 0x{1:X8}\nMessage: {2}";

    /// <summary>Gets the save failure format.</summary>
    public const string SaveFailedFormat = "Could not save the file.\n\n{0}";

    /// <summary>Gets the export failure format.</summary>
    public const string ExportFailedFormat = "Could not export the file.\n\n{0}";

    /// <summary>Gets the external-link confirmation format.</summary>
    public const string ConfirmOpenLinkFormat =
        "Open this link in your default browser?\n\n{0}";

    /// <summary>Gets the local-file confirmation format.</summary>
    public const string ConfirmOpenFileFormat =
        "Open this file with its default application?\n\n{0}";

    /// <summary>Gets the blocked-target message format.</summary>
    public const string BlockedTargetFormat =
        "This link was blocked for security reasons:\n\n{0}";

    /// <summary>Gets the About dialog title.</summary>
    public const string AboutTitle = "About MDRead";

    /// <summary>Gets the About dialog content format.</summary>
    public const string AboutMessageFormat =
        "MDRead {0}\nMarkdown reader, editor and standalone HTML exporter.\n\n" +
        "Freeware — Olivier Dahan © 2026";

    /// <summary>Gets the About dialog content.</summary>
    public static string AboutMessage =>
        string.Format(AboutMessageFormat, ApplicationConstants.Version);

    /// <summary>Gets the label of the confirmation button.</summary>
    public const string OkButton = "OK";

    /// <summary>Gets the label of the affirmative button.</summary>
    public const string YesButton = "Yes";

    /// <summary>Gets the label of the negative button.</summary>
    public const string NoButton = "No";

    /// <summary>Gets the label of the cancel button.</summary>
    public const string CancelButton = "Cancel";

    /// <summary>Gets the error raised when a second view model is attached.</summary>
    public const string ViewModelAlreadyAttached =
        "A view model has already been attached to this window.";

    /// <summary>Gets the error raised when the view model is accessed before attachment.</summary>
    public const string ViewModelNotAttached =
        "The main window view model has not been attached.";
}
