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
    private readonly IDesktopShortcutService _desktopShortcutService;
    private readonly IFileAssociationService _fileAssociationService;
    private readonly IShellLauncher _shellLauncher;
    private readonly ISettingsService _settingsService;
    private readonly UserSettings _settings;
    private readonly string[] _startupArguments;
    private readonly Stack<(string Path, string Fragment)> _backHistory = new();
    private CancellationTokenSource? _renderCancellation;
    private string? _filePath;
    private string _fileFragment = string.Empty;
    private bool _isDirty;
    private bool _isLoadingDocument;
    private bool _isPreviewReady;
    private long _previewResourceVersion;

    [ObservableProperty]
    private string _markdownText = string.Empty;

    [ObservableProperty]
    private string _windowTitle = AppText.UntitledWindowTitle;

    [ObservableProperty]
    private string _statusText = AppText.ReadyStatus;

    [ObservableProperty]
    private string _previewHtml = string.Empty;

    [ObservableProperty]
    private string? _previewResourceRoot;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isMarkdownTabSelected;

    [ObservableProperty]
    private bool _isWordWrapEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dialogService">The modal dialog abstraction.</param>
    /// <param name="editorOperations">The selection-aware editor operations.</param>
    /// <param name="desktopShortcutService">The desktop shortcut service.</param>
    /// <param name="fileAssociationService">The current-user file association service.</param>
    /// <param name="shellLauncher">The approved-target launcher.</param>
    /// <param name="settingsService">The settings persistence service.</param>
    /// <param name="startupArguments">The command-line arguments.</param>
    public MainWindowViewModel(
        IDialogService dialogService,
        IEditorTextOperations editorOperations,
        IDesktopShortcutService desktopShortcutService,
        IFileAssociationService fileAssociationService,
        IShellLauncher shellLauncher,
        ISettingsService settingsService,
        IEnumerable<string> startupArguments)
    {
        _dialogService = dialogService;
        _editorOperations = editorOperations;
        _desktopShortcutService = desktopShortcutService;
        _fileAssociationService = fileAssociationService;
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

    /// <summary>Gets the Markdown snapshot used by the latest generated preview.</summary>
    internal string? PreviewSource { get; private set; }

    /// <summary>Gets the revision identifying the latest generated preview.</summary>
    internal long PreviewRevision { get; private set; }

    /// <summary>
    /// Flushes any pending editor changes before synchronizing to the HTML preview.
    /// </summary>
    internal void RefreshPreviewForSynchronization()
    {
        if (PreviewSource != MarkdownText)
        {
            RenderPreview();
        }
    }

    /// <summary>
    /// Gets whether the supported Markdown extensions are currently associated with this executable.
    /// </summary>
    public bool IsMarkdownFileAssociationRegistered =>
        _fileAssociationService.IsMarkdownAssociationRegistered();

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

        OpenStartupTarget(startupPath);
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
    /// <param name="target">The resolved link or drive-qualified reference supplied by WebView2.</param>
    public void OpenPreviewTarget(string target)
    {
        if (!Uri.TryCreate(NormalizeDriveReference(target), UriKind.Absolute, out var uri))
        {
            return;
        }

        if (uri.Host.Equals(WebView2Constants.PreviewVirtualHostName, StringComparison.OrdinalIgnoreCase))
        {
            var path = GetPreviewResourcePath(uri);
            if (path is not null)
            {
                OpenLocalFile(path, uri.Fragment);
            }

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
            OpenLocalFile(uri.LocalPath, uri.Fragment);
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

        RememberCurrentVisit();
        SetDocumentText(string.Empty);
        _filePath = null;
        _fileFragment = string.Empty;
        ReloadDocumentCommand.NotifyCanExecuteChanged();
        GoBackCommand.NotifyCanExecuteChanged();
        PreviewResourceRoot = null;
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

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        if (_backHistory.TryPeek(out var previous))
        {
            OpenFile(previous.Path, fragment: previous.Fragment, fromHistory: true);
        }
    }

    [RelayCommand(CanExecute = nameof(CanReloadDocument))]
    private void ReloadDocument()
    {
        if (_filePath is not null)
        {
            OpenFile(_filePath, isReload: true, fragment: _fileFragment);
        }
    }

    [RelayCommand]
    private void CreateDesktopShortcut()
    {
        var result = _desktopShortcutService.CreateDesktopShortcut();
        StatusText = result.ShortcutPath is not null
            ? string.Format(
                AppText.DesktopShortcutCreatedStatusFormat,
                result.ShortcutPath)
            : string.Format(
                AppText.DesktopShortcutFailedStatusFormat,
                result.ErrorMessage ?? string.Empty);
    }

    [RelayCommand]
    private void AssociateMarkdownFiles()
    {
        var result = _fileAssociationService.AssociateMarkdownFiles();
        StatusText = result.IsSuccessful
            ? AppText.MarkdownFilesAssociatedStatus
            : string.Format(
                AppText.MarkdownFileAssociationFailedStatusFormat,
                result.ErrorMessage ?? string.Empty);
        OnPropertyChanged(nameof(IsMarkdownFileAssociationRegistered));
    }

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
        _dialogService.ShowAbout(
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

    [RelayCommand]
    private void SynchronizeHtml() => _editorOperations.SynchronizeHtmlPreview();

    [RelayCommand]
    private void SynchronizeMarkdown() => _editorOperations.SynchronizeMarkdownEditor();

    partial void OnMarkdownTextChanged(string value)
    {
        if (_isLoadingDocument)
        {
            return;
        }

        _isDirty = true;
        SchedulePreviewRender();
    }

    partial void OnIsMarkdownTabSelectedChanged(bool value)
    {
        ExitPreviewCommand.NotifyCanExecuteChanged();

        if (value)
        {
            _editorOperations.FocusEditor();
        }
    }

    private bool CanExitPreview() => !IsMarkdownTabSelected;

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

    private bool CanReloadDocument() => _filePath is not null;

    private bool CanGoBack() => _backHistory.Count > 0;

    /// <summary>Records the open file and its entry anchor before leaving it.</summary>
    private void RememberCurrentVisit()
    {
        if (_filePath is not null)
        {
            _backHistory.Push((_filePath, _fileFragment));
        }
    }

    /// <summary>Converts a slash-prefixed Windows drive reference into a file URI.</summary>
    private static string NormalizeDriveReference(string target) =>
        target.Length >= 4 && target[0] == '/' && char.IsAsciiLetter(target[1])
            && target[2] == ':' && target[3] == '/'
            ? "file://" + target
            : target;

    /// <summary>Opens a command-line path or URI with an optional Markdown fragment.</summary>
    private void OpenStartupTarget(string target)
    {
        var normalized = NormalizeDriveReference(target);
        if (normalized.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            OpenFile(uri.LocalPath, fragment: uri.Fragment);
            return;
        }

        var separator = target.LastIndexOf('#');
        if (separator > 0 && DocumentConstants.IsMarkdownFilePath(target[..separator])
            && !File.Exists(target))
        {
            OpenFile(target[..separator], fragment: target[separator..]);
            return;
        }

        OpenFile(target);
    }

    private void OpenFile(string path, bool isReload = false, string fragment = "", bool fromHistory = false)
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        string fullPath;
        string text;
        try
        {
            fullPath = Path.GetFullPath(path);
            text = File.ReadAllText(fullPath);
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException)
        {
            if (fromHistory && exception is FileNotFoundException or DirectoryNotFoundException)
            {
                _backHistory.Pop();
                GoBackCommand.NotifyCanExecuteChanged();
            }

            _dialogService.ShowMessage(exception.Message);
            return;
        }

        if (fromHistory)
        {
            _backHistory.Pop();
        }
        else if (!string.Equals(_filePath, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            RememberCurrentVisit();
        }

        SetDocumentText(text);
        _filePath = fullPath;
        _fileFragment = fragment;
        ReloadDocumentCommand.NotifyCanExecuteChanged();
        GoBackCommand.NotifyCanExecuteChanged();
        PreviewResourceRoot = Path.GetPathRoot(fullPath);
        _isDirty = false;
        RememberFile(fullPath);
        WindowTitle = string.Format(
            AppText.WindowTitleFormat,
            Path.GetFileName(fullPath));
        StatusText = isReload
            ? string.Format(AppText.ReloadedStatusFormat, fullPath)
            : fullPath;
        RenderPreview(forceResourceRefresh: isReload, fragment: fragment);
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

        var pathChanged = !string.Equals(_filePath, fullPath, StringComparison.Ordinal);
        _filePath = fullPath;
        ReloadDocumentCommand.NotifyCanExecuteChanged();
        PreviewResourceRoot = Path.GetPathRoot(fullPath);
        _isDirty = false;
        RememberFile(_filePath);
        WindowTitle = string.Format(
            AppText.WindowTitleFormat,
            Path.GetFileName(_filePath));
        StatusText = string.Format(AppText.SavedStatusFormat, _filePath);
        if (pathChanged)
        {
            RenderPreview();
        }

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
            cancellationToken.ThrowIfCancellationRequested();
            RenderPreview();
        }
        catch (OperationCanceledException)
        {
            // A newer editor change superseded this pending render.
        }
    }

    private void RenderPreview(bool forceResourceRefresh = false, string fragment = "")
    {
        if (_isPreviewReady)
        {
            _renderCancellation?.Cancel();
            PreviewSource = MarkdownText;
            PreviewRevision++;
            PreviewHtml = BuildHtml(
                includePreviewBridge: true,
                forceResourceRefresh: forceResourceRefresh,
                fragment: fragment);
        }
    }

    private string BuildHtml(
        bool includePreviewBridge,
        bool forceResourceRefresh = false,
        string fragment = "")
    {
        var title = WebUtility.HtmlEncode(
            Path.GetFileNameWithoutExtension(_filePath)
            ?? AppText.DefaultDocumentTitle);
        var styles = IsDarkTheme
            ? HtmlTemplates.DarkStyles
            : HtmlTemplates.LightStyles;
        var script = HtmlTemplates.InternalAnchorScript
            + (includePreviewBridge
                ? HtmlTemplates.ClickInterceptionScript + PreviewSynchronization.CreateScript(PreviewRevision)
                : string.Empty)
            + (forceResourceRefresh
                ? HtmlTemplates.CreateResourceReloadScript(++_previewResourceVersion)
                : string.Empty)
            + (fragment.Length > 1 ? HtmlTemplates.CreateInitialAnchorScript(fragment) : string.Empty);

        return HtmlTemplates.BuildDocument(
            BuildBaseTag(),
            title,
            includePreviewBridge ? styles + PreviewSynchronization.Styles : styles,
            includePreviewBridge
                ? MarkdownConverter.ToPreviewHtml(MarkdownText)
                : MarkdownConverter.ToHtml(MarkdownText),
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
        var root = Path.GetPathRoot(directoryPath);
        if (string.IsNullOrEmpty(root))
        {
            return string.Empty;
        }

        var relativeDirectory = string.Join(
            '/',
            directoryPath[root.Length..]
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
        var baseUri = new Uri(
            new Uri(WebView2Constants.PreviewVirtualHostAddress),
            string.IsNullOrEmpty(relativeDirectory)
                ? string.Empty
                : relativeDirectory + "/");
        return string.Format(
            HtmlTemplates.BaseTagFormat,
            WebUtility.HtmlEncode(baseUri.AbsoluteUri));
    }

    /// <summary>
    /// Resolves a virtual preview URL against the volume mapped by WebView2.
    /// </summary>
    private string? GetPreviewResourcePath(Uri uri)
    {
        if (PreviewResourceRoot is not { } root
            || uri.Scheme != Uri.UriSchemeHttps
            || !uri.IsDefaultPort
            || uri.UserInfo.Length != 0)
        {
            return null;
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        if (segments.Length == 0 || segments.Any(segment =>
                segment.EndsWith('.') || segment.EndsWith(' ')
                || segment.IndexOfAny(invalidCharacters) >= 0))
        {
            return null;
        }

        return Path.Combine(root, string.Join(Path.DirectorySeparatorChar, segments));
    }

    private void OpenLocalFile(string path, string fragment)
    {
        if (!File.Exists(path))
        {
            return;
        }

        if (DocumentConstants.IsMarkdownFilePath(path))
        {
            if (fragment.Length > 1 && string.Equals(
                    Path.GetFullPath(path), _filePath, StringComparison.OrdinalIgnoreCase))
            {
                _fileFragment = fragment;
                RenderPreview(fragment: fragment);
                return;
            }

            OpenFile(path, fragment: fragment);
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
