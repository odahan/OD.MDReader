namespace MDRead.Models;

/// <summary>
/// Represents one entry in the recent-files menu.
/// </summary>
/// <param name="DisplayName">The text displayed by the menu.</param>
/// <param name="FilePath">The path opened by the entry, if one exists.</param>
/// <param name="IsEnabled">Whether the entry can be selected.</param>
internal sealed record RecentFileEntry(
    string DisplayName,
    string? FilePath,
    bool IsEnabled);
