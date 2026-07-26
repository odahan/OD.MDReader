using MDRead.Models;

namespace MDRead.Services;

/// <summary>
/// Abstracts loading and saving user settings.
/// </summary>
internal interface ISettingsService
{
    /// <summary>
    /// Loads settings or returns defaults when no valid settings are available.
    /// </summary>
    /// <returns>The loaded settings.</returns>
    UserSettings Load();

    /// <summary>
    /// Saves settings on a best-effort basis.
    /// </summary>
    /// <param name="settings">The settings to persist.</param>
    void Save(UserSettings settings);
}
