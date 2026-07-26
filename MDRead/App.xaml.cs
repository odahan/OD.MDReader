using MDRead.Constants;
using MDRead.Services;
using MDRead.ViewModels;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Threading;

namespace MDRead;

/// <summary>
/// Defines the application entry point and composes the main window dependencies.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Creates and displays the main application window.
    /// </summary>
    /// <param name="sender">The application instance.</param>
    /// <param name="eventArgs">The startup arguments.</param>
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            if (!IsWebView2RuntimeAvailable())
            {
                ShowWebView2InstallationPrompt();
                Shutdown();
                return;
            }

            var window = new MainWindow();
            MainWindowViewModel? viewModel = null;
            var dialogService = new DialogService(window, () => viewModel?.IsDarkTheme ?? true);

            viewModel = new MainWindowViewModel(
                dialogService,
                window,
                new ShellLauncher(),
                new JsonSettingsService(),
                e.Args);

            window.AttachViewModel(viewModel);
            MainWindow = window;
            window.Show();
        }
        catch (Exception exception)
        {
            ShowFatalError(AppText.StartupFailedFormat, exception);
            Shutdown(-1);
        }
    }

    private void Application_DispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        ShowFatalError(AppText.UnexpectedErrorFormat, eventArgs.Exception);
        Shutdown(-1);
    }

    private static bool IsWebView2RuntimeAvailable()
    {
        try
        {
            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
            return true;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            return false;
        }
    }

    private static void ShowWebView2InstallationPrompt()
    {
        var result = MessageBox.Show(
            AppText.WebView2RuntimeMissing,
            AppText.ApplicationName,
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var errorMessage = new ShellLauncher().TryLaunch(WebView2Constants.DownloadUrl);
        if (errorMessage is not null)
        {
            MessageBox.Show(
                string.Format(AppText.WebView2DownloadPageFailedFormat, errorMessage),
                AppText.ApplicationName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void ShowFatalError(string messageFormat, Exception exception)
    {
        var rootCause = exception.GetBaseException();
        var diagnosticDetails = string.Format(
            AppText.ErrorDiagnosticFormat,
            rootCause.GetType().FullName ?? rootCause.GetType().Name,
            unchecked((uint)rootCause.HResult),
            rootCause.Message);

        MessageBox.Show(
            string.Format(messageFormat, diagnosticDetails),
            AppText.ApplicationName,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
