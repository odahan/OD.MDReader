using MDRead.Constants;
using MDRead.Models;
using MDRead.Services;

namespace MDRead.Tests;

/// <summary>
/// Verifies main view-model initialization, state transitions, and window commands.
/// </summary>
public sealed class MainWindowViewModelStateTests
{
    [Fact]
    public void Constructor_UsesSettingsAndDefaultPresentationValues()
    {
        var context = new ViewModelTestContext
        {
            Settings =
            {
                Settings = new UserSettings
                {
                    IsDark = false,
                    EditMode = true,
                    EditorHeight = 245
                }
            }
        };

        var viewModel = context.CreateViewModel();

        Assert.False(viewModel.IsDarkTheme);
        Assert.True(viewModel.IsEditMode);
        Assert.Equal(245, viewModel.EditorHeight);
        Assert.Equal(AppText.UntitledWindowTitle, viewModel.WindowTitle);
        Assert.Equal(AppText.ReadyStatus, viewModel.StatusText);
        Assert.False(viewModel.ExitPreviewCommand.CanExecute(null));
    }

    [Theory]
    [InlineData("--light", false)]
    [InlineData("--LIGHT", false)]
    [InlineData("--dark", true)]
    [InlineData("--DARK", true)]
    public void Constructor_CommandLineThemeOverridesSettings(
        string argument,
        bool expectedDarkTheme)
    {
        var context = new ViewModelTestContext();
        context.Settings.Settings.IsDark = !expectedDarkTheme;

        var viewModel = context.CreateViewModel(argument);

        Assert.Equal(expectedDarkTheme, viewModel.IsDarkTheme);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(80)]
    [InlineData(-10)]
    public void EditorHeight_InvalidSettingUsesDefault(double configuredHeight)
    {
        var context = new ViewModelTestContext();
        context.Settings.Settings.EditorHeight = configuredHeight;

        var viewModel = context.CreateViewModel();

        Assert.Equal(UiLayoutConstants.DefaultEditorHeight, viewModel.EditorHeight);
    }

    [Fact]
    public void SaveEditorHeight_UpdatesAndPersistsSetting()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.SaveEditorHeight(321);

        Assert.Equal(321, viewModel.EditorHeight);
        Assert.Equal(321, context.Settings.Settings.EditorHeight);
        Assert.Equal(1, context.Settings.SaveCount);
    }

    [Fact]
    public void EditMode_PersistsFocusesAndUpdatesEscapeCommand()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        Assert.True(viewModel.ExitPreviewCommand.CanExecute(null));

        viewModel.IsEditMode = true;

        Assert.True(context.Settings.Settings.EditMode);
        Assert.Equal(1, context.Settings.SaveCount);
        Assert.Equal(1, context.Editor.FocusRequestCount);
        Assert.False(viewModel.ExitPreviewCommand.CanExecute(null));
    }

    [Fact]
    public void ThemeCommands_UpdateSettingsAndPreview()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        viewModel.InitializePreview();

        viewModel.UseLightThemeCommand.Execute(null);

        Assert.False(viewModel.IsDarkTheme);
        Assert.False(context.Settings.Settings.IsDark);
        Assert.Contains(HtmlTemplates.LightStyles, viewModel.PreviewHtml);

        viewModel.UseDarkThemeCommand.Execute(null);

        Assert.True(viewModel.IsDarkTheme);
        Assert.True(context.Settings.Settings.IsDark);
        Assert.Contains(HtmlTemplates.DarkStyles, viewModel.PreviewHtml);
        Assert.Equal(2, context.Settings.SaveCount);
    }

    [Fact]
    public void InitializePreview_RendersHtmlWithHostBridge()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "# Hello";

        viewModel.InitializePreview();

        Assert.Contains("<h1>Hello</h1>", viewModel.PreviewHtml);
        Assert.Contains(HtmlTemplates.ClickInterceptionScript, viewModel.PreviewHtml);
        Assert.Contains("<html lang=\"en\">", viewModel.PreviewHtml);
    }

    [Fact]
    public async Task MarkdownTextChange_DebouncesAndRefreshesReadyPreview()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        viewModel.InitializePreview();

        viewModel.MarkdownText = "First";
        viewModel.MarkdownText = "Second";
        await Task.Delay(ApplicationConstants.PreviewRenderDelayMilliseconds + 150);

        Assert.Contains("<p>Second</p>", viewModel.PreviewHtml);
        Assert.DoesNotContain("<p>First</p>", viewModel.PreviewHtml);
    }

    [Fact]
    public void ExitCommands_RequestWindowClosureWhenAllowed()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        var requestCount = 0;
        viewModel.CloseRequested += (_, _) => requestCount++;

        viewModel.ExitCommand.Execute(null);
        viewModel.ExitPreviewCommand.Execute(null);

        Assert.Equal(2, requestCount);
    }

    [Fact]
    public void ShowAbout_DisplaysCentralizedContent()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.ShowAboutCommand.Execute(null);

        var message = Assert.Single(context.Dialogs.Messages);
        Assert.Equal(AppText.AboutMessage, message.Message);
        Assert.Equal(AppText.AboutTitle, message.Title);
    }

    [Fact]
    public void ReportPreviewFailure_DisplaysExceptionMessage()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        viewModel.ReportPreviewFailure(new InvalidOperationException("Unavailable"));

        var message = Assert.Single(context.Dialogs.Messages);
        Assert.Contains("Unavailable", message.Message);
    }

    [Theory]
    [InlineData(nameof(DialogChoice.No), true)]
    [InlineData(nameof(DialogChoice.Cancel), false)]
    public void CanClose_RespectsUnsavedChangeChoice(
        string choiceName,
        bool expected)
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();
        viewModel.MarkdownText = "Changed";
        context.Dialogs.EnqueueSaveConfirmation(
            Enum.Parse<DialogChoice>(choiceName));

        var result = viewModel.CanClose();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanClose_CleanDocumentDoesNotPrompt()
    {
        var context = new ViewModelTestContext();
        var viewModel = context.CreateViewModel();

        var result = viewModel.CanClose();

        Assert.True(result);
        Assert.Empty(context.Dialogs.Messages);
    }
}
