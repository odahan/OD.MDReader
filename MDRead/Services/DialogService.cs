using MDRead.Constants;
using Microsoft.Win32;
using System.Windows;

namespace MDRead.Services;

/// <summary>
/// Provides themed WPF dialogs and system file pickers.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private readonly Window _owner;
    private readonly Func<bool> _isDarkThemeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    /// <param name="owner">The owner of modal dialogs.</param>
    /// <param name="isDarkThemeProvider">Provides the current theme selection.</param>
    public DialogService(Window owner, Func<bool> isDarkThemeProvider)
    {
        _owner = owner;
        _isDarkThemeProvider = isDarkThemeProvider;
    }

    /// <inheritdoc />
    public void ShowMessage(string message, string? title = null) =>
        ThemedDialog.Show(
            _owner,
            message,
            title ?? AppText.ApplicationName,
            DialogButtonSet.Ok,
            _isDarkThemeProvider());

    /// <inheritdoc />
    public bool Confirm(string message, string? title = null) =>
        ThemedDialog.Show(
            _owner,
            message,
            title ?? AppText.ApplicationName,
            DialogButtonSet.YesNo,
            _isDarkThemeProvider()) == MessageBoxResult.Yes;

    /// <inheritdoc />
    public DialogChoice ConfirmSave(string message, string? title = null)
    {
        var result = ThemedDialog.Show(
            _owner,
            message,
            title ?? AppText.ApplicationName,
            DialogButtonSet.YesNoCancel,
            _isDarkThemeProvider());

        return result switch
        {
            MessageBoxResult.Yes => DialogChoice.Yes,
            MessageBoxResult.No => DialogChoice.No,
            _ => DialogChoice.Cancel
        };
    }

    /// <inheritdoc />
    public string? PickFileToOpen(string filter, string? initialDirectory)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            InitialDirectory = initialDirectory
        };

        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? PickFileToSave(
        string filter,
        string suggestedFileName,
        string? initialDirectory)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = suggestedFileName,
            InitialDirectory = initialDirectory
        };

        return dialog.ShowDialog(_owner) == true ? dialog.FileName : null;
    }
}
