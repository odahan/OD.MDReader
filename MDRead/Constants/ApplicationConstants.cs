namespace MDRead.Constants;

/// <summary>
/// Provides application-wide behavioral constants.
/// </summary>
internal static class ApplicationConstants
{
    /// <summary>Gets the delay before regenerating the preview after an edit.</summary>
    public const int PreviewRenderDelayMilliseconds = 300;

    /// <summary>Gets the maximum number of recent files retained in settings.</summary>
    public const int MaximumRecentFiles = 10;


    /// <summary>Gets the light-theme command-line argument.</summary>
    public const string LightThemeArgument = "--light";

    /// <summary>Gets the dark-theme command-line argument.</summary>
    public const string DarkThemeArgument = "--dark";

    /// <summary>Gets the prefix used by command-line options.</summary>
    public const string CommandLineOptionPrefix = "--";

    /// <summary>Gets the version of the running application.</summary>
    public static string Version => typeof(ApplicationConstants)
        .Assembly
        .GetName()
        .Version?
        .ToString(fieldCount: 3) ?? "Unknown";
}

/// <summary>
/// Provides constants used to locate the application settings file.
/// </summary>
internal static class SettingsConstants
{
    /// <summary>Gets the publisher folder stored under local application data.</summary>
    public const string PublisherFolderName = "Olivier Dahan";

    /// <summary>Gets the application settings folder name.</summary>
    public const string ApplicationFolderName = "MDRead";

    /// <summary>Gets the settings file name.</summary>
    public const string FileName = "settings.json";
}

/// <summary>
/// Provides values used by the WebView2 preview engine.
/// </summary>
internal static class WebView2Constants
{
    /// <summary>Gets the WebView2 user data folder name.</summary>
    public const string UserDataFolderName = "WebView2";

    /// <summary>Gets the virtual HTTPS host used to serve Markdown resources from disk.</summary>
    public const string PreviewVirtualHostName = "mdread-preview.invalid";

    /// <summary>Gets the base address of the virtual host used by the preview.</summary>
    public const string PreviewVirtualHostAddress =
        "https://" + PreviewVirtualHostName + "/";

    /// <summary>Gets the official Microsoft download page for WebView2.</summary>
    public const string DownloadUrl = "https://developer.microsoft.com/microsoft-edge/webview2/consumer/";
}

/// <summary>
/// Provides URI and file-system values used by preview-link validation.
/// </summary>
internal static class SecurityConstants
{
    /// <summary>Gets extensions that cannot be launched from the preview.</summary>
    public static readonly IReadOnlySet<string> DangerousFileExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe",
            ".bat",
            ".cmd",
            ".com",
            ".ps1",
            ".psm1",
            ".msi",
            ".vbs",
            ".vbe",
            ".js",
            ".jse",
            ".wsf",
            ".wsh",
            ".scr",
            ".jar",
            ".reg",
            ".lnk",
            ".pif",
            ".hta",
            ".cpl"
        };
}
