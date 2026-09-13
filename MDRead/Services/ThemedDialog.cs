using MDRead.Constants;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MDRead.Services;

/// <summary>
/// Identifies the button set displayed by a themed dialog.
/// </summary>
internal enum DialogButtonSet
{
    /// <summary>Displays a single confirmation button.</summary>
    Ok,

    /// <summary>Displays affirmative and negative buttons.</summary>
    YesNo,

    /// <summary>Displays affirmative, negative, and cancel buttons.</summary>
    YesNoCancel
}

/// <summary>
/// Creates modal dialogs that match the current application theme.
/// </summary>
internal static class ThemedDialog
{
    /// <summary>
    /// Displays a themed modal dialog.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="buttonSet">The buttons to display.</param>
    /// <param name="isDarkTheme">Whether the dark theme is active.</param>
    /// <param name="showApplicationIcon">Whether to display the application icon above the message.</param>
    /// <returns>The selected button result.</returns>
    public static MessageBoxResult Show(
        Window owner,
        string message,
        string title,
        DialogButtonSet buttonSet,
        bool isDarkTheme,
        bool showApplicationIcon = false)
    {
        var palette = ThemePalette.Select(isDarkTheme);
        var result = buttonSet == DialogButtonSet.YesNoCancel
            ? MessageBoxResult.Cancel
            : MessageBoxResult.None;
        var dialog = CreateDialog(owner, title, palette);
        var contentPanel = CreateContentPanel();
        var buttonPanel = CreateButtonPanel();

        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        contentPanel.Children.Add(buttonPanel);
        contentPanel.Children.Add(CreateMessageContent(message, showApplicationIcon));
        dialog.Content = contentPanel;

        void AddButton(
            string label,
            MessageBoxResult buttonResult,
            bool isPrimary = false)
        {
            var button = CreateButton(label, isPrimary, palette);
            button.Click += (_, _) =>
            {
                result = buttonResult;
                dialog.DialogResult = true;
            };
            buttonPanel.Children.Add(button);
        }

        if (buttonSet == DialogButtonSet.Ok)
        {
            AddButton(AppText.OkButton, MessageBoxResult.OK, true);
        }
        else
        {
            AddButton(AppText.YesButton, MessageBoxResult.Yes, true);
            AddButton(AppText.NoButton, MessageBoxResult.No);

            if (buttonSet == DialogButtonSet.YesNoCancel)
            {
                AddButton(AppText.CancelButton, MessageBoxResult.Cancel);
            }
        }

        dialog.ShowDialog();
        return result;
    }

    private static Window CreateDialog(
        Window owner,
        string title,
        ThemePalette palette) =>
        new()
        {
            Title = title,
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = UiLayoutConstants.DialogMinimumWidth,
            MaxWidth = UiLayoutConstants.DialogMaximumWidth,
            ResizeMode = ResizeMode.NoResize,
            Background = ThemeBrushFactory.Create(palette.DialogBackground),
            Foreground = ThemeBrushFactory.Create(palette.DialogForeground),
            ShowInTaskbar = false
        };

    private static DockPanel CreateContentPanel() =>
        new()
        {
            Margin = UiLayoutConstants.DialogMargin
        };

    private static StackPanel CreateButtonPanel() =>
        new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = UiLayoutConstants.DialogButtonPanelMargin
        };

    private static FrameworkElement CreateMessageContent(
        string message,
        bool showApplicationIcon)
    {
        var textBlock = CreateMessage(message);
        if (!showApplicationIcon)
        {
            return textBlock;
        }

        var panel = new StackPanel();
        panel.Children.Add(new Image
        {
            Source = new BitmapImage(new Uri(
                "pack://application:,,,/Assets/AppIcon.png",
                UriKind.Absolute)),
            Width = UiLayoutConstants.AboutIconSize,
            Height = UiLayoutConstants.AboutIconSize,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = UiLayoutConstants.AboutIconMargin
        });
        panel.Children.Add(textBlock);
        return panel;
    }

    private static TextBlock CreateMessage(string message) =>
        new()
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = UiLayoutConstants.DialogMessageFontSize,
            MinWidth = UiLayoutConstants.DialogMessageMinimumWidth
        };

    private static Button CreateButton(
        string label,
        bool isPrimary,
        ThemePalette palette) =>
        new()
        {
            Content = label,
            MinWidth = UiLayoutConstants.DialogButtonMinimumWidth,
            Padding = UiLayoutConstants.DialogButtonPadding,
            Margin = UiLayoutConstants.DialogButtonMargin,
            Background = ThemeBrushFactory.Create(
                isPrimary
                    ? palette.PrimaryButtonBackground
                    : palette.DialogButtonBackground),
            BorderBrush = ThemeBrushFactory.Create(palette.DialogButtonBorder),
            Foreground = ThemeBrushFactory.Create(
                isPrimary ? ThemeColorConstants.White : palette.DialogForeground)
        };
}
