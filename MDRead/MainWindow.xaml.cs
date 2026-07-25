using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MDRead;

public partial class MainWindow : Window
{
    private string? _filePath;
    private bool _dark = true, _dirty, _loading, _webReady, _internalNav;
    private readonly UserSettings _settings;
    private readonly DispatcherTimer _renderTimer;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        _settings = UserSettings.Load();
        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _renderTimer.Tick += (_, _) => { _renderTimer.Stop(); Render(); };
        RefreshRecentFiles();
        var startDark = _settings.IsDark;
        if (args.Any(a => a.Equals("--light", StringComparison.OrdinalIgnoreCase))) startDark = false;
        if (args.Any(a => a.Equals("--dark", StringComparison.OrdinalIgnoreCase))) startDark = true;
        ApplyTheme(startDark);
        _ = InitializePreviewAsync(args);
    }

    private async Task InitializePreviewAsync(string[] args)
    {
        try { await PreviewBrowser.EnsureCoreWebView2Async(); }
        catch (Exception ex) { ShowDialog($"The preview engine (WebView2 runtime) could not start.\n\n{ex.Message}", "MDRead", DialogButtons.Ok); return; }
        PreviewBrowser.CoreWebView2.NavigationStarting += Preview_NavigationStarting;
        PreviewBrowser.CoreWebView2.NewWindowRequested += Preview_NewWindowRequested;
        PreviewBrowser.CoreWebView2.WebMessageReceived += Preview_WebMessageReceived;
        _webReady = true;
        var path = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        if (path is not null) OpenFile(path); else Render();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
        _loading = true; MarkdownEditor.Text = ""; _loading = false;
        _filePath = null; _dirty = false; Title = "MDRead — Untitled"; StatusText.Text = "New document."; Render();
    }
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Markdown files|*.md;*.markdown|All files|*.*", InitialDirectory = ValidDirectory(_settings.LastDirectory) };
        if (dialog.ShowDialog() == true) OpenFile(dialog.FileName);
    }
    private void OpenFile(string path)
    {
        if (!ConfirmDiscardChanges()) return;
        try
        {
            _loading = true; MarkdownEditor.Text = File.ReadAllText(path); _loading = false;
            _filePath = Path.GetFullPath(path); _dirty = false; RememberFile(_filePath); Title = $"MDRead — {Path.GetFileName(path)}"; StatusText.Text = _filePath; Render();
        }
        catch (Exception ex) { _loading = false; ShowDialog(ex.Message, "MDRead", DialogButtons.Ok); }
    }
    private bool ConfirmDiscardChanges()
    {
        if (!_dirty) return true;
        var choice = ShowDialog("Save your Markdown changes before continuing?", "MDRead", DialogButtons.YesNoCancel);
        if (choice == MessageBoxResult.Cancel) return false;
        if (choice == MessageBoxResult.Yes) return SaveCurrent();
        return true;
    }
    private void MarkdownEditor_TextChanged(object sender, TextChangedEventArgs e) { if (_loading) return; _dirty = true; _renderTimer.Stop(); _renderTimer.Start(); }
    private void Render()
    {
        if (!_webReady) return;
        _internalNav = true;
        PreviewBrowser.NavigateToString(BuildHtml(MarkdownEditor.Text));
    }
    private const string ClickScript = "<script>document.addEventListener('click',function(e){var a=e.target.closest('a');if(a&&a.href){e.preventDefault();window.chrome.webview.postMessage(a.href);}},true);</script>";
    private string BuildHtml(string markdown)
    {
        var colors = _dark ? "body{background:#1e1e1e;color:#e8e8e8}a{color:#72b7ff}pre,code{background:#2b2b2b}blockquote{border-color:#777;color:#ccc}html,body{scrollbar-face-color:#525252;scrollbar-track-color:#1e1e1e;scrollbar-arrow-color:#d8d8d8;scrollbar-shadow-color:#1e1e1e;scrollbar-highlight-color:#525252;scrollbar-3dlight-color:#1e1e1e;scrollbar-darkshadow-color:#1e1e1e;scrollbar-base-color:#303030;scrollbar-color:#525252 #1e1e1e}" : "body{background:#fff;color:#15171a}a{color:#0645ad}pre,code{background:#e9edf2}blockquote{border-color:#8793a1;color:#374151}html,body{scrollbar-face-color:#a7b1bd;scrollbar-track-color:#f2f4f7;scrollbar-arrow-color:#253143;scrollbar-shadow-color:#f2f4f7;scrollbar-highlight-color:#c5cdd6;scrollbar-3dlight-color:#f2f4f7;scrollbar-darkshadow-color:#d7dce3;scrollbar-base-color:#d7dce3;scrollbar-color:#a7b1bd #f2f4f7}";
        var baseTag = BuildBaseTag();
        return $$"""<!doctype html><html lang="en"><head><meta charset="utf-8">{{baseTag}}<meta name="viewport" content="width=device-width,initial-scale=1"><title>{{WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(_filePath) ?? "MDRead document")}}</title><style>body{font-family:Segoe UI,Arial,sans-serif;line-height:1.6;max-width:960px;margin:0 auto;padding:32px}h1,h2,h3,h4,h5,h6{line-height:1.25}pre{padding:14px;overflow:auto;border-radius:6px}code{font-family:Consolas,monospace;padding:2px 4px;border-radius:3px}pre code{padding:0}blockquote{margin-left:0;padding-left:16px;border-left:4px solid}img{max-width:100%}{{colors}}</style></head><body>{{MarkdownConverter.ToHtml(markdown)}}{{ClickScript}}</body></html>""";
    }
    private string BuildBaseTag()
    {
        if (_filePath is null) return "";
        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrEmpty(directory)) return "";
        var href = new Uri(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar).AbsoluteUri;
        return $"<base href=\"{WebUtility.HtmlEncode(href)}\">";
    }
    private void EditMode_Click(object sender, RoutedEventArgs e) => SetEditMode(EditModeMenuItem.IsChecked);
    private void SetEditMode(bool enabled)
    {
        EditModeMenuItem.IsChecked = enabled; EditorPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed; EditorSplitter.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        EditorRow.Height = enabled ? new GridLength(_settings.EditorHeight > 80 ? _settings.EditorHeight : 300) : new GridLength(0); SplitterRow.Height = enabled ? new GridLength(7) : new GridLength(0); if (enabled) MarkdownEditor.Focus();
    }
    private void LightTheme_Click(object s, RoutedEventArgs e) { ApplyTheme(false); _settings.IsDark = false; _settings.Save(); }
    private void DarkTheme_Click(object s, RoutedEventArgs e) { ApplyTheme(true); _settings.IsDark = true; _settings.Save(); }
    private void ApplyTheme(bool dark)
    {
        _dark = dark; DarkThemeMenuItem.IsChecked = dark; LightThemeMenuItem.IsChecked = !dark; MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#1E1E1E" : "#F2F4F7")); MarkdownEditor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#252526" : "#FFFFFF")); MarkdownEditor.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8E8E8" : "#16181D")); ApplicationStatusBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#252526" : "#E1E6ED")); ApplicationStatusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8E8E8" : "#202833")); ApplicationStatusBar.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#3E3E42" : "#B6C0CD")); Resources["MenuBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#252526" : "#E1E6ED")); Resources["MenuForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8E8E8" : "#202833")); Resources["MenuHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#3E3E42" : "#C6D0DC")); Resources["MenuBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#454545" : "#AAB5C2")); Resources["ScrollTrackBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#1A1A1A" : "#D7DCE3")); Resources["ScrollThumbBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#686868" : "#687385")); Resources["ScrollThumbHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#909090" : "#39465A")); if (IsLoaded) Render();
    }
    private void Save_Click(object sender, RoutedEventArgs e) => SaveCurrent();
    private void SaveAs_Click(object sender, RoutedEventArgs e) => SaveAs();
    private bool SaveCurrent() => _filePath is null ? SaveAs() : SaveTo(_filePath);
    private bool SaveAs()
    {
        var dialog = new SaveFileDialog { Filter = "Markdown files|*.md|All files|*.*", FileName = _filePath is null ? "document.md" : Path.GetFileName(_filePath), InitialDirectory = ValidDirectory(_settings.LastDirectory) };
        return dialog.ShowDialog() == true && SaveTo(dialog.FileName);
    }
    private bool SaveTo(string path)
    {
        try { File.WriteAllText(path, MarkdownEditor.Text, new UTF8Encoding(false)); }
        catch (Exception ex) { ShowDialog($"Could not save the file.\n\n{ex.Message}", "MDRead", DialogButtons.Ok); return false; }
        _filePath = path; _dirty = false; RememberFile(path); Title = $"MDRead — {Path.GetFileName(path)}"; StatusText.Text = $"Saved: {path}"; return true;
    }
    private void ExportHtml_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "HTML files|*.html", FileName = _filePath is null ? "document.html" : Path.ChangeExtension(Path.GetFileName(_filePath), ".html"), InitialDirectory = ValidDirectory(_settings.LastDirectory) };
        if (dialog.ShowDialog() != true) return;
        try { File.WriteAllText(dialog.FileName, BuildHtml(MarkdownEditor.Text), new UTF8Encoding(false)); RememberDirectory(dialog.FileName); StatusText.Text = $"Exported: {dialog.FileName}"; }
        catch (Exception ex) { ShowDialog($"Could not export the file.\n\n{ex.Message}", "MDRead", DialogButtons.Ok); }
    }
    private void Preview_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Our own NavigateToString refresh is the only navigation allowed; link clicks are handled via WebMessage.
        if (_internalNav) { _internalNav = false; return; }
        e.Cancel = true;
    }
    private void Preview_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        var uri = e.Uri;
        Dispatcher.BeginInvoke(() => OpenLink(uri));
    }
    private void Preview_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string uri;
        try { uri = e.TryGetWebMessageAsString(); }
        catch { return; }
        Dispatcher.BeginInvoke(() => OpenLink(uri));
    }
    private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".exe", ".bat", ".cmd", ".com", ".ps1", ".psm1", ".msi", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".scr", ".jar", ".reg", ".lnk", ".pif", ".hta", ".cpl" };
    private void OpenLink(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed)) return;

        if (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps || parsed.Scheme == Uri.UriSchemeMailto)
        {
            if (ShowDialog($"Open this link in your default browser?\n\n{parsed.AbsoluteUri}", "MDRead", DialogButtons.YesNo) != MessageBoxResult.Yes) return;
            Launch(parsed.AbsoluteUri);
            return;
        }

        if (parsed.IsFile)
        {
            var localPath = parsed.LocalPath;
            // In-page anchors resolve against the base directory and never point to a real file: ignore silently.
            if (!File.Exists(localPath)) return;
            if (DangerousExtensions.Contains(Path.GetExtension(localPath)))
            {
                ShowDialog($"This link was blocked for security reasons:\n\n{localPath}", "MDRead", DialogButtons.Ok);
                return;
            }
            if (ShowDialog($"Open this file with its default application?\n\n{localPath}", "MDRead", DialogButtons.YesNo) != MessageBoxResult.Yes) return;
            Launch(localPath);
            return;
        }

        ShowDialog($"This link was blocked for security reasons:\n\n{uri}", "MDRead", DialogButtons.Ok);
    }
    private void Launch(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception ex) { ShowDialog(ex.Message, "MDRead", DialogButtons.Ok); }
    }
    private void Exit_Click(object s, RoutedEventArgs e) => Close();
    private void About_Click(object s, RoutedEventArgs e) => ShowDialog("MDRead\nMarkdown reader, editor and standalone HTML exporter.\n\nFreeware — Olivier Dahan © 2026", "About MDRead", DialogButtons.Ok);
    private void Window_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape && !EditModeMenuItem.IsChecked) Close(); else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control) New_Click(s, e); else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) Open_Click(s, e); else if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) SaveAs_Click(s, e); else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) Save_Click(s, e); }
    private void Window_Closing(object? sender, CancelEventArgs e) { if (!_dirty) return; var choice = ShowDialog("Save your Markdown changes before closing?", "MDRead", DialogButtons.YesNoCancel); if (choice == MessageBoxResult.Cancel) e.Cancel = true; else if (choice == MessageBoxResult.Yes && !SaveCurrent()) e.Cancel = true; }
    private MessageBoxResult ShowDialog(string text, string title, DialogButtons buttons) => ThemedDialog.Show(this, text, title, buttons, _dark);
    private void EditorSplitter_DragCompleted(object sender, DragCompletedEventArgs e) { if (!EditModeMenuItem.IsChecked) return; _settings.EditorHeight = EditorPanel.ActualHeight; _settings.Save(); }
    private void Wrap(string before, string after) { var start = MarkdownEditor.SelectionStart; var value = MarkdownEditor.SelectedText; MarkdownEditor.SelectedText = before + (string.IsNullOrEmpty(value) ? "text" : value) + after; MarkdownEditor.SelectionStart = start + before.Length; MarkdownEditor.SelectionLength = string.IsNullOrEmpty(value) ? 4 : value.Length; MarkdownEditor.Focus(); }
    private void Prefix(string prefix) { var start = MarkdownEditor.SelectionStart; var end = start + MarkdownEditor.SelectionLength; var text = MarkdownEditor.Text; var first = text.LastIndexOf('\n', Math.Max(0, start - 1)) + 1; var last = text.IndexOf('\n', end); if (last < 0) last = text.Length; MarkdownEditor.Select(first, last - first); MarkdownEditor.SelectedText = string.Join("\n", text[first..last].Split('\n').Select(l => prefix + l)); MarkdownEditor.Focus(); }
    private void RememberFile(string path) { _settings.LastDirectory = Path.GetDirectoryName(path); _settings.RecentFiles.RemoveAll(p => p.Equals(path, StringComparison.OrdinalIgnoreCase)); _settings.RecentFiles.Insert(0, path); if (_settings.RecentFiles.Count > 10) _settings.RecentFiles.RemoveRange(10, _settings.RecentFiles.Count - 10); _settings.Save(); RefreshRecentFiles(); }
    private void RememberDirectory(string path) { _settings.LastDirectory = Path.GetDirectoryName(path); _settings.Save(); }
    private static string? ValidDirectory(string? path) => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? path : null;
    private void RefreshRecentFiles() { RecentFilesMenuItem.Items.Clear(); var files = _settings.RecentFiles.Where(File.Exists).ToList(); if (files.Count == 0) { RecentFilesMenuItem.Items.Add(new MenuItem { Header = "(No recent files)", IsEnabled = false }); return; } foreach (var path in files) { var item = new MenuItem { Header = path, ToolTip = path }; item.Click += (_, _) => OpenFile(path); RecentFilesMenuItem.Items.Add(item); } }
    private void Heading1_Click(object s, RoutedEventArgs e) => Prefix("# "); private void Heading2_Click(object s, RoutedEventArgs e) => Prefix("## "); private void Heading3_Click(object s, RoutedEventArgs e) => Prefix("### "); private void Heading4_Click(object s, RoutedEventArgs e) => Prefix("#### "); private void Bold_Click(object s, RoutedEventArgs e) => Wrap("**", "**"); private void Italic_Click(object s, RoutedEventArgs e) => Wrap("*", "*"); private void Underline_Click(object s, RoutedEventArgs e) => Wrap("<u>", "</u>"); private void Strike_Click(object s, RoutedEventArgs e) => Wrap("~~", "~~"); private void Link_Click(object s, RoutedEventArgs e) => Wrap("[", "](https://)"); private void Image_Click(object s, RoutedEventArgs e) => Wrap("![description](", ")"); private void List_Click(object s, RoutedEventArgs e) => Prefix("- "); private void NumberedList_Click(object s, RoutedEventArgs e) => Prefix("1. "); private void Code_Click(object s, RoutedEventArgs e) => Wrap("`", "`"); private void CodeBlock_Click(object s, RoutedEventArgs e) => Wrap("```\n", "\n```"); private void Quote_Click(object s, RoutedEventArgs e) => Prefix("> "); private void Rule_Click(object s, RoutedEventArgs e) => Wrap("\n---\n", "");
}

internal static class MarkdownConverter
{
    private static readonly Regex HeadingRx = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex UnorderedRx = new(@"^[-*+]\s+", RegexOptions.Compiled);
    private static readonly Regex OrderedRx = new(@"^\d+\.\s+", RegexOptions.Compiled);
    private static readonly Regex ImgRx = new(@"!\[([^]]*)\]\(([^ )]+)\)", RegexOptions.Compiled);
    private static readonly Regex LinkRx = new(@"\[([^]]+)\]\(([^ )]+)\)", RegexOptions.Compiled);
    private static readonly Regex CodeRx = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex BoldRx = new(@"\*\*([^*]+)\*\*|__([^_]+)__", RegexOptions.Compiled);
    private static readonly Regex StrikeRx = new(@"~~([^~]+)~~", RegexOptions.Compiled);
    private static readonly Regex UnderlineRx = new(@"&lt;(/?u)&gt;", RegexOptions.Compiled);
    private static readonly Regex EmRx = new(@"(?<!\*)\*([^*]+)\*(?!\*)|(?<!_)_([^_]+)_(?!_)", RegexOptions.Compiled);

    public static string ToHtml(string markdown)
    {
        var outp = new StringBuilder(); var lines = markdown.Replace("\r\n", "\n").Split('\n'); var paragraph = new StringBuilder(); string? listTag = null; var code = false;
        void Flush() { if (paragraph.Length > 0) { outp.Append("<p>").Append(Inline(paragraph.ToString().Trim())).AppendLine("</p>"); paragraph.Clear(); } }
        void EndList() { if (listTag is not null) { outp.AppendLine($"</{listTag}>"); listTag = null; } }
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd(); if (line.StartsWith("```")) { Flush(); EndList(); outp.AppendLine(code ? "</code></pre>" : "<pre><code>"); code = !code; continue; }
            if (code) { outp.AppendLine(WebUtility.HtmlEncode(raw)); continue; }
            var h = HeadingRx.Match(line); if (h.Success) { Flush(); EndList(); var n = h.Groups[1].Length; outp.AppendLine($"<h{n}>{Inline(h.Groups[2].Value)}</h{n}>"); continue; }
            if (UnorderedRx.IsMatch(line)) { Flush(); if (listTag != "ul") { EndList(); outp.AppendLine("<ul>"); listTag = "ul"; } outp.AppendLine($"<li>{Inline(UnorderedRx.Replace(line, ""))}</li>"); continue; }
            if (OrderedRx.IsMatch(line)) { Flush(); if (listTag != "ol") { EndList(); outp.AppendLine("<ol>"); listTag = "ol"; } outp.AppendLine($"<li>{Inline(OrderedRx.Replace(line, ""))}</li>"); continue; }
            if (line.StartsWith("> ")) { Flush(); EndList(); outp.AppendLine($"<blockquote><p>{Inline(line[2..])}</p></blockquote>"); continue; }
            if (string.IsNullOrWhiteSpace(line)) { Flush(); EndList(); continue; }
            if (line is "---" or "***") { Flush(); EndList(); outp.AppendLine("<hr>"); continue; }
            if (paragraph.Length > 0) paragraph.Append(' '); paragraph.Append(line);
        }
        Flush(); EndList(); if (code) outp.AppendLine("</code></pre>"); return outp.ToString();
    }
    private static string Inline(string text)
    {
        var x = WebUtility.HtmlEncode(text); x = ImgRx.Replace(x, "<img src=\"$2\" alt=\"$1\">"); x = LinkRx.Replace(x, "<a href=\"$2\">$1</a>"); x = CodeRx.Replace(x, "<code>$1</code>"); x = BoldRx.Replace(x, "<strong>$1$2</strong>"); x = StrikeRx.Replace(x, "<del>$1</del>"); x = UnderlineRx.Replace(x, "<$1>"); return EmRx.Replace(x, "<em>$1$2</em>");
    }
}
