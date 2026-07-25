using System.Windows;

namespace MDRead;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var window = new MainWindow(e.Args);
        MainWindow = window;
        window.Show();
    }
}
