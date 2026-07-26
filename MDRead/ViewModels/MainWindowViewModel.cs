using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDRead.Constants;
using MDRead.Markdown;
using MDRead.Models;
using MDRead.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;

namespace MDRead.ViewModels;

/// <summary>
/// Coordinates document editing, persistence, preview generation, and commands.
/// </summary>
internal partial class MainWindowViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IEditorTextOperations _editorOperations;
    private readonly IShellLauncher _shellLauncher;
    private readonly ISettingsService _settingsService;
    private readonly UserSettings _settings;
    private readonly string[] _startupArguments;
    private CancellationTokenSource? _renderCancellation;
    private string? _filePath;
    private bool _isDirty;
    private bool _isLoadingDocument;
    private bool _isPreviewReady;

    [ObservableProperty]
    private string _markdownText = string.Empty;

    [ObservableProperty]
    private string _windowTitle = AppText.UntitledWindowTitle;

    [ObservableProperty]
    private string _statusText = AppText.ReadyStatus;

    [ObservableProperty]
    private string _previewHtml = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isEditMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dialogService">The modal dialog abstraction.</param>
    /// <param name="editorOperations">The selection-aware editor operations.</param>
    /// <param name="shellLauncher">The approved-target launcher.</param>
    /// <param name="settingsService">The settings persistence service.</param>
    /// <param name="startupArguments">The command-line arguments.</param>
    public MainWindowViewModel(
        IDialogService dialogService,
        IEditorTextOperations editorOperations,
        IShellLauncher shellLauncher,
        ISettingsService settingsService,
        IEnumerable<string> startupArguments)
    {
        _dialogService = dialogService;
        _editorOperations = editorOperations;
        _shellLauncher = shellLauncher;
        _settingsService = settingsService;
        _startupArguments = startupArguments.ToArray();
        _settings = settingsService.Load();
        _isDarkTheme = ResolveInitialTheme(_settings.IsDark, _startupArguments);
        RefreshRecentFiles();
    }

    /// <summary>
    /// Occurs when the view should close the application window.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Gets the recent-file entries displayed by the File menu.
    /// </summary>
    public ObservableCollection<RecentFileEntry> RecentFiles { get; } = [];

    /// <summary>
    /// Gets the valid editor height restored from user settings.
    /// </summary>
    public double EditorHeight =>
        _settings.EditorHeight > UiLayoutConstants.MinimumEditorHeight
            ? _settings.EditorHeight
            : UiLayoutConstants.DefaultEditorHeight;

    /// <summary>
    /// Completes application startup after WebView2 is ready.
    /// </summary>
    public void InitializePreview()
    {
        _isPreviewReady = true;
        var startupPath = _startupArguments.FirstOrDefault(
            argument => !argument.StartsWith(
                ApplicationConstants.CommandLineOptionPrefix,
                StringComparison.Ordinal));

        if (startupPath is null)
        {
            RenderPreview();
            return;
        }

        OpenFile(startupPath);
    }

    /// <summary>
    /// Reports a WebView2 initialization failure.
    /// </summary>
    /// <param name="exception">The initialization error.</param>
    public void ReportPreviewFailure(Exception exception) =>
        _dialogService.ShowMessage(string.Format(
            AppText.PreviewInitializationFailedFormat,
            exception.Message));

    /// <summary>
    /// Determines whether the window may close.
    /// </summary>
    /// <returns><see langword="true"/> when closing may continue.</returns>
    public bool CanClose()
    {
        if (!_isDirty)
        {
            return true;
        }

        return ResolvePendingChanges(AppText.ConfirmSaveBeforeClosing);
    }

    /// <summary>
    /// Persists the current editor-pane height.
    /// </summary>
    /// <param name="height">The height in device-independent units.</param>
    public void SaveEditorHeight(double height)
    {
        _settings.EditorHeight = height;
        SaveSettings();
        OnPropertyChanged(nameof(EditorHeight));
    }

    /// <summary>
    /// Opens a Markdown document supplied by a drag-and-drop operation.
    /// </summary>
    /// <param name="path">The path supplied by the drop source.</param>
    public void OpenDroppedFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (DocumentConstants.IsMarkdownFilePath(path))
        {
            OpenFile(path);
        }
    }

    /// <summary>
    /// Validates and opens a target selected in the HTML preview.
    /// </summary>
    /// <param name="target">The absolute target supplied by WebView2.</param>
    public void OpenPreviewTarget(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == Uri.UriSchemeMailto)
        {
            if (_dialogService.Confirm(string.Format(
                AppText.ConfirmOpenLinkFormat,
                uri.AbsoluteUri)))
            {
                LaunchTarget(uri.AbsoluteUri);
            }

            return;
        }

        if (uri.IsFile)
        {
            OpenLocalFile(uri.LocalPath);
            return;
        }

        _dialogService.ShowMessage(string.Format(
            AppText.BlockedTargetFormat,
            target));
    }

    [RelayCommand]
    private void NewDocument()
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        SetDocumentText(string.Empty);
        _filePath = null;
        _isDirty = false;
        WindowTitle = AppText.UntitledWindowTitle;
        StatusText = AppText.NewDocumentStatus;
        RenderPreview();
    }

    [RelayCommand]
    private void OpenDocument()
    {
        var path = _dialogService.PickFileToOpen(
            AppText.OpenMarkdownFilter,
            GetValidDirectory());

        if (path is not null)
        {
            OpenFile(path);
        }
    }

    [RelayCommand]
    private void OpenRecentFile(RecentFileEntry? entry)
    {
        if (entry?.FilePath is not null)
        {
            OpenFile(entry.FilePath);
        }
    }

    [RelayCommand]
    private void Save() => SaveCurrent();

    [RelayCommand]
    private void SaveAs() => SaveToNewPath();

    [RelayCommand]
    private void ExportHtml()
    {
        var suggestedFileName = _filePath is null
            ? AppText.DefaultHtmlFileName
            : Path.ChangeExtension(
                Path.GetFileName(_filePath),
                DocumentConstants.HtmlFileExtension);
        var path = _dialogService.PickFileToSave(
            AppText.ExportHtmlFilter,
            suggestedFileName,
            GetValidDirectory());

        if (path is null)
        {
            return;
        }

        try
        {
            File.WriteAllText(
                path,
                BuildHtml(includePreviewBridge: false),
                CreateUtf8Encoding());
            RememberDirectory(path);
            StatusText = string.Format(AppText.ExportedStatusFormat, path);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException)
        {
            _dialogService.ShowMessage(string.Format(
                AppText.ExportFailedFormat,
                exception.Message));
        }
    }

    [RelayCommand]
    private void UseLightTheme() => SetTheme(isDarkTheme: false);

    [RelayCommand]
    private void UseDarkTheme() => SetTheme(isDarkTheme: true);

    [RelayCommand]
    private void Exit() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanExitPreview))]
    private void ExitPreview() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ShowAbout() =>
        _dialogService.ShowMessage(
            AppText.AboutMessage,
            AppText.AboutTitle);

    [RelayCommand]
    private void ApplyHeading1() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.Heading1);

    [RelayCommand]
    private void ApplyHeading2() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.Heading2);

    [RelayCommand]
    private void ApplyHeading3() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.Heading3);

    [RelayCommand]
    private void ApplyHeading4() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.Heading4);

    [RelayCommand]
    private void ApplyBold() =>
        _editorOperations.WrapSelection(MarkdownSyntax.Bold, MarkdownSyntax.Bold);

    [RelayCommand]
    private void ApplyItalic() =>
        _editorOperations.WrapSelection(MarkdownSyntax.Italic, MarkdownSyntax.Italic);

    [RelayCommand]
    private void ApplyUnderline() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.UnderlineOpen,
            MarkdownSyntax.UnderlineClose);

    [RelayCommand]
    private void ApplyStrikethrough() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.Strikethrough,
            MarkdownSyntax.Strikethrough);

    [RelayCommand]
    private void InsertLink() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.LinkOpen,
            MarkdownSyntax.LinkClose);

    [RelayCommand]
    private void InsertImage() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.ImageOpen,
            MarkdownSyntax.ImageClose);

    [RelayCommand]
    private void InsertUnorderedList() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.UnorderedListItem);

    [RelayCommand]
    private void InsertOrderedList() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.OrderedListItem);

    [RelayCommand]
    private void ApplyInlineCode() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.InlineCode,
            MarkdownSyntax.InlineCode);

    [RelayCommand]
    private void InsertCodeBlock() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.CodeBlockOpen,
            MarkdownSyntax.CodeBlockClose);

    [RelayCommand]
    private void InsertQuote() =>
        _editorOperations.PrefixSelectedLines(MarkdownSyntax.Quote);

    [RelayCommand]
    private void InsertHorizontalRule() =>
        _editorOperations.WrapSelection(
            MarkdownSyntax.HorizontalRule,
            string.Empty);

    partial void OnMarkdownTextChanged(string value)
    {
        if (_isLoadingDocument)
        {
            return;
        }

        _isDirty = true;
        SchedulePreviewRender();
    }

    partial void OnIsEditModeChanged(bool value)
    {
        ExitPreviewCommand.NotifyCanExecuteChanged();

        if (value)
        {
            _editorOperations.FocusEditor();
        }
    }

    private bool CanExitPreview() => !IsEditMode;

    private static bool ResolveInitialTheme(
        bool configuredDarkTheme,
        IEnumerable<string> arguments)
    {
        if (arguments.Any(argument => argument.Equals(
            ApplicationConstants.LightThemeArgument,
            StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (arguments.Any(argument => argument.Equals(
            ApplicationConstants.DarkThemeArgument,
            StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return configuredDarkTheme;
    }

    private void SetTheme(bool isDarkTheme)
    {
        IsDarkTheme = isDarkTheme;
        _settings.IsDark = isDarkTheme;
        SaveSettings();
        RenderPreview();
    }

    private bool ConfirmDiscardChanges() =>
        !_isDirty || ResolvePendingChanges(AppText.ConfirmDiscardChanges);

    private bool ResolvePendingChanges(string message)
    {
        var choice = _dialogService.ConfirmSave(message);
        return choice switch
        {
            DialogChoice.Yes => SaveCurrent(),
            DialogChoice.No => true,
            _ => false
        };
    }

    private void OpenFile(string path)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            SetDocumentText(File.ReadAllText(fullPath));
            _filePath = fullPath;
            _isDirty = false;
            RememberFile(fullPath);
            WindowTitle = string.Format(
                AppText.WindowTitleFormat,
                Path.GetFileName(fullPath));
            StatusText = fullPath;
            RenderPreview();
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            _dialogService.ShowMessage(exception.Message);
        }
    }

    private void SetDocumentText(string text)
    {
        _isLoadingDocument = true;
        MarkdownText = text;
        _isLoadingDocument = false;
    }

    private bool SaveCurrent() =>
        _filePath is null ? SaveToNewPath() : SaveTo(_filePath);

    private bool SaveToNewPath()
    {
        var suggestedFileName = _filePath is null
            ? AppText.DefaultMarkdownFileName
            : Path.GetFileName(_filePath);
        var path = _dialogService.PickFileToSave(
            AppText.SaveMarkdownFilter,
            suggestedFileName,
            GetValidDirectory());

        return path is not null && SaveTo(path);
    }

    private bool SaveTo(string path)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
            File.WriteAllText(fullPath, MarkdownText, CreateUtf8Encoding());
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            _dialogService.ShowMessage(string.Format(
                AppText.SaveFailedFormat,
                exception.Message));
            return false;
        }

        _filePath = fullPath;
        _isDirty = false;
        RememberFile(_filePath);
        WindowTitle = string.Format(
            AppText.WindowTitleFormat,
            Path.GetFileName(_filePath));
        StatusText = string.Format(AppText.SavedStatusFormat, _filePath);
        return true;
    }

    private void SchedulePreviewRender()
    {
        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();
        _renderCancellation = new CancellationTokenSource();
        _ = RenderAfterDelayAsync(_renderCancellation.Token);
    }

    private async Task RenderAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                ApplicationConstants.PreviewRenderDelayMilliseconds,
                cancellationToken);
            RenderPreview();
        }
        catch (OperationCanceledException)
        {
            // A newer editor change superseded this pending render.
        }
    }

    private void RenderPreview()
    {
        if (_isPreviewReady)
        {
            PreviewHtml = BuildHtml(includePreviewBridge: true);
        }
    }

    private string BuildHtml(bool includePreviewBridge)
    {
        var title = WebUtility.HtmlEncode(
            Path.GetFileNameWithoutExtension(_filePath)
            ?? AppText.DefaultDocumentTitle);
        var styles = IsDarkTheme
            ? HtmlTemplates.DarkStyles
            : HtmlTemplates.LightStyles;
        var script = HtmlTemplates.InternalAnchorScript
            + (includePreviewBridge
                ? HtmlTemplates.ClickInterceptionScript
                : string.Empty);

        return HtmlTemplates.BuildDocument(
            BuildBaseTag(),
            title,
            styles,
            MarkdownConverter.ToHtml(MarkdownText),
            script);
    }

    private string BuildBaseTag()
    {
        if (_filePath is null)
        {
            return string.Empty;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrEmpty(directory))
        {
            return string.Empty;
        }

        var directoryPath = directory.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var directoryUri = new Uri(directoryPath);
        return string.Format(
            HtmlTemplates.BaseTagFormat,
            WebUtility.HtmlEncode(directoryUri.AbsoluteUri));
    }

    private void OpenLocalFile(string path)
    {
        // Anchors resolved against the base directory do not refer to real files.
        if (!File.Exists(path))
        {
            return;
        }

        if (SecurityConstants.DangerousFileExtensions.Contains(
            Path.GetExtension(path)))
        {
            _dialogService.ShowMessage(string.Format(
                AppText.BlockedTargetFormat,
                path));
            return;
        }

        if (_dialogService.Confirm(string.Format(
            AppText.ConfirmOpenFileFormat,
            path)))
        {
            LaunchTarget(path);
        }
    }

    private void LaunchTarget(string target)
    {
        var errorMessage = _shellLauncher.TryLaunch(target);
        if (errorMessage is not null)
        {
            _dialogService.ShowMessage(errorMessage);
        }
    }

    private void RememberFile(string path)
    {
        _settings.LastDirectory = Path.GetDirectoryName(path);
        _settings.RecentFiles.RemoveAll(
            recentPath => recentPath.Equals(
                path,
                StringComparison.OrdinalIgnoreCase));
        _settings.RecentFiles.Insert(0, path);

        if (_settings.RecentFiles.Count > ApplicationConstants.MaximumRecentFiles)
        {
            _settings.RecentFiles.RemoveRange(
                ApplicationConstants.MaximumRecentFiles,
                _settings.RecentFiles.Count
                - ApplicationConstants.MaximumRecentFiles);
        }

        SaveSettings();
        RefreshRecentFiles();
    }

    private void RememberDirectory(string path)
    {
        _settings.LastDirectory = Path.GetDirectoryName(path);
        SaveSettings();
    }

    private string? GetValidDirectory() =>
        !string.IsNullOrWhiteSpace(_settings.LastDirectory)
        && Directory.Exists(_settings.LastDirectory)
            ? _settings.LastDirectory
            : null;

    private void RefreshRecentFiles()
    {
        RecentFiles.Clear();

        foreach (var path in _settings.RecentFiles.Where(File.Exists))
        {
            RecentFiles.Add(new RecentFileEntry(path, path, true));
        }

        if (RecentFiles.Count == 0)
        {
            RecentFiles.Add(new RecentFileEntry(
                AppText.NoRecentFiles,
                null,
                false));
        }
    }

    private static UTF8Encoding CreateUtf8Encoding() =>
        new(DocumentConstants.EmitUtf8ByteOrderMark);

    private void SaveSettings() => _settingsService.Save(_settings);
}
