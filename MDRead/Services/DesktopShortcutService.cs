using MDRead.Constants;
using System.IO;

namespace MDRead.Services;

/// <summary>
/// Creates a desktop shortcut for the currently running application.
/// </summary>
internal sealed class DesktopShortcutService : IDesktopShortcutService
{
    /// <inheritdoc />
    public DesktopShortcutResult CreateDesktopShortcut()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return new DesktopShortcutResult(
                    null,
                    AppText.DesktopShortcutExecutableNotFound);
            }

            var desktopPath = Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(
                desktopPath,
                AppText.DesktopShortcutFileName);
            var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)!;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);

            shortcut.TargetPath = executablePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath)!;
            shortcut.IconLocation = string.Concat(executablePath, ",0");
            shortcut.Description = AppText.ApplicationName;
            shortcut.Save();

            return new DesktopShortcutResult(shortcutPath, null);
        }
        catch (Exception exception)
        {
            return new DesktopShortcutResult(null, exception.Message);
        }
    }
}

/// <summary>
/// Defines desktop-shortcut operations provided by the Windows shell.
/// </summary>
internal interface IDesktopShortcutService
{
    /// <summary>
    /// Creates or updates the desktop shortcut for the current application.
    /// </summary>
    /// <returns>The outcome of the shortcut operation.</returns>
    DesktopShortcutResult CreateDesktopShortcut();
}

/// <summary>
/// Represents the outcome of a desktop-shortcut operation.
/// </summary>
/// <param name="ShortcutPath">The shortcut path when creation succeeded.</param>
/// <param name="ErrorMessage">The error message when creation failed.</param>
internal sealed record DesktopShortcutResult(
    string? ShortcutPath,
    string? ErrorMessage);
