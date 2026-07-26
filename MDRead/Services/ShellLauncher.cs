using System.Diagnostics;

namespace MDRead.Services;

/// <summary>
/// Abstracts launching an approved target through the operating-system shell.
/// </summary>
internal interface IShellLauncher
{
    /// <summary>
    /// Attempts to launch a URL or file.
    /// </summary>
    /// <param name="target">The absolute URL or file path.</param>
    /// <returns>An error message on failure; otherwise, <see langword="null"/>.</returns>
    string? TryLaunch(string target);
}

/// <summary>
/// Launches targets with their operating-system default application.
/// </summary>
internal sealed class ShellLauncher : IShellLauncher
{
    /// <inheritdoc />
    public string? TryLaunch(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target)
            {
                UseShellExecute = true
            });
            return null;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return exception.Message;
        }
    }
}
