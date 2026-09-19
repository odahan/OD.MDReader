using MDRead.Constants;
using MDRead.Models;
using MDRead.Services;
using System.Text;

namespace MDRead.Tests;

/// <summary>
/// Verifies document lifecycle, persistence, export, and recent-file behavior.
/// </summary>
public sealed class MainWindowViewModelFileTests
{
    [Fact]
    public void OpenDocument_LoadsContentAndUpdatesPresentationState()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("sample.md");
        File.WriteAllText(documentPath, "# Sample");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();

        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Equal("# Sample", viewModel.MarkdownText);
        Assert.Equal(
            string.Format(AppText.WindowTitleFormat, "sample.md"),
            viewModel.WindowTitle);
        Assert.Equal(Path.GetFullPath(documentPath), viewModel.StatusText);
        Assert.Equal(
            temporaryDirectory.Path,
            context.Settings.Settings.LastDirectory);
        Assert.Equal(
            Path.GetFullPath(documentPath),
            Assert.Single(context.Settings.Settings.RecentFiles));
    }

    [Fact]
    public void OpenDroppedFile_LoadsMarkdownDocument()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("dropped.markdown");
        File.WriteAllText(documentPath, "# Dropped");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenDroppedFile(documentPath);

        Assert.Equal("# Dropped", viewModel.MarkdownText);
        Assert.Equal(Path.GetFullPath(documentPath), viewModel.StatusText);
    }

    [Fact]
    public void ReloadDocument_ReloadsTheAssociatedFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("reload.md");
        File.WriteAllText(documentPath, "Original");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();
        viewModel.InitializePreview();
        viewModel.OpenDocumentCommand.Execute(null);
        var initialPreview = viewModel.PreviewHtml;
        File.WriteAllText(documentPath, "Reloaded");

        viewModel.ReloadDocumentCommand.Execute(null);

        Assert.Equal("Reloaded", viewModel.MarkdownText);
        Assert.Equal(
            string.Format(AppText.ReloadedStatusFormat, Path.GetFullPath(documentPath)),
            viewModel.StatusText);
        Assert.NotEqual(initialPreview, viewModel.PreviewHtml);
        Assert.Contains("_mdreadReload=1", viewModel.PreviewHtml);
    }

    [Fact]
    public void ReloadDocument_IsDisabledForAnUnsavedNewDocument()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.NewDocumentCommand.Execute(null);

        Assert.False(viewModel.ReloadDocumentCommand.CanExecute(null));
    }

    [Fact]
    public void ReloadDocument_IsEnabledAfterSavingANewDocument()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("saved.md");
        var context = new ViewModelTestContext();
        context.Dialogs.SavePath = documentPath;
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Saved document";

        viewModel.SaveCommand.Execute(null);

        Assert.True(viewModel.ReloadDocumentCommand.CanExecute(null));
    }

    [Fact]
    public void OpenDocument_UsesVirtualHostBaseForRelativeResources()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var chapterDirectory = temporaryDirectory.GetPath("Manuscript");
        Directory.CreateDirectory(chapterDirectory);
        var documentPath = Path.Combine(chapterDirectory, "chapter.md");
        File.WriteAllText(documentPath, "![Figure](../Illustrations/figure.svg)");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();

        viewModel.InitializePreview();
        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Equal(Path.GetPathRoot(documentPath), viewModel.PreviewResourceRoot);
        var root = Path.GetPathRoot(documentPath)!;
        var relativeDirectory = Path.GetRelativePath(root, chapterDirectory)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Trim('/');
        var expectedBaseUri = new Uri(
            new Uri(WebView2Constants.PreviewVirtualHostAddress),
            relativeDirectory + "/");
        Assert.Contains(
            $"<base href=\"{expectedBaseUri.AbsoluteUri}\">",
            viewModel.PreviewHtml);
        Assert.Contains("src=\"../Illustrations/figure.svg\"", viewModel.PreviewHtml);
    }

    [Fact]
    public void OpenDroppedFile_IgnoresUnsupportedFiles()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("dropped.txt");
        File.WriteAllText(documentPath, "Not Markdown");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenDroppedFile(documentPath);

        Assert.Equal(string.Empty, viewModel.MarkdownText);
        Assert.Empty(context.Settings.Settings.RecentFiles);
    }

    [Fact]
    public void InitializePreview_OpensStartupDocument()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("startup.md");
        File.WriteAllText(documentPath, "Startup");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel("--dark", documentPath);

        viewModel.InitializePreview();

        Assert.Equal("Startup", viewModel.MarkdownText);
        Assert.Contains("<p>Startup</p>", viewModel.PreviewHtml);
    }

    [Fact]
    public void OpenDocument_UsesExistingLastDirectory()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var context = new ViewModelTestContext();
        context.Settings.Settings.LastDirectory = temporaryDirectory.Path;
        var viewModel = context.CreateViewModel();

        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Equal(
            temporaryDirectory.Path,
            context.Dialogs.LastOpenInitialDirectory);
    }

    [Fact]
    public void OpenDocument_IgnoresMissingLastDirectory()
    {
        var context = new ViewModelTestContext();
        context.Settings.Settings.LastDirectory =
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var viewModel = context.CreateViewModel();

        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Null(context.Dialogs.LastOpenInitialDirectory);
    }

    [Fact]
    public void OpenDocument_ReadFailureDisplaysMessage()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = temporaryDirectory.GetPath("missing.md");
        var viewModel = context.CreateViewModel();

        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Single(context.Dialogs.Messages);
        Assert.Equal(string.Empty, viewModel.MarkdownText);
    }

    [Fact]
    public void OpenDocument_CanceledPendingChangesPreserveCurrentDocument()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("replacement.md");
        File.WriteAllText(documentPath, "Replacement");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Unsaved";
        context.Dialogs.EnqueueSaveConfirmation(DialogChoice.Cancel);

        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Equal("Unsaved", viewModel.MarkdownText);
    }

    [Fact]
    public void Save_NewDocumentWritesUtf8WithoutByteOrderMark()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var savePath = temporaryDirectory.GetPath("saved.md");
        var context = new ViewModelTestContext();
        context.Dialogs.SavePath = savePath;
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Résumé";

        viewModel.SaveCommand.Execute(null);

        Assert.Equal("Résumé", File.ReadAllText(savePath, Encoding.UTF8));
        var bytes = File.ReadAllBytes(savePath);
        Assert.False(
            bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF);
        Assert.Equal(AppText.DefaultMarkdownFileName, context.Dialogs.LastSuggestedFileName);
        Assert.Equal(
            string.Format(AppText.SavedStatusFormat, Path.GetFullPath(savePath)),
            viewModel.StatusText);
    }

    [Fact]
    public void Save_ExistingDocumentDoesNotPromptForAnotherPath()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("existing.md");
        File.WriteAllText(documentPath, "Initial");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();
        viewModel.OpenDocumentCommand.Execute(null);
        context.Dialogs.SavePath = temporaryDirectory.GetPath("unused.md");
        viewModel.MarkdownText = "Updated";

        viewModel.SaveCommand.Execute(null);

        Assert.Equal("Updated", File.ReadAllText(documentPath));
        Assert.False(File.Exists(context.Dialogs.SavePath));
    }

    [Fact]
    public void SaveAs_UsesCurrentFileNameAsSuggestion()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("current.md");
        var targetPath = temporaryDirectory.GetPath("copy.md");
        File.WriteAllText(documentPath, "Content");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();
        viewModel.OpenDocumentCommand.Execute(null);
        context.Dialogs.SavePath = targetPath;

        viewModel.SaveAsCommand.Execute(null);

        Assert.Equal("current.md", context.Dialogs.LastSuggestedFileName);
        Assert.Equal("Content", File.ReadAllText(targetPath));
    }

    [Fact]
    public void Save_FailureDisplaysCentralizedMessage()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var context = new ViewModelTestContext();
        context.Dialogs.SavePath = temporaryDirectory.Path;
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Content";

        viewModel.SaveCommand.Execute(null);

        var message = Assert.Single(context.Dialogs.Messages);
        Assert.StartsWith("Could not save the file.", message.Message);
    }

    [Fact]
    public void ExportHtml_CreatesStandaloneDocumentWithoutWebViewBridge()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("source.md");
        var exportPath = temporaryDirectory.GetPath("source.html");
        File.WriteAllText(documentPath, "# Export");
        var context = new ViewModelTestContext();
        context.Dialogs.OpenPath = documentPath;
        var viewModel = context.CreateViewModel();
        viewModel.OpenDocumentCommand.Execute(null);
        context.Dialogs.SavePath = exportPath;

        viewModel.ExportHtmlCommand.Execute(null);

        var html = File.ReadAllText(exportPath);
        Assert.Contains("<h1 id=\"export\">Export</h1>", html);
        Assert.Contains("<title>source</title>", html);
        Assert.Contains("<base href=", html);
        Assert.Contains(HtmlTemplates.InternalAnchorScript, html);
        Assert.DoesNotContain(HtmlTemplates.ClickInterceptionScript, html);
        Assert.Equal(
            string.Format(AppText.ExportedStatusFormat, exportPath),
            viewModel.StatusText);
    }

    [Fact]
    public void ExportHtml_UsesDefaultNameForUnsavedDocument()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.ExportHtmlCommand.Execute(null);

        Assert.Equal(AppText.DefaultHtmlFileName, context.Dialogs.LastSuggestedFileName);
    }

    [Fact]
    public void ExportHtml_WriteFailureDisplaysCentralizedMessage()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var context = new ViewModelTestContext();
        context.Dialogs.SavePath = temporaryDirectory.Path;
        var viewModel = context.CreateViewModel();

        viewModel.ExportHtmlCommand.Execute(null);

        var message = Assert.Single(context.Dialogs.Messages);
        Assert.StartsWith("Could not export the file.", message.Message);
    }

    [Fact]
    public void NewDocument_CancelPreservesUnsavedContent()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Unsaved";
        context.Dialogs.EnqueueSaveConfirmation(DialogChoice.Cancel);

        viewModel.NewDocumentCommand.Execute(null);

        Assert.Equal("Unsaved", viewModel.MarkdownText);
    }

    [Fact]
    public void NewDocument_NoDiscardsUnsavedContent()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Unsaved";
        context.Dialogs.EnqueueSaveConfirmation(DialogChoice.No);

        viewModel.NewDocumentCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.MarkdownText);
        Assert.Equal(AppText.NewDocumentStatus, viewModel.StatusText);
        Assert.Equal(AppText.UntitledWindowTitle, viewModel.WindowTitle);
    }

    [Fact]
    public void NewDocument_YesSavesBeforeClearing()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var savePath = temporaryDirectory.GetPath("preserved.md");
        var context = new ViewModelTestContext();
        context.Dialogs.SavePath = savePath;
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Preserve me";
        context.Dialogs.EnqueueSaveConfirmation(DialogChoice.Yes);

        viewModel.NewDocumentCommand.Execute(null);

        Assert.Equal("Preserve me", File.ReadAllText(savePath));
        Assert.Equal(string.Empty, viewModel.MarkdownText);
    }

    [Fact]
    public void CanClose_YesSavesUnsavedDocument()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var savePath = temporaryDirectory.GetPath("closing.md");
        var context = new ViewModelTestContext();
        context.Dialogs.SavePath = savePath;
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Closing";
        context.Dialogs.EnqueueSaveConfirmation(DialogChoice.Yes);

        var canClose = viewModel.CanClose();

        Assert.True(canClose);
        Assert.Equal("Closing", File.ReadAllText(savePath));
    }

    [Fact]
    public void RecentFiles_OnlyDisplaysExistingPaths()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var existingPath = temporaryDirectory.GetPath("existing.md");
        File.WriteAllText(existingPath, "Existing");
        var context = new ViewModelTestContext();
        context.Settings.Settings.RecentFiles =
        [
            existingPath,
            temporaryDirectory.GetPath("missing.md")
        ];

        var viewModel = context.CreateViewModel();

        var entry = Assert.Single(viewModel.RecentFiles);
        Assert.True(entry.IsEnabled);
        Assert.Equal(existingPath, entry.FilePath);
    }

    [Fact]
    public void RecentFiles_DisplaysDisabledPlaceholderWhenEmpty()
    {
        var context = new ViewModelTestContext();

        var viewModel = context.CreateViewModel();

        var entry = Assert.Single(viewModel.RecentFiles);
        Assert.False(entry.IsEnabled);
        Assert.Null(entry.FilePath);
        Assert.Equal(AppText.NoRecentFiles, entry.DisplayName);
    }

    [Fact]
    public void OpenRecentFile_LoadsSelectedEntry()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var documentPath = temporaryDirectory.GetPath("recent.md");
        File.WriteAllText(documentPath, "Recent");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenRecentFileCommand.Execute(
            new RecentFileEntry(documentPath, documentPath, true));

        Assert.Equal("Recent", viewModel.MarkdownText);
    }

    [Fact]
    public void RememberFile_TrimsRecentListToConfiguredMaximum()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var recentFiles = Enumerable.Range(0, 12)
            .Select(index => temporaryDirectory.GetPath($"recent-{index}.md"))
            .ToList();
        foreach (var path in recentFiles)
        {
            File.WriteAllText(path, path);
        }

        var newPath = temporaryDirectory.GetPath("new.md");
        File.WriteAllText(newPath, "New");
        var context = new ViewModelTestContext();
        context.Settings.Settings.RecentFiles = recentFiles;
        context.Dialogs.OpenPath = newPath;
        var viewModel = context.CreateViewModel();

        viewModel.OpenDocumentCommand.Execute(null);

        Assert.Equal(
            ApplicationConstants.MaximumRecentFiles,
            context.Settings.Settings.RecentFiles.Count);
        Assert.Equal(
            Path.GetFullPath(newPath),
            context.Settings.Settings.RecentFiles[0]);
    }
}
