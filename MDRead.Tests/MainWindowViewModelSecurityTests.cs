using MDRead.Constants;
using System.Text.RegularExpressions;

namespace MDRead.Tests;

/// <summary>
/// Verifies preview-target validation and shell-launch behavior.
/// </summary>
public sealed class MainWindowViewModelSecurityTests
{
    [Theory]
    [InlineData("https://example.com/docs")]
    [InlineData("http://example.com/")]
    [InlineData("mailto:test@example.com")]
    public void OpenPreviewTarget_ConfirmedExternalUriLaunches(string target)
    {
        var context = new ViewModelTestContext();
        context.Dialogs.EnqueueConfirmation(true);
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget(target);

        Assert.Equal(target, Assert.Single(context.Shell.Targets));
    }

    [Fact]
    public void OpenPreviewTarget_RejectedExternalUriDoesNotLaunch()
    {
        var context = new ViewModelTestContext();
        context.Dialogs.EnqueueConfirmation(false);
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget("https://example.com/");

        Assert.Empty(context.Shell.Targets);
    }

    [Fact]
    public void OpenPreviewTarget_OpensRelativeMarkdownFromParentDirectory()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var specDirectory = temporaryDirectory.GetPath("Specs/003 # profiles");
        Directory.CreateDirectory(specDirectory);
        var docsDirectory = temporaryDirectory.GetPath("Docs");
        Directory.CreateDirectory(docsDirectory);
        var source = Path.Combine(specDirectory, "SPEC-003.md");
        var destination = Path.Combine(docsDirectory, "Gap Analysis %.markdown");
        File.WriteAllText(source, "[Gaps](../../Docs/Gap%20Analysis%20%25.markdown)");
        File.WriteAllText(destination, "# Gap Analysis");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel(source);
        viewModel.InitializePreview();
        var baseUrl = Regex.Match(
            viewModel.PreviewHtml, "<base href=\"([^\"]+)\">").Groups[1].Value;
        var target = new Uri(new Uri(baseUrl), "../../Docs/Gap%20Analysis%20%25.markdown");

        Assert.Contains("003%20%23%20profiles/", baseUrl);
        viewModel.OpenPreviewTarget(target.AbsoluteUri);

        Assert.Equal("# Gap Analysis", viewModel.MarkdownText);
        Assert.Equal(destination, viewModel.StatusText);
        Assert.Empty(context.Shell.Targets);
        Assert.Empty(context.Dialogs.Messages);
    }

    [Fact]
    public void OpenPreviewTarget_VirtualHostWithoutDocumentDoesNotLaunch()
    {
        var context = new ViewModelTestContext();
        context.Dialogs.EnqueueConfirmation(true);
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget(WebView2Constants.PreviewVirtualHostAddress + "document.md");

        Assert.Empty(context.Shell.Targets);
        Assert.Empty(context.Dialogs.Messages);
    }

    [Fact]
    public void OpenPreviewTarget_SafeLocalFileLaunchesAfterConfirmation()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var filePath = temporaryDirectory.GetPath("notes.txt");
        File.WriteAllText(filePath, "Notes");
        var context = new ViewModelTestContext();
        context.Dialogs.EnqueueConfirmation(true);
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget(new Uri(filePath).AbsoluteUri);

        Assert.Equal(filePath, Assert.Single(context.Shell.Targets));
    }

    [Theory]
    [InlineData("program.exe")]
    [InlineData("script.ps1")]
    [InlineData("shortcut.lnk")]
    public void OpenPreviewTarget_DangerousLocalFileIsBlocked(string fileName)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var filePath = temporaryDirectory.GetPath(fileName);
        File.WriteAllText(filePath, "Blocked");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget(new Uri(filePath).AbsoluteUri);

        Assert.Empty(context.Shell.Targets);
        var message = Assert.Single(context.Dialogs.Messages);
        Assert.Contains(filePath, message.Message);
        Assert.StartsWith("This link was blocked", message.Message);
    }

    [Fact]
    public void OpenPreviewTarget_DangerousVirtualFileIsBlocked()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var document = temporaryDirectory.GetPath("links.md");
        var program = temporaryDirectory.GetPath("program.exe");
        File.WriteAllText(document, "[Program](program.exe)");
        File.WriteAllText(program, string.Empty);
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel(document);
        viewModel.InitializePreview();
        var baseUrl = Regex.Match(
            viewModel.PreviewHtml, "<base href=\"([^\"]+)\">").Groups[1].Value;
        var target = new Uri(new Uri(baseUrl), "program.exe");

        viewModel.OpenPreviewTarget(target.AbsoluteUri);

        Assert.Empty(context.Shell.Targets);
        Assert.Contains(program, Assert.Single(context.Dialogs.Messages).Message);
    }

    [Fact]
    public void OpenPreviewTarget_MissingLocalFileIsIgnored()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var filePath = temporaryDirectory.GetPath("missing.txt");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget(new Uri(filePath).AbsoluteUri);

        Assert.Empty(context.Shell.Targets);
        Assert.Empty(context.Dialogs.Messages);
    }

    [Fact]
    public void OpenPreviewTarget_UnsupportedSchemeIsBlocked()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget("ftp://example.com/file.txt");

        Assert.Empty(context.Shell.Targets);
        Assert.Single(context.Dialogs.Messages);
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/path")]
    [InlineData("not a URI")]
    public void OpenPreviewTarget_InvalidTargetIsIgnored(string target)
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget(target);

        Assert.Empty(context.Shell.Targets);
        Assert.Empty(context.Dialogs.Messages);
    }

    [Fact]
    public void OpenPreviewTarget_ShellErrorIsDisplayed()
    {
        var context = new ViewModelTestContext();
        context.Dialogs.EnqueueConfirmation(true);
        context.Shell.Error = "No associated application";
        var viewModel = context.CreateViewModel();

        viewModel.OpenPreviewTarget("https://example.com/");

        var message = Assert.Single(context.Dialogs.Messages);
        Assert.Equal(context.Shell.Error, message.Message);
    }
}
