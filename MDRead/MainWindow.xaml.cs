using MDRead.Constants;
using MDRead.Markdown;
using MDRead.Services;
using MDRead.ViewModels;
using Microsoft.Web.WebView2.Core;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace MDRead;

/// <summary>
/// Provides the WPF-specific integration required by the main view model.
/// </summary>
public partial class MainWindow : Window, IEditorTextOperations
{
    private MainWindowViewModel? _viewModel;
    private bool _isInternalNavigation;

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
        ApplyEditMode(viewModel.IsEditMode);
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
    public void FocusEditor() => MarkdownEditor.Focus();

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

            case nameof(MainWindowViewModel.IsEditMode):
                ApplyEditMode(ViewModel.IsEditMode);
                break;
        }
    }

    private void NavigatePreview(string html)
    {
        if (PreviewBrowser.CoreWebView2 is null)
        {
            return;
        }

        _isInternalNavigation = true;
        PreviewBrowser.NavigateToString(html);
    }

    private async Task SynchronizeHtmlPreviewAsync()
    {
        var anchor = MarkdownTextSynchronizer.CreateAnchor(
            MarkdownEditor.Text,
            MarkdownEditor.SelectionStart);
        if (anchor is null)
        {
            ViewModel.StatusText = AppText.HtmlSynchronizationNotFoundStatus;
            return;
        }

        var preview = PreviewBrowser.CoreWebView2;
        if (preview is null)
        {
            ViewModel.StatusText = AppText.HtmlSynchronizationNotFoundStatus;
            return;
        }

        try
        {
            var result = await preview.ExecuteScriptAsync(
                BuildFindHtmlBlockScript(anchor));
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
        if (preview is null)
        {
            ViewModel.StatusText = AppText.MarkdownSynchronizationNotFoundStatus;
            return;
        }

        try
        {
            var result = await preview.ExecuteScriptAsync(BuildVisibleHtmlAnchorScript());
            var anchorJson = JsonSerializer.Deserialize<string>(result);
            var anchor = anchorJson is null
                ? null
                : JsonSerializer.Deserialize<TextSynchronizationAnchor>(anchorJson);
            var block = anchor is null
                ? null
                : MarkdownTextSynchronizer.FindMatchingBlock(
                    MarkdownEditor.Text,
                    anchor);

            if (anchor is null || block is null)
            {
                ViewModel.StatusText = AppText.MarkdownSynchronizationNotFoundStatus;
                return;
            }

            ScrollMarkdownEditorTo(block, anchor.BlockProgress);
            ViewModel.StatusText = AppText.MarkdownSynchronizedStatus;
        }
        catch (Exception)
        {
            ViewModel.StatusText = AppText.MarkdownSynchronizationNotFoundStatus;
        }
    }

    private static string BuildFindHtmlBlockScript(
        TextSynchronizationAnchor anchor) =>
        string.Concat(
            "(()=>{const a=",
            JsonSerializer.Serialize(anchor),
            ";const n=v=>v.normalize('NFD').toLowerCase().replace(/\\p{M}/gu,'').replace(/[^\\p{L}\\p{N}]/gu,'');",
            "const e=[...document.querySelectorAll('h1,h2,h3,h4,h5,h6,p,li,pre,blockquote,tr')]",
            ".map(x=>({element:x,text:n(x.innerText)})).filter(x=>x.text);const m=e.map((x,i)=>({x,i})).filter(x=>x.x.text===a.Text);",
            "if(!m.length)return false;const s=q=>(a.Previous&&e[q.i-1].text===a.Previous?1:0)+(a.Next&&e[q.i+1].text===a.Next?1:0);",
            "const h=Math.max(...m.map(s));const b=m.filter(q=>s(q)===h);if(m.length>1&&(h===0||b.length!==1))return false;",
            "b[0].x.element.scrollIntoView({block:'center'});return true;})()");

    private static string BuildVisibleHtmlAnchorScript() =>
        "(()=>{const n=v=>v.normalize('NFD').toLowerCase().replace(/\\p{M}/gu,'').replace(/[^\\p{L}\\p{N}]/gu,'');"
        + "const y=window.innerHeight/3;const e=[...document.querySelectorAll('h1,h2,h3,h4,h5,h6,p,li,pre,blockquote,tr')]"
        + ".map(x=>({element:x,text:n(x.innerText),distance:Math.abs(((x.getBoundingClientRect().top+x.getBoundingClientRect().bottom)/2)-y)})).filter(x=>x.text);"
        + "if(!e.length)return null;const q=e.reduce((a,b)=>a.distance<=b.distance?a:b);const i=e.indexOf(q);"
        + "const r=q.element.getBoundingClientRect();const p=Math.max(0,Math.min(1,(y-r.top)/Math.max(r.height,1)));"
        + "return JSON.stringify({Text:q.text,Previous:i?e[i-1].text:null,Next:i<e.length-1?e[i+1].text:null,BlockProgress:p});})()";

    private void ScrollMarkdownEditorTo(
        MarkdownTextBlock block,
        double blockProgress)
    {
        var progress = Math.Clamp(blockProgress, 0, 1);
        var targetIndex = block.Start + (int)Math.Round(
            (block.End - block.Start) * progress);
        targetIndex = Math.Clamp(targetIndex, 0, MarkdownEditor.Text.Length);

        var selectionStart = MarkdownEditor.SelectionStart;
        var selectionLength = MarkdownEditor.SelectionLength;
        MarkdownEditor.Select(targetIndex, 0);
        MarkdownEditor.Focus();
        MarkdownEditor.UpdateLayout();
        MarkdownEditor.Select(selectionStart, selectionLength);
    }

    private void ApplyEditMode(bool isEnabled)
    {
        EditorPanel.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
        EditorSplitter.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
        EditorRow.Height = isEnabled
            ? new GridLength(ViewModel.EditorHeight)
            : new GridLength(UiLayoutConstants.CollapsedHeight);
        SplitterRow.Height = isEnabled
            ? new GridLength(UiLayoutConstants.SplitterHeight)
            : new GridLength(UiLayoutConstants.CollapsedHeight);

        if (isEnabled)
        {
            MarkdownEditor.Focus();
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
            return;
        }

        eventArgs.Cancel = true;
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

    private void EditorSplitter_DragCompleted(object sender, DragCompletedEventArgs eventArgs)
    {
        if (ViewModel.IsEditMode)
        {
            ViewModel.SaveEditorHeight(EditorPanel.ActualHeight);
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
