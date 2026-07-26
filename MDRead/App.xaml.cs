using MDRead.Services;
using MDRead.ViewModels;
using System.Windows;

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
}
