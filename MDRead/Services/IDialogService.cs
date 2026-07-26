namespace MDRead.Services;

/// <summary>
/// Identifies the choices returned by a confirmation dialog.
/// </summary>
internal enum DialogChoice
{
    /// <summary>The user accepted the operation.</summary>
    Yes,

    /// <summary>The user declined the operation.</summary>
    No,

    /// <summary>The user canceled the operation.</summary>
    Cancel
}

/// <summary>
/// Abstracts modal messages and file pickers from the view model.
/// </summary>
internal interface IDialogService
{
    /// <summary>
    /// Displays an informational message.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The optional dialog title.</param>
    void ShowMessage(string message, string? title = null);

    /// <summary>
    /// Displays a yes-or-no confirmation.
    /// </summary>
    /// <param name="message">The question to display.</param>
    /// <param name="title">The optional dialog title.</param>
    /// <returns><see langword="true"/> when Yes is selected.</returns>
    bool Confirm(string message, string? title = null);

    /// <summary>
    /// Displays a yes, no, or cancel confirmation.
    /// </summary>
    /// <param name="message">The question to display.</param>
    /// <param name="title">The optional dialog title.</param>
    /// <returns>The selected choice.</returns>
    DialogChoice ConfirmSave(string message, string? title = null);

    /// <summary>
    /// Prompts the user to select an existing file.
    /// </summary>
    /// <param name="filter">The file-extension filter.</param>
    /// <param name="initialDirectory">The initial directory, if available.</param>
    /// <returns>The selected path, or <see langword="null"/> when canceled.</returns>
    string? PickFileToOpen(string filter, string? initialDirectory);

    /// <summary>
    /// Prompts the user to select a save location.
    /// </summary>
    /// <param name="filter">The file-extension filter.</param>
    /// <param name="suggestedFileName">The suggested file name.</param>
    /// <param name="initialDirectory">The initial directory, if available.</param>
    /// <returns>The selected path, or <see langword="null"/> when canceled.</returns>
    string? PickFileToSave(
        string filter,
        string suggestedFileName,
        string? initialDirectory);
}
