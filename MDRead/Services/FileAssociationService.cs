using MDRead.Constants;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;

namespace MDRead.Services;

/// <summary>
/// Registers the supported Markdown file extensions for the current user.
/// </summary>
internal sealed class FileAssociationService : IFileAssociationService
{
    private const string UserClassesRegistryPath = "Software\\Classes";
    private const uint AssociationChangedNotification = 0x08000000;
    private const uint ShellItemListNotification = 0;

    /// <inheritdoc />
    public FileAssociationResult AssociateMarkdownFiles()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return new FileAssociationResult(
                    false,
                    AppText.FileAssociationExecutableNotFound);
            }

            RegisterFileExtension(DocumentConstants.MarkdownFileExtension);
            RegisterFileExtension(DocumentConstants.AlternateMarkdownFileExtension);
            RegisterApplication(executablePath);
            NotifyShellOfAssociationChange();

            return new FileAssociationResult(true, null);
        }
        catch (Exception exception)
        {
            return new FileAssociationResult(false, exception.Message);
        }
    }

    /// <inheritdoc />
    public bool IsMarkdownAssociationRegistered()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return false;
        }

        return IsExtensionRegistered(DocumentConstants.MarkdownFileExtension)
            && IsExtensionRegistered(DocumentConstants.AlternateMarkdownFileExtension)
            && IsApplicationRegistered(executablePath);
    }

    private static void RegisterFileExtension(string extension)
    {
        using var extensionKey = Registry.CurrentUser.CreateSubKey(
            string.Concat(UserClassesRegistryPath, "\\", extension),
            writable: true)
            ?? throw new InvalidOperationException(AppText.FileAssociationRegistryUnavailable);
        extensionKey.SetValue(string.Empty, FileAssociationConstants.MarkdownProgId);

        using var openWithKey = extensionKey.CreateSubKey("OpenWithProgids", writable: true)
            ?? throw new InvalidOperationException(AppText.FileAssociationRegistryUnavailable);
        openWithKey.SetValue(
            FileAssociationConstants.MarkdownProgId,
            string.Empty,
            RegistryValueKind.String);
    }

    private static void RegisterApplication(string executablePath)
    {
        using var applicationKey = Registry.CurrentUser.CreateSubKey(
            string.Concat(
                UserClassesRegistryPath,
                "\\",
                FileAssociationConstants.MarkdownProgId),
            writable: true)
            ?? throw new InvalidOperationException(AppText.FileAssociationRegistryUnavailable);
        applicationKey.SetValue(string.Empty, AppText.MarkdownFileTypeDescription);

        using var iconKey = applicationKey.CreateSubKey("DefaultIcon", writable: true)
            ?? throw new InvalidOperationException(AppText.FileAssociationRegistryUnavailable);
        iconKey.SetValue(string.Empty, string.Concat(executablePath, ",0"));

        using var commandKey = applicationKey.CreateSubKey(
            "shell\\open\\command",
            writable: true)
            ?? throw new InvalidOperationException(AppText.FileAssociationRegistryUnavailable);
        commandKey.SetValue(string.Empty, BuildOpenCommand(executablePath));
    }

    private static bool IsExtensionRegistered(string extension) =>
        string.Equals(
            GetDefaultRegistryValue(string.Concat(
                UserClassesRegistryPath,
                "\\",
                extension)),
            FileAssociationConstants.MarkdownProgId,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsApplicationRegistered(string executablePath) =>
        string.Equals(
            GetDefaultRegistryValue(string.Concat(
                UserClassesRegistryPath,
                "\\",
                FileAssociationConstants.MarkdownProgId,
                "\\DefaultIcon")),
            string.Concat(executablePath, ",0"),
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            GetDefaultRegistryValue(string.Concat(
                UserClassesRegistryPath,
                "\\",
                FileAssociationConstants.MarkdownProgId,
                "\\shell\\open\\command")),
            BuildOpenCommand(executablePath),
            StringComparison.OrdinalIgnoreCase);

    private static string? GetDefaultRegistryValue(string registryPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(registryPath, writable: false);
        return key?.GetValue(string.Empty) as string;
    }

    private static string BuildOpenCommand(string executablePath) => string.Concat(
        "\"",
        executablePath,
        "\" \"%1\"");

    private static void NotifyShellOfAssociationChange() =>
        SHChangeNotify(
            AssociationChangedNotification,
            ShellItemListNotification,
            IntPtr.Zero,
            IntPtr.Zero);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(
        uint eventId,
        uint flags,
        IntPtr item1,
        IntPtr item2);
}

/// <summary>
/// Defines file-association operations for the current user.
/// </summary>
internal interface IFileAssociationService
{
    /// <summary>
    /// Associates the supported Markdown file extensions with the current application.
    /// </summary>
    /// <returns>The outcome of the association operation.</returns>
    FileAssociationResult AssociateMarkdownFiles();

    /// <summary>
    /// Determines whether the current executable is fully registered for the supported Markdown extensions.
    /// </summary>
    /// <returns><see langword="true"/> when the live registry entries match the current executable.</returns>
    bool IsMarkdownAssociationRegistered();
}

/// <summary>
/// Represents the outcome of a Markdown file-association operation.
/// </summary>
/// <param name="IsSuccessful">Whether the registration succeeded.</param>
/// <param name="ErrorMessage">The error message when registration failed.</param>
internal sealed record FileAssociationResult(bool IsSuccessful, string? ErrorMessage);
