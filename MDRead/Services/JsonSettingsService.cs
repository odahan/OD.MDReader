using MDRead.Constants;
using MDRead.Models;
using System.IO;
using System.Text.Json;

namespace MDRead.Services;

/// <summary>
/// Persists user settings as JSON under the local application-data directory.
/// </summary>
internal sealed class JsonSettingsService : ISettingsService
{
    private readonly string _settingsPath;

    /// <summary>
    /// Initializes a new instance that uses the standard local application-data path.
    /// </summary>
    public JsonSettingsService()
        : this(BuildDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Initializes a new instance that uses an explicit settings path.
    /// </summary>
    /// <param name="settingsPath">The JSON settings file path.</param>
    internal JsonSettingsService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    private static string BuildDefaultSettingsPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            SettingsConstants.PublisherFolderName,
            SettingsConstants.ApplicationFolderName,
            SettingsConstants.FileName);

    /// <inheritdoc />
    public UserSettings Load()
    {
        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json)
                ?? new UserSettings();

            if (ContainsLegacyEditModePreference(json))
            {
                Save(settings);
            }

            return settings;
        }
        catch (IOException)
        {
            return new UserSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new UserSettings();
        }
        catch (JsonException)
        {
            return new UserSettings();
        }
    }

    /// <inheritdoc />
    public void Save(UserSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings));
        }
        catch (IOException)
        {
            // Settings persistence must never interrupt the editing workflow.
        }
        catch (UnauthorizedAccessException)
        {
            // Settings persistence must never interrupt the editing workflow.
        }
    }

    /// <summary>
    /// Determines whether a settings file still contains the retired Edit mode preference.
    /// </summary>
    /// <param name="json">The settings document to inspect.</param>
    /// <returns><see langword="true"/> when the retired preference is present.</returns>
    private static bool ContainsLegacyEditModePreference(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("EditMode", out _);
    }
}
