using System.Windows;
using System.Windows.Media;

namespace MDRead.Constants;

/// <summary>
/// Provides dimensions and presentation values used by WPF views.
/// </summary>
public static class UiLayoutConstants
{
    /// <summary>Gets the default window height.</summary>
    public const double WindowHeight = 760;

    /// <summary>Gets the default window width.</summary>
    public const double WindowWidth = 1100;

    /// <summary>Gets the minimum window height.</summary>
    public const double MinimumWindowHeight = 500;

    /// <summary>Gets the minimum window width.</summary>
    public const double MinimumWindowWidth = 700;

    /// <summary>Gets the default editor-pane height.</summary>
    public const double DefaultEditorHeight = 300;

    /// <summary>Gets the minimum editor-pane height accepted from settings.</summary>
    public const double MinimumEditorHeight = 80;

    /// <summary>Gets the splitter height.</summary>
    public const double SplitterHeight = 7;

    /// <summary>Gets the height used for collapsed rows.</summary>
    public const double CollapsedHeight = 0;

    /// <summary>Gets the editor font size.</summary>
    public const double EditorFontSize = 14;

    /// <summary>Gets the editor font family name.</summary>
    public const string EditorFontFamilyName = "Consolas";

    /// <summary>Gets the editor font family.</summary>
    public static readonly FontFamily EditorFontFamily =
        new(EditorFontFamilyName);

    /// <summary>Gets the row height used by collapsed editor rows.</summary>
    public static readonly GridLength CollapsedRowHeight =
        new(CollapsedHeight);

    /// <summary>Gets the editor content padding.</summary>
    public static readonly Thickness EditorPadding = new(12);

    /// <summary>Gets the status-bar border thickness.</summary>
    public static readonly Thickness StatusBarBorderThickness = new(1, 1, 0, 0);

    /// <summary>Gets the editor border thickness.</summary>
    public static readonly Thickness EditorBorderThickness = new(0, 0, 0, 1);

    /// <summary>Gets the minimum themed-dialog width.</summary>
    internal const double DialogMinimumWidth = 390;

    /// <summary>Gets the maximum themed-dialog width.</summary>
    internal const double DialogMaximumWidth = 640;

    /// <summary>Gets the themed-dialog message minimum width.</summary>
    internal const double DialogMessageMinimumWidth = 340;

    /// <summary>Gets the themed-dialog message font size.</summary>
    internal const double DialogMessageFontSize = 14;

    /// <summary>Gets the minimum themed-dialog button width.</summary>
    internal const double DialogButtonMinimumWidth = 82;

    /// <summary>Gets the themed-dialog content margin.</summary>
    internal static readonly Thickness DialogMargin = new(24, 20, 24, 18);

    /// <summary>Gets the themed-dialog button row margin.</summary>
    internal static readonly Thickness DialogButtonPanelMargin = new(0, 22, 0, 0);

    /// <summary>Gets the themed-dialog button padding.</summary>
    internal static readonly Thickness DialogButtonPadding = new(12, 5, 12, 5);

    /// <summary>Gets the themed-dialog button margin.</summary>
    internal static readonly Thickness DialogButtonMargin = new(8, 0, 0, 0);

    /// <summary>Gets the application-icon size shown in the About dialog.</summary>
    internal const double AboutIconSize = 72;

    /// <summary>Gets the application-icon margin shown in the About dialog.</summary>
    internal static readonly Thickness AboutIconMargin = new(0, 0, 0, 16);
}

/// <summary>
/// Provides reusable dimensions and visual values for shared WPF styles.
/// </summary>
public static class UiStyleConstants
{
    /// <summary>Gets the menu-item padding.</summary>
    public static readonly Thickness MenuItemPadding = new(8, 4, 8, 4);

    /// <summary>Gets the submenu item minimum width.</summary>
    public const double SubmenuMinimumWidth = 125;

    /// <summary>Gets the submenu checkmark column width.</summary>
    public static readonly GridLength CheckmarkColumnWidth = new(18);

    /// <summary>Gets the submenu header column width.</summary>
    public static readonly GridLength HeaderColumnWidth =
        new(1, GridUnitType.Star);

    /// <summary>Gets the submenu gesture column width.</summary>
    public static readonly GridLength GestureColumnWidth = GridLength.Auto;

    /// <summary>Gets the submenu arrow column width.</summary>
    public static readonly GridLength ArrowColumnWidth = new(14);

    /// <summary>Gets the submenu checkmark.</summary>
    public const string Checkmark = "✓";

    /// <summary>Gets the input-gesture text opacity.</summary>
    public const double InputGestureOpacity = 0.7;

    /// <summary>Gets the input-gesture text margin.</summary>
    public static readonly Thickness InputGestureMargin = new(22, 0, 4, 0);

    /// <summary>Gets the submenu arrow geometry data.</summary>
    public const string SubmenuArrowGeometryData = "M 0 0 L 4 4 L 0 8 Z";

    /// <summary>Gets the submenu arrow geometry.</summary>
    public static readonly Geometry SubmenuArrowGeometry =
        Geometry.Parse(SubmenuArrowGeometryData);

    /// <summary>Gets the submenu arrow width.</summary>
    public const double SubmenuArrowWidth = 4;

    /// <summary>Gets the submenu arrow height.</summary>
    public const double SubmenuArrowHeight = 8;

    /// <summary>Gets the popup border thickness.</summary>
    public static readonly Thickness PopupBorderThickness = new(1);

    /// <summary>Gets the popup content padding.</summary>
    public static readonly Thickness PopupPadding = new(2);

    /// <summary>Gets the top-level menu-item minimum width.</summary>
    public const double TopLevelMenuMinimumWidth = 0;

    /// <summary>Gets the top-level header column span.</summary>
    public const int TopLevelHeaderColumnSpan = 4;

    /// <summary>Gets the separator thickness.</summary>
    public const double SeparatorThickness = 1;

    /// <summary>Gets the menu separator margin.</summary>
    public static readonly Thickness MenuSeparatorMargin = new(6, 4, 6, 4);

    /// <summary>Gets the scrollbar width and height.</summary>
    public const double ScrollBarSize = 12;

    /// <summary>Gets the scrollbar thumb corner radius.</summary>
    public static readonly CornerRadius ScrollThumbCornerRadius = new(5);

    /// <summary>Gets the scrollbar thumb margin.</summary>
    public static readonly Thickness ScrollThumbMargin = new(2);

    /// <summary>Gets the minimum height of a vertical scrollbar thumb.</summary>
    public const double MinimumVerticalScrollThumbHeight = 32;

    /// <summary>Gets the width of the word-wrap switch track.</summary>
    public const double WordWrapSwitchWidth = 32;

    /// <summary>Gets the height of the word-wrap switch track.</summary>
    public const double WordWrapSwitchHeight = 16;

    /// <summary>Gets the diameter of the word-wrap switch thumb.</summary>
    public const double WordWrapSwitchThumbSize = 12;

    /// <summary>Gets the corner radius of the word-wrap switch track.</summary>
    public static readonly CornerRadius WordWrapSwitchCornerRadius = new(8);

    /// <summary>Gets the margin between the word-wrap label and its switch.</summary>
    public static readonly Thickness WordWrapSwitchContentMargin = new(0, 0, 6, 0);

    /// <summary>Gets the inset around the word-wrap switch thumb.</summary>
    public static readonly Thickness WordWrapSwitchThumbMargin = new(2);

    /// <summary>Gets the toolbar border thickness.</summary>
    public static readonly Thickness ToolBarBorderThickness = new(0, 0, 0, 1);

    /// <summary>Gets the toolbar content margin.</summary>
    public static readonly Thickness ToolBarMargin = new(2);

    /// <summary>Gets the toolbar button padding.</summary>
    public static readonly Thickness ToolBarButtonPadding = new(7, 3, 7, 3);

    /// <summary>Gets the toolbar button margin.</summary>
    public static readonly Thickness ToolBarButtonMargin = new(1, 0, 1, 0);

    /// <summary>Gets the toolbar button corner radius.</summary>
    public static readonly CornerRadius ToolBarButtonCornerRadius = new(3);

    /// <summary>Gets the toolbar separator margin.</summary>
    public static readonly Thickness ToolBarSeparatorMargin = new(4, 3, 4, 3);
}
