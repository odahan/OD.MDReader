using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MDRead;

internal enum DialogButtons { Ok, YesNo, YesNoCancel }

internal static class ThemedDialog
{
    public static MessageBoxResult Show(Window owner, string message, string title, DialogButtons buttons, bool dark)
    {
        var background = (Brush)new BrushConverter().ConvertFromString(dark ? "#252526" : "#FFFFFF")!;
        var foreground = (Brush)new BrushConverter().ConvertFromString(dark ? "#F1F1F1" : "#15171A")!;
        var buttonBackground = (Brush)new BrushConverter().ConvertFromString(dark ? "#3E3E42" : "#E1E6ED")!;
        var buttonBorder = (Brush)new BrushConverter().ConvertFromString(dark ? "#777777" : "#8995A5")!;
        var dialog = new Window
        {
            Title = title, Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight, MinWidth = 390, MaxWidth = 640,
            ResizeMode = ResizeMode.NoResize, Background = background, Foreground = foreground,
            ShowInTaskbar = false
        };
        var panel = new DockPanel { Margin = new Thickness(24, 20, 24, 18) };
        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 22, 0, 0) };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        panel.Children.Add(buttonPanel);
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14, MinWidth = 340 });
        dialog.Content = panel;
        MessageBoxResult result = buttons == DialogButtons.YesNoCancel ? MessageBoxResult.Cancel : MessageBoxResult.None;
        void AddButton(string label, MessageBoxResult value, bool primary = false)
        {
            var buttonForeground = primary || dark ? Brushes.White : (Brush)new BrushConverter().ConvertFromString("#15171A")!;
            var button = new Button { Content = label, MinWidth = 82, Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(8, 0, 0, 0), Background = primary ? new SolidColorBrush(dark ? Color.FromRgb(0, 94, 160) : Color.FromRgb(0, 96, 170)) : buttonBackground, BorderBrush = buttonBorder, Foreground = buttonForeground };
            button.Click += (_, _) => { result = value; dialog.DialogResult = true; };
            buttonPanel.Children.Add(button);
        }
        if (buttons == DialogButtons.Ok) AddButton("OK", MessageBoxResult.OK, true);
        else { AddButton("Yes", MessageBoxResult.Yes, true); AddButton("No", MessageBoxResult.No); if (buttons == DialogButtons.YesNoCancel) AddButton("Cancel", MessageBoxResult.Cancel); }
        dialog.ShowDialog();
        return result;
    }
}
