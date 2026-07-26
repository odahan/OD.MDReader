using MDRead.Models;
using MDRead.Services;

namespace MDRead.Tests;

/// <summary>
/// Verifies JSON settings persistence without using the real application-data path.
/// </summary>
public sealed class JsonSettingsServiceTests
{
    [Fact]
    public void Load_MissingFileReturnsDefaults()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var service = new JsonSettingsService(
            temporaryDirectory.GetPath("missing/settings.json"));

        var settings = service.Load();

        Assert.True(settings.IsDark);
        Assert.Empty(settings.RecentFiles);
    }

    [Fact]
    public void Load_MalformedJsonReturnsDefaults()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = temporaryDirectory.GetPath("settings.json");
        File.WriteAllText(settingsPath, "{ malformed");
        var service = new JsonSettingsService(settingsPath);

        var settings = service.Load();

        Assert.True(settings.IsDark);
        Assert.Empty(settings.RecentFiles);
    }

    [Fact]
    public void Save_CreatesDirectoryAndRoundTripsAllValues()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = temporaryDirectory.GetPath("nested/settings.json");
        var service = new JsonSettingsService(settingsPath);
        var expected = new UserSettings
        {
            LastDirectory = temporaryDirectory.Path,
            RecentFiles = ["one.md", "two.md"],
            EditorHeight = 280,
            IsDark = false
        };

        service.Save(expected);
        var actual = service.Load();

        Assert.Equal(expected.LastDirectory, actual.LastDirectory);
        Assert.Equal(expected.RecentFiles, actual.RecentFiles);
        Assert.Equal(expected.EditorHeight, actual.EditorHeight);
        Assert.Equal(expected.IsDark, actual.IsDark);
    }

    [Fact]
    public void Load_RemovesLegacyEditModePreference()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = temporaryDirectory.GetPath("settings.json");
        File.WriteAllText(
            settingsPath,
            "{\"IsDark\":false,\"EditMode\":true}");
        var service = new JsonSettingsService(settingsPath);

        var settings = service.Load();

        var json = File.ReadAllText(settingsPath);
        Assert.False(settings.IsDark);
        Assert.Contains("\"IsDark\":false", json);
        Assert.DoesNotContain("\"EditMode\"", json);
        Assert.DoesNotContain("\"IsDarkTheme\"", json);
        Assert.DoesNotContain("\"IsEditMode\"", json);
    }
}
