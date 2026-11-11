using MDRead.Constants;
using MDRead.Markdown;
using MDRead.Services;
using MDRead.ViewModels;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace MDRead;

/// <summary>
/// Provides the WPF-specific integration required by the main view model.
/// </summary>
public partial class MainWindow : Window, IEditorTextOperations
{
    private MainWindowViewModel? _viewModel;
    private bool _isInternalNavigation;
    private string? _mappedPreviewResourceRoot;
    private TaskCompletionSource<bool>? _previewReady;
    private ulong? _previewNavigationId;
    private MarkdownSynchronizationHighlight? _markdownHighlight;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        ApplyTheme(ThemePalette.Dark);
    }

    /// <summary>
    /// Attaches the view model after all window-dependent services have been composed.
    /// </summary>
    /// <param name="viewModel">The main window view model.</param>
    internal void AttachViewModel(MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (_viewModel is not null)
        {
            throw new InvalidOperationException(AppText.ViewModelAlreadyAttached);
        }

        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.CloseRequested += ViewModel_CloseRequested;

        ApplyTheme(ThemePalette.Select(viewModel.IsDarkTheme));
        _ = InitializePreviewAsync();
    }

    /// <inheritdoc />
    public void WrapSelection(string prefix, string suffix)
    {
        var selectionStart = MarkdownEditor.SelectionStart;
        var selectedText = MarkdownEditor.SelectedText;
        var replacement = string.IsNullOrEmpty(selectedText)
            ? MarkdownSyntax.PlaceholderText
            : selectedText;

        MarkdownEditor.SelectedText = string.Concat(prefix, replacement, suffix);
        MarkdownEditor.SelectionStart = selectionStart + prefix.Length;
        MarkdownEditor.SelectionLength = replacement.Length;
        MarkdownEditor.Focus();
    }

    /// <inheritdoc />
    public void PrefixSelectedLines(string prefix)
    {
        var selectionStart = MarkdownEditor.SelectionStart;
        var selectionEnd = selectionStart + MarkdownEditor.SelectionLength;
        var editorText = MarkdownEditor.Text;
        var firstLineStart = editorText.LastIndexOf(
            DocumentConstants.LineFeed,
            Math.Max(0, selectionStart - 1)) + 1;
        var lastLineEnd = editorText.IndexOf(DocumentConstants.LineFeed, selectionEnd);

        if (lastLineEnd < 0)
        {
            lastLineEnd = editorText.Length;
        }

        MarkdownEditor.Select(firstLineStart, lastLineEnd - firstLineStart);
        MarkdownEditor.SelectedText = string.Join(
            DocumentConstants.LineFeedString,
            editorText[firstLineStart..lastLineEnd]
                .Split(DocumentConstants.LineFeed)
                .Select(line => prefix + line));
        MarkdownEditor.Focus();
    }

    /// <inheritdoc />
    public void FocusEditor() => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
    {
        if (MarkdownTab.IsSelected)
        {
            MarkdownEditor.Focus();
        }
    });

    /// <inheritdoc />
    public void SynchronizeHtmlPreview() => _ = SynchronizeHtmlPreviewAsync();

    /// <inheritdoc />
    public void SynchronizeMarkdownEditor() => _ = SynchronizeMarkdownEditorAsync();

    private MainWindowViewModel ViewModel =>
        _viewModel ?? throw new InvalidOperationException(AppText.ViewModelNotAttached);

    private async Task InitializePreviewAsync()
    {
        try
        {
            var environment = await CreateWebView2EnvironmentAsync();
            await PreviewBrowser.EnsureCoreWebView2Async(environment);
        }
        catch (Exception exception)
        {
            ViewModel.ReportPreviewFailure(exception);
            return;
        }

        PreviewBrowser.CoreWebView2.NavigationStarting += Preview_NavigationStarting;
        PreviewBrowser.CoreWebView2.DOMContentLoaded += Preview_DOMContentLoaded;
        PreviewBrowser.CoreWebView2.NavigationCompleted += Preview_NavigationCompleted;
        PreviewBrowser.CoreWebView2.NewWindowRequested += Preview_NewWindowRequested;
        PreviewBrowser.CoreWebView2.WebMessageReceived += Preview_WebMessageReceived;
        ViewModel.InitializePreview();
    }

    private static Task<CoreWebView2Environment> CreateWebView2EnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            SettingsConstants.PublisherFolderName,
            SettingsConstants.ApplicationFolderName,
            WebView2Constants.UserDataFolderName);

        Directory.CreateDirectory(userDataFolder);
        return CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        switch (eventArgs.PropertyName)
        {
            case nameof(MainWindowViewModel.PreviewHtml):
                NavigatePreview(ViewModel.PreviewHtml);
                break;

            case nameof(MainWindowViewModel.IsDarkTheme):
                ApplyTheme(ThemePalette.Select(ViewModel.IsDarkTheme));
                break;
        }
    }

    private void NavigatePreview(string html)
    {
        if (PreviewBrowser.CoreWebView2 is null)
        {
            return;
        }

        ConfigurePreviewResourceMapping(ViewModel.PreviewResourceRoot);
        _previewReady?.TrySetResult(false);
        _previewReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _previewNavigationId = null;
        _isInternalNavigation = true;
        PreviewBrowser.NavigateToString(html);
    }

    /// <summary>
    /// Maps the current document volume to an isolated virtual host so that HTML
    /// supplied through <c>NavigateToString</c> can load its local resources.
    /// </summary>
    /// <param name="resourceRoot">The root directory containing the current document.</param>
    private void ConfigurePreviewResourceMapping(string? resourceRoot)
    {
        if (string.IsNullOrEmpty(resourceRoot))
        {
            if (_mappedPreviewResourceRoot is not null)
            {
                PreviewBrowser.CoreWebView2!.ClearVirtualHostNameToFolderMapping(
                    WebView2Constants.PreviewVirtualHostName);
                _mappedPreviewResourceRoot = null;
            }

            return;
        }

        if (string.Equals(
                resourceRoot,
                _mappedPreviewResourceRoot,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PreviewBrowser.CoreWebView2!.SetVirtualHostNameToFolderMapping(
            WebView2Constants.PreviewVirtualHostName,
            resourceRoot,
            CoreWebView2HostResourceAccessKind.DenyCors);
        _mappedPreviewResourceRoot = resourceRoot;
    }

    private async Task SynchronizeHtmlPreviewAsync()
    {
        var preview = PreviewBrowser.CoreWebView2;
        var source = MarkdownEditor.Text;
        var line = MarkdownTextSynchronizer.GetLineRange(source, MarkdownEditor.CaretIndex);
        if (preview is null || line.Start == line.End)
        {
            ViewModel.StatusText = AppText.HtmlSynchronizationNotFoundStatus;
            return;
        }

        try
        {
            ViewModel.RefreshPreviewForSynchronization();
            var revision = ViewModel.PreviewRevision;
            var ready = _previewReady?.Task;
            HtmlTab.SetCurrentValue(System.Windows.Controls.TabItem.IsSelectedProperty, true);
            UpdateLayout();

            if (ready is null || !await ready.WaitAsync(TimeSpan.FromSeconds(5))
                || !HtmlTab.IsSelected || ViewModel.PreviewRevision != revision
                || ViewModel.PreviewSource != source || MarkdownEditor.Text != source)
            {
                ViewModel.StatusText = AppText.HtmlSynchronizationNotFoundStatus;
                return;
            }

            var result = await preview.ExecuteScriptAsync(
                $"window.mdreadSync?.highlightLine({revision}, {line.Start}, {line.End}) ?? false");
            ViewModel.StatusText = JsonSerializer.Deserialize<bool>(result)
                ? AppText.HtmlSynchronizedStatus
                : AppText.HtmlSynchronizationNotFoundStatus;
        }
        catch (Exception)
        {
            ViewModel.StatusText = AppText.HtmlSynchronizationNotFoundStatus;
        }
    }

    private async Task SynchronizeMarkdownEditorAsync()
    {
        var preview = PreviewBrowser.CoreWebView2;
        var source = MarkdownEditor.Text;
        var revision = ViewModel.PreviewRevision;
        if (preview is null || _previewReady?.Task is not { IsCompletedSuccessfully: true, Result: true }
            || ViewModel.PreviewSource != source)
        {
            ViewModel.StatusText = AppText.PreviewSelectionUnavailableStatus;
            return;
        }

        try
        {
            var result = await preview.ExecuteScriptAsync(
                $"window.mdreadSync?.readSelection({revision}) ?? null");
            var range = JsonSerializer.Deserialize<MarkdownSourceRange?>(result);
            if (!HtmlTab.IsSelected || ViewModel.PreviewRevision != revision
                || MarkdownEditor.Text != source)
            {
                ViewModel.StatusText = AppText.PreviewSelectionUnavailableStatus;
                return;
            }

            if (range is not { } match || match.Start < 0 || match.End <= match.Start
                || match.End > source.Length)
            {
                ViewModel.StatusText = AppText.MarkdownSynchronizationNotFoundStatus;
                return;
            }

            MarkdownTab.SetCurrentValue(System.Windows.Controls.TabItem.IsSelectedProperty, true);
            UpdateLayout();
            MarkdownEditor.Focus();
            MarkdownEditor.Select(match.Start, 0);
            MarkdownEditor.ScrollToLine(MarkdownEditor.GetLineIndexFromCharacterIndex(match.Start));
            MarkdownEditor.UpdateLayout();
            var caretBounds = MarkdownEditor.GetRectFromCharacterIndex(match.Start);
            if (!caretBounds.IsEmpty && (caretBounds.X < 0 || caretBounds.X >= MarkdownEditor.ViewportWidth))
            {
                MarkdownEditor.ScrollToHorizontalOffset(
                    Math.Max(0, MarkdownEditor.HorizontalOffset + caretBounds.X - MarkdownEditor.ViewportWidth / 3));
            }

            _markdownHighlight?.Remove();
            _markdownHighlight = MarkdownSynchronizationHighlight.Show(MarkdownEditor, match);
            ViewModel.StatusText = AppText.MarkdownSynchronizedStatus;
        }
        catch (Exception)
        {
            ViewModel.StatusText = AppText.MarkdownSynchronizationNotFoundStatus;
        }
    }

    private void ApplyTheme(ThemePalette palette)
    {
        SetBrush(ThemeResourceKeys.WindowBackground, palette.WindowBackground);
        SetBrush(ThemeResourceKeys.EditorBackground, palette.EditorBackground);
        SetBrush(ThemeResourceKeys.EditorForeground, palette.EditorForeground);
        SetBrush(ThemeResourceKeys.MenuBackground, palette.ChromeBackground);
        SetBrush(ThemeResourceKeys.MenuForeground, palette.ChromeForeground);
        SetBrush(ThemeResourceKeys.MenuHover, palette.ChromeHover);
        SetBrush(ThemeResourceKeys.SelectedTabBackground, palette.PrimaryButtonBackground);
        SetBrush(ThemeResourceKeys.MenuBorder, palette.ChromeBorder);
        SetBrush(ThemeResourceKeys.DisabledForeground, palette.DisabledForeground);
        SetBrush(ThemeResourceKeys.ScrollTrack, palette.ScrollTrack);
        SetBrush(ThemeResourceKeys.ScrollThumb, palette.ScrollThumb);
        SetBrush(ThemeResourceKeys.ScrollThumbHover, palette.ScrollThumbHover);
    }

    private void SetBrush(string resourceKey, string color) =>
        Resources[resourceKey] = ThemeBrushFactory.Create(color);

    private void Preview_NavigationStarting(
        object? sender,
        CoreWebView2NavigationStartingEventArgs eventArgs)
    {
        if (TryGetMarkdownPathFromWebTarget(eventArgs.Uri, out var markdownPath))
        {
            eventArgs.Cancel = true;
            Dispatcher.BeginInvoke(() => ViewModel.OpenDroppedFile(markdownPath));
            return;
        }

        // NavigateToString is the only navigation initiated by the application itself.
        if (_isInternalNavigation)
        {
            _isInternalNavigation = false;
            _previewNavigationId = eventArgs.NavigationId;
            return;
        }

        eventArgs.Cancel = true;
    }

    private void Preview_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs eventArgs)
    {
        if (eventArgs.NavigationId == _previewNavigationId)
        {
            _previewReady?.TrySetResult(true);
        }
    }

    private void Preview_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs eventArgs)
    {
        if (eventArgs.NavigationId == _previewNavigationId)
        {
            _previewReady?.TrySetResult(eventArgs.IsSuccess);
        }
    }

    private void Preview_NewWindowRequested(
        object? sender,
        CoreWebView2NewWindowRequestedEventArgs eventArgs)
    {
        eventArgs.Handled = true;
        Dispatcher.BeginInvoke(() =>
        {
            if (TryGetMarkdownPathFromWebTarget(eventArgs.Uri, out var markdownPath))
            {
                ViewModel.OpenDroppedFile(markdownPath);
                return;
            }

            ViewModel.OpenPreviewTarget(eventArgs.Uri);
        });
    }

    private void Preview_WebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs eventArgs)
    {
        string target;
        try
        {
            target = eventArgs.TryGetWebMessageAsString();
        }
        catch (Exception)
        {
            // Ignore malformed or non-string messages from untrusted preview content.
            return;
        }

        Dispatcher.BeginInvoke(() => ViewModel.OpenPreviewTarget(target));
    }

    private void ViewModel_CloseRequested(object? sender, EventArgs eventArgs) => Close();

    private void Window_PreviewDragOver(object sender, DragEventArgs eventArgs)
    {
        eventArgs.Effects = TryGetDroppedMarkdownPath(eventArgs.Data, out _)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        eventArgs.Handled = true;
    }

    private void Window_PreviewDrop(object sender, DragEventArgs eventArgs)
    {
        if (TryGetDroppedMarkdownPath(eventArgs.Data, out var path))
        {
            ViewModel.OpenDroppedFile(path);
        }

        eventArgs.Handled = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs eventArgs)
    {
        if (_viewModel is not null && !_viewModel.CanClose())
        {
            eventArgs.Cancel = true;
        }
    }

    /// <summary>
    /// Gets the first supported Markdown file supplied through the standard shell file-drop format.
    /// </summary>
    /// <param name="data">The data supplied by the drag-and-drop source.</param>
    /// <param name="path">The selected Markdown path, when available.</param>
    /// <returns><see langword="true"/> when a supported Markdown file was dropped.</returns>
    private static bool TryGetDroppedMarkdownPath(IDataObject data, out string path)
    {
        path = string.Empty;
        if (!data.GetDataPresent(DataFormats.FileDrop) ||
            data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return false;
        }

        var markdownPath = paths.FirstOrDefault(DocumentConstants.IsMarkdownFilePath);
        if (markdownPath is null)
        {
            return false;
        }

        path = markdownPath;
        return true;
    }

    /// <summary>
    /// Resolves a Markdown file URI produced by WebView2's native drop handling.
    /// </summary>
    /// <param name="target">The WebView2 navigation target.</param>
    /// <param name="path">The local Markdown path, when available.</param>
    /// <returns><see langword="true"/> when the target is a local Markdown file.</returns>
    private static bool TryGetMarkdownPathFromWebTarget(
        string target,
        out string path)
    {
        path = string.Empty;
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
            !uri.IsFile ||
            !DocumentConstants.IsMarkdownFilePath(uri.LocalPath))
        {
            return false;
        }

        path = uri.LocalPath;
        return true;
    }
}
