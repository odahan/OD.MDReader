using MDRead.Models;
using MDRead.Services;
using MDRead.ViewModels;

namespace MDRead.Tests;

/// <summary>
/// Provides configurable service doubles used by view-model tests.
/// </summary>
internal sealed class ViewModelTestContext
{
    /// <summary>Gets the fake dialog service.</summary>
    public FakeDialogService Dialogs { get; } = new();

    /// <summary>Gets the fake editor adapter.</summary>
    public FakeEditorTextOperations Editor { get; } = new();

    /// <summary>Gets the fake shell launcher.</summary>
    public FakeShellLauncher Shell { get; } = new();

    /// <summary>Gets the fake settings service.</summary>
    public FakeSettingsService Settings { get; } = new();

    /// <summary>Creates a view model backed by the context's service doubles.</summary>
    public MainWindowViewModel CreateViewModel(params string[] startupArguments) =>
        new(Dialogs, Editor, Shell, Settings, startupArguments);
}

/// <summary>
/// Records dialog requests and returns configured responses.
/// </summary>
internal sealed class FakeDialogService : IDialogService
{
    private readonly Queue<bool> _confirmations = [];
    private readonly Queue<DialogChoice> _saveConfirmations = [];

    /// <summary>Gets recorded informational messages.</summary>
    public List<(string Message, string? Title)> Messages { get; } = [];

    /// <summary>Gets or sets the path returned by the open-file picker.</summary>
    public string? OpenPath { get; set; }

    /// <summary>Gets or sets the path returned by the save-file picker.</summary>
    public string? SavePath { get; set; }

    /// <summary>Gets the most recent open-file initial directory.</summary>
    public string? LastOpenInitialDirectory { get; private set; }

    /// <summary>Gets the most recent save-file initial directory.</summary>
    public string? LastSaveInitialDirectory { get; private set; }

    /// <summary>Gets the most recent suggested save-file name.</summary>
    public string? LastSuggestedFileName { get; private set; }

    /// <summary>Adds a yes-or-no response to the queue.</summary>
    public void EnqueueConfirmation(bool value) => _confirmations.Enqueue(value);

    /// <summary>Adds a pending-change response to the queue.</summary>
    public void EnqueueSaveConfirmation(DialogChoice value) =>
        _saveConfirmations.Enqueue(value);

    /// <inheritdoc />
    public void ShowMessage(string message, string? title = null) =>
        Messages.Add((message, title));

    /// <inheritdoc />
    public bool Confirm(string message, string? title = null) =>
        _confirmations.Count > 0 && _confirmations.Dequeue();

    /// <inheritdoc />
    public DialogChoice ConfirmSave(string message, string? title = null) =>
        _saveConfirmations.Count > 0
            ? _saveConfirmations.Dequeue()
            : DialogChoice.Cancel;

    /// <inheritdoc />
    public string? PickFileToOpen(string filter, string? initialDirectory)
    {
        LastOpenInitialDirectory = initialDirectory;
        return OpenPath;
    }

    /// <inheritdoc />
    public string? PickFileToSave(
        string filter,
        string suggestedFileName,
        string? initialDirectory)
    {
        LastSaveInitialDirectory = initialDirectory;
        LastSuggestedFileName = suggestedFileName;
        return SavePath;
    }
}

/// <summary>
/// Records selection-aware editor requests.
/// </summary>
internal sealed class FakeEditorTextOperations : IEditorTextOperations
{
    /// <summary>Gets the recorded wrap requests.</summary>
    public List<(string Prefix, string Suffix)> WrapRequests { get; } = [];

    /// <summary>Gets the recorded line-prefix requests.</summary>
    public List<string> PrefixRequests { get; } = [];

    /// <summary>Gets the number of focus requests.</summary>
    public int FocusRequestCount { get; private set; }

    /// <inheritdoc />
    public void WrapSelection(string prefix, string suffix) =>
        WrapRequests.Add((prefix, suffix));

    /// <inheritdoc />
    public void PrefixSelectedLines(string prefix) =>
        PrefixRequests.Add(prefix);

    /// <inheritdoc />
    public void FocusEditor() => FocusRequestCount++;
}

/// <summary>
/// Records targets submitted to the operating-system shell.
/// </summary>
internal sealed class FakeShellLauncher : IShellLauncher
{
    /// <summary>Gets launched targets.</summary>
    public List<string> Targets { get; } = [];

    /// <summary>Gets or sets the error returned for each launch request.</summary>
    public string? Error { get; set; }

    /// <inheritdoc />
    public string? TryLaunch(string target)
    {
        Targets.Add(target);
        return Error;
    }
}

/// <summary>
/// Stores user settings in memory.
/// </summary>
internal sealed class FakeSettingsService : ISettingsService
{
    /// <summary>Gets or sets the settings returned by <see cref="Load"/>.</summary>
    public UserSettings Settings { get; set; } = new();

    /// <summary>Gets the number of save operations.</summary>
    public int SaveCount { get; private set; }

    /// <inheritdoc />
    public UserSettings Load() => Settings;

    /// <inheritdoc />
    public void Save(UserSettings settings)
    {
        Settings = settings;
        SaveCount++;
    }
}

/// <summary>
/// Owns an isolated temporary directory and deletes it after a test.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    /// <summary>
    /// Initializes a new isolated directory.
    /// </summary>
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            nameof(MDRead),
            nameof(MDRead.Tests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the temporary directory path.</summary>
    public string Path { get; }

    /// <summary>Builds a path inside the temporary directory.</summary>
    public string GetPath(string fileName) => System.IO.Path.Combine(Path, fileName);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
