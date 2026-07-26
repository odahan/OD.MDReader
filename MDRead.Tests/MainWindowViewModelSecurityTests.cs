using MDRead.Constants;

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
