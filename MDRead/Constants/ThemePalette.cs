using System.Windows.Media;

namespace MDRead.Constants;

/// <summary>
/// Describes the colors used by a WPF application theme.
/// </summary>
/// <param name="WindowBackground">The main-window background.</param>
/// <param name="EditorBackground">The editor background.</param>
/// <param name="EditorForeground">The editor foreground.</param>
/// <param name="ChromeBackground">The menu, toolbar, and status-bar background.</param>
/// <param name="ChromeForeground">The menu, toolbar, and status-bar foreground.</param>
/// <param name="ChromeHover">The highlighted chrome color.</param>
/// <param name="ChromeBorder">The chrome border color.</param>
/// <param name="DisabledForeground">The disabled menu-item foreground.</param>
/// <param name="ScrollTrack">The scrollbar track color.</param>
/// <param name="ScrollThumb">The scrollbar thumb color.</param>
/// <param name="ScrollThumbHover">The highlighted scrollbar thumb color.</param>
/// <param name="DialogBackground">The dialog background.</param>
/// <param name="DialogForeground">The dialog foreground.</param>
/// <param name="DialogButtonBackground">The secondary dialog button background.</param>
/// <param name="DialogButtonBorder">The dialog button border.</param>
/// <param name="PrimaryButtonBackground">The primary dialog button background.</param>
internal sealed record ThemePalette(
    string WindowBackground,
    string EditorBackground,
    string EditorForeground,
    string ChromeBackground,
    string ChromeForeground,
    string ChromeHover,
    string ChromeBorder,
    string DisabledForeground,
    string ScrollTrack,
    string ScrollThumb,
    string ScrollThumbHover,
    string DialogBackground,
    string DialogForeground,
    string DialogButtonBackground,
    string DialogButtonBorder,
    string PrimaryButtonBackground)
{
    /// <summary>Gets the dark application palette.</summary>
    public static readonly ThemePalette Dark = new(
        WindowBackground: "#1E1E1E",
        EditorBackground: "#252526",
        EditorForeground: "#E8E8E8",
        ChromeBackground: "#252526",
        ChromeForeground: "#E8E8E8",
        ChromeHover: "#3E3E42",
        ChromeBorder: "#454545",
        DisabledForeground: "#808080",
        ScrollTrack: "#1A1A1A",
        ScrollThumb: "#686868",
        ScrollThumbHover: "#909090",
        DialogBackground: "#252526",
        DialogForeground: "#F1F1F1",
        DialogButtonBackground: "#3E3E42",
        DialogButtonBorder: "#777777",
        PrimaryButtonBackground: "#005EA0");

    /// <summary>Gets the light application palette.</summary>
    public static readonly ThemePalette Light = new(
        WindowBackground: "#F2F4F7",
        EditorBackground: "#FFFFFF",
        EditorForeground: "#16181D",
        ChromeBackground: "#E1E6ED",
        ChromeForeground: "#202833",
        ChromeHover: "#C6D0DC",
        ChromeBorder: "#AAB5C2",
        DisabledForeground: "#697586",
        ScrollTrack: "#D7DCE3",
        ScrollThumb: "#687385",
        ScrollThumbHover: "#39465A",
        DialogBackground: "#FFFFFF",
        DialogForeground: "#15171A",
        DialogButtonBackground: "#E1E6ED",
        DialogButtonBorder: "#8995A5",
        PrimaryButtonBackground: "#0060AA");

    /// <summary>
    /// Selects the palette that matches the requested theme.
    /// </summary>
    /// <param name="isDarkTheme">Whether the dark theme is active.</param>
    /// <returns>The matching palette.</returns>
    public static ThemePalette Select(bool isDarkTheme) =>
        isDarkTheme ? Dark : Light;
}

/// <summary>
/// Provides keys for theme brushes stored in WPF resource dictionaries.
/// </summary>
public static class ThemeResourceKeys
{
    /// <summary>Gets the window-background brush key.</summary>
    public const string WindowBackground = "WindowBackgroundBrush";

    /// <summary>Gets the editor-background brush key.</summary>
    public const string EditorBackground = "EditorBackgroundBrush";

    /// <summary>Gets the editor-foreground brush key.</summary>
    public const string EditorForeground = "EditorForegroundBrush";

    /// <summary>Gets the chrome-background brush key.</summary>
    public const string MenuBackground = "MenuBackgroundBrush";

    /// <summary>Gets the chrome-foreground brush key.</summary>
    public const string MenuForeground = "MenuForegroundBrush";

    /// <summary>Gets the chrome-highlight brush key.</summary>
    public const string MenuHover = "MenuHoverBrush";

    /// <summary>Gets the chrome-border brush key.</summary>
    public const string MenuBorder = "MenuBorderBrush";

    /// <summary>Gets the disabled-foreground brush key.</summary>
    public const string DisabledForeground = "DisabledForegroundBrush";

    /// <summary>Gets the scrollbar-track brush key.</summary>
    public const string ScrollTrack = "ScrollTrackBrush";

    /// <summary>Gets the scrollbar-thumb brush key.</summary>
    public const string ScrollThumb = "ScrollThumbBrush";

    /// <summary>Gets the scrollbar-thumb-highlight brush key.</summary>
    public const string ScrollThumbHover = "ScrollThumbHoverBrush";
}

/// <summary>
/// Provides keys for reusable WPF styles.
/// </summary>
public static class UiResourceKeys
{
    /// <summary>Gets the editor scrollbar style key.</summary>
    public const string EditorScrollBarStyle = "EditorScrollBar";

    /// <summary>Gets the editor word-wrap switch style key.</summary>
    public const string WordWrapToggleStyle = "WordWrapToggle";
}

/// <summary>
/// Creates immutable WPF brushes from theme color data.
/// </summary>
internal static class ThemeBrushFactory
{
    /// <summary>
    /// Creates a frozen brush from a hexadecimal color.
    /// </summary>
    /// <param name="hexColor">The color in hexadecimal notation.</param>
    /// <returns>The frozen brush.</returns>
    public static SolidColorBrush Create(string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor)!;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// Provides fixed colors that do not vary by theme.
/// </summary>
internal static class ThemeColorConstants
{
    /// <summary>Gets the primary-button foreground color.</summary>
    public const string White = "#FFFFFF";
}
