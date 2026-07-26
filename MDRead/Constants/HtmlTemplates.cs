namespace MDRead.Constants;

/// <summary>
/// Provides HTML tags, replacement templates, scripts, and styles.
/// </summary>
internal static class HtmlTemplates
{
    /// <summary>Gets the empty HTML base tag.</summary>
    public const string BaseTagFormat = "<base href=\"{0}\">";

    /// <summary>
    /// Gets the script that sends preview link clicks to the WPF host.
    /// </summary>
    public const string ClickInterceptionScript =
        "<script>document.addEventListener('click',function(e){" +
        "var a=e.target.closest('a');if(a&&a.href){e.preventDefault();" +
        "window.chrome.webview.postMessage(a.href);}},true);</script>";

    /// <summary>Gets styles shared by light and dark preview themes.</summary>
    public const string BaseStyles =
        "body{font-family:Segoe UI,Arial,sans-serif;line-height:1.6;" +
        "max-width:960px;margin:0 auto;padding:32px}" +
        "h1,h2,h3,h4,h5,h6{line-height:1.25}" +
        "pre{padding:14px;overflow:auto;border-radius:6px}" +
        "code{font-family:Consolas,monospace;padding:2px 4px;border-radius:3px}" +
        "pre code{padding:0}" +
        "blockquote{margin-left:0;padding-left:16px;border-left:4px solid}" +
        "table{border-collapse:collapse;max-width:100%}" +
        "th,td{padding:8px 12px;border:1px solid}th{text-align:left}" +
        ".task-list-item{list-style:none}" +
        ".task-list-item input{margin:0 7px 0 -20px}" +
        ".footnotes{margin-top:32px;font-size:.9em}" +
        "img{max-width:100%}";

    /// <summary>Gets the dark preview-theme styles.</summary>
    public const string DarkStyles =
        "body{background:#1e1e1e;color:#e8e8e8}" +
        "a{color:#72b7ff}" +
        "pre,code{background:#2b2b2b}" +
        "blockquote{border-color:#777;color:#ccc}" +
        "th,td{border-color:#555}th{background:#2b2b2b}" +
        "html,body{scrollbar-face-color:#525252;scrollbar-track-color:#1e1e1e;" +
        "scrollbar-arrow-color:#d8d8d8;scrollbar-shadow-color:#1e1e1e;" +
        "scrollbar-highlight-color:#525252;scrollbar-3dlight-color:#1e1e1e;" +
        "scrollbar-darkshadow-color:#1e1e1e;scrollbar-base-color:#303030;" +
        "scrollbar-color:#525252 #1e1e1e}";

    /// <summary>Gets the light preview-theme styles.</summary>
    public const string LightStyles =
        "body{background:#fff;color:#15171a}" +
        "a{color:#0645ad}" +
        "pre,code{background:#e9edf2}" +
        "blockquote{border-color:#8793a1;color:#374151}" +
        "th,td{border-color:#cbd5e1}th{background:#f2f4f7}" +
        "html,body{scrollbar-face-color:#a7b1bd;scrollbar-track-color:#f2f4f7;" +
        "scrollbar-arrow-color:#253143;scrollbar-shadow-color:#f2f4f7;" +
        "scrollbar-highlight-color:#c5cdd6;scrollbar-3dlight-color:#f2f4f7;" +
        "scrollbar-darkshadow-color:#d7dce3;scrollbar-base-color:#d7dce3;" +
        "scrollbar-color:#a7b1bd #f2f4f7}";

    /// <summary>
    /// Builds a complete standalone HTML document.
    /// </summary>
    /// <param name="baseTag">The optional encoded base tag.</param>
    /// <param name="title">The encoded document title.</param>
    /// <param name="themeStyles">The current theme style sheet.</param>
    /// <param name="body">The converted Markdown body.</param>
    /// <param name="script">The optional host-integration script.</param>
    /// <returns>The standalone HTML document.</returns>
    public static string BuildDocument(
        string baseTag,
        string title,
        string themeStyles,
        string body,
        string script) =>
        $"""
         <!doctype html>
         <html lang="en">
         <head>
         <meta charset="utf-8">
         {baseTag}
         <meta name="viewport" content="width=device-width,initial-scale=1">
         <title>{title}</title>
         <style>{BaseStyles}{themeStyles}</style>
         </head>
         <body>{body}{script}</body>
         </html>
         """;
}
