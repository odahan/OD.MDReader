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

    [Theory]
    [InlineData("relative")]
    [InlineData("file-uri")]
    [InlineData("drive-reference")]
    public void OpenPreviewTarget_PreservesMarkdownHeadingFragment(string linkType)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var source = temporaryDirectory.GetPath("links.md");
        var destination = temporaryDirectory.GetPath("Package #1.md");
        const string fragment = "#document-reranking";
        var fileUri = new Uri(destination);
        var href = linkType switch
        {
            "relative" => "Package%20%231.md" + fragment,
            "file-uri" => fileUri.AbsoluteUri + fragment,
            _ => "/" + fileUri.AbsoluteUri["file:///".Length..] + fragment
        };
        File.WriteAllText(source, $"[Reranking]({href})");
        File.WriteAllText(destination, "## Document reranking");
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel(source);
        viewModel.InitializePreview();
        Assert.Contains($"href=\"{href}\"", viewModel.PreviewHtml);
        var baseUrl = Regex.Match(
            viewModel.PreviewHtml, "<base href=\"([^\"]+)\">").Groups[1].Value;
        var target = linkType == "relative" ? new Uri(new Uri(baseUrl), href).AbsoluteUri : href;

        viewModel.OpenPreviewTarget(target);

        Assert.Equal(destination, viewModel.StatusText);
        Assert.Contains("id=\"document-reranking\"", viewModel.PreviewHtml);
        Assert.Contains(HtmlTemplates.CreateInitialAnchorScript(fragment), viewModel.PreviewHtml);
        Assert.Empty(context.Shell.Targets);
        Assert.Empty(context.Dialogs.Messages);
    }

    [Theory]
    [InlineData("native-path")]
    [InlineData("file-uri")]
    [InlineData("drive-reference")]
    public void InitializePreview_OpensMarkdownAtLinkedHeading(string targetType)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var destination = temporaryDirectory.GetPath("Package #1.md");
        const string fragment = "#document-reranking";
        File.WriteAllText(destination, "## Document reranking");
        var uri = new Uri(destination).AbsoluteUri;
        var target = targetType switch
        {
            "native-path" => destination,
            "file-uri" => uri,
            _ => "/" + uri["file:///".Length..]
        };
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel(target + fragment);

        viewModel.InitializePreview();

        Assert.Empty(context.Dialogs.Messages);
        Assert.Equal(destination, viewModel.StatusText);
        Assert.Contains("id=\"document-reranking\"", viewModel.PreviewHtml);
        Assert.Contains(HtmlTemplates.CreateInitialAnchorScript(fragment), viewModel.PreviewHtml);
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
