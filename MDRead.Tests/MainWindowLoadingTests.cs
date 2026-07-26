using System.Windows;

namespace MDRead.Tests;

/// <summary>
/// Verifies that compiled WPF resources can construct the main window at runtime.
/// </summary>
public sealed class MainWindowLoadingTests
{
    [Fact]
    public void Constructor_LoadsCompiledXamlWithoutException()
    {
        Exception? capturedException = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = new App();
                application.InitializeComponent();
                var window = new MainWindow();
                window.Close();
            }
            catch (Exception exception)
            {
                capturedException = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));

        Assert.Null(capturedException);
    }
}
