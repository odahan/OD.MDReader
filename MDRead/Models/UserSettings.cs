namespace MDRead.Models;

/// <summary>
/// Represents the user preferences persisted between application sessions.
/// </summary>
internal sealed class UserSettings
{
    /// <summary>Gets or sets the last directory used by a file picker.</summary>
    public string? LastDirectory { get; set; }

    /// <summary>Gets or sets the most recently opened document paths.</summary>
    public List<string> RecentFiles { get; set; } = [];

    /// <summary>Gets or sets the last editor-pane height.</summary>
    public double EditorHeight { get; set; }

    /// <summary>Gets or sets whether the dark theme is enabled.</summary>
    public bool IsDark { get; set; } = true;

}
