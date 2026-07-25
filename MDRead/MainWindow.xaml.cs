using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MDRead;

public partial class MainWindow : Window
{
    private string? _filePath;
    private bool _dark = true, _dirty, _loading, _renderingPreview;
    private readonly UserSettings _settings;

    public MainWindow(string[] args)
    {
        InitializeComponent();
        _settings = UserSettings.Load();
        RefreshRecentFiles();
        ApplyTheme(!args.Any(a => a.Equals("--light", StringComparison.OrdinalIgnoreCase)));
        var path = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
        if (path is not null) OpenFile(path); else Render();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Markdown files|*.md;*.markdown|All files|*.*", InitialDirectory = ValidDirectory(_settings.LastDirectory) };
        if (dialog.ShowDialog() == true) OpenFile(dialog.FileName);
    }
    private void OpenFile(string path)
    {
        try
        {
            _loading = true; MarkdownEditor.Text = File.ReadAllText(path); _loading = false;
            _filePath = Path.GetFullPath(path); _dirty = false; RememberFile(_filePath); Title = $"MDRead — {Path.GetFileName(path)}"; StatusText.Text = _filePath; Render();
        }
        catch (Exception ex) { _loading = false; ShowDialog(ex.Message, "MDRead", DialogButtons.Ok); }
    }
    private void MarkdownEditor_TextChanged(object sender, TextChangedEventArgs e) { if (!_loading) { _dirty = true; Render(); } }
    private void Render()
    {
        _renderingPreview = true;
        PreviewBrowser.NavigateToString(BuildHtml(MarkdownEditor.Text));
    }
    private string BuildHtml(string markdown)
    {
        var colors = _dark ? "body{background:#1e1e1e;color:#e8e8e8}a{color:#72b7ff}pre,code{background:#2b2b2b}blockquote{border-color:#777;color:#ccc}html,body{scrollbar-face-color:#525252;scrollbar-track-color:#1e1e1e;scrollbar-arrow-color:#d8d8d8;scrollbar-shadow-color:#1e1e1e;scrollbar-highlight-color:#525252;scrollbar-3dlight-color:#1e1e1e;scrollbar-darkshadow-color:#1e1e1e;scrollbar-base-color:#303030;scrollbar-color:#525252 #1e1e1e}" : "body{background:#fff;color:#15171a}a{color:#0645ad}pre,code{background:#e9edf2}blockquote{border-color:#8793a1;color:#374151}html,body{scrollbar-face-color:#a7b1bd;scrollbar-track-color:#f2f4f7;scrollbar-arrow-color:#253143;scrollbar-shadow-color:#f2f4f7;scrollbar-highlight-color:#c5cdd6;scrollbar-3dlight-color:#f2f4f7;scrollbar-darkshadow-color:#d7dce3;scrollbar-base-color:#d7dce3;scrollbar-color:#a7b1bd #f2f4f7}";
        return $$"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{{WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(_filePath) ?? "MDRead document")}}</title><style>body{font-family:Segoe UI,Arial,sans-serif;line-height:1.6;max-width:960px;margin:0 auto;padding:32px}h1,h2,h3,h4,h5,h6{line-height:1.25}pre{padding:14px;overflow:auto;border-radius:6px}code{font-family:Consolas,monospace;padding:2px 4px;border-radius:3px}pre code{padding:0}blockquote{margin-left:0;padding-left:16px;border-left:4px solid}img{max-width:100%}{{colors}}</style></head><body>{{MarkdownConverter.ToHtml(markdown)}}</body></html>""";
    }
    private void EditMode_Click(object sender, RoutedEventArgs e) => SetEditMode(EditModeMenuItem.IsChecked);
    private void SetEditMode(bool enabled)
    {
        EditModeMenuItem.IsChecked = enabled; EditorPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed; EditorSplitter.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        EditorRow.Height = enabled ? new GridLength(_settings.EditorHeight > 80 ? _settings.EditorHeight : 300) : new GridLength(0); SplitterRow.Height = enabled ? new GridLength(7) : new GridLength(0); if (enabled) MarkdownEditor.Focus();
    }
    private void LightTheme_Click(object s, RoutedEventArgs e) => ApplyTheme(false);
    private void DarkTheme_Click(object s, RoutedEventArgs e) => ApplyTheme(true);
    private void ApplyTheme(bool dark)
    {
        _dark = dark; DarkThemeMenuItem.IsChecked = dark; LightThemeMenuItem.IsChecked = !dark; MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#1E1E1E" : "#F2F4F7")); MarkdownEditor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#252526" : "#FFFFFF")); MarkdownEditor.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8E8E8" : "#16181D")); ApplicationStatusBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#252526" : "#E1E6ED")); ApplicationStatusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8E8E8" : "#202833")); ApplicationStatusBar.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#3E3E42" : "#B6C0CD")); Resources["MenuBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#252526" : "#E1E6ED")); Resources["MenuForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#E8E8E8" : "#202833")); Resources["MenuHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#3E3E42" : "#C6D0DC")); Resources["MenuBorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#454545" : "#AAB5C2")); Resources["ScrollTrackBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#1A1A1A" : "#D7DCE3")); Resources["ScrollThumbBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#686868" : "#687385")); Resources["ScrollThumbHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#909090" : "#39465A")); if (IsLoaded) Render();
    }
    private void Save_Click(object sender, RoutedEventArgs e) { if (_filePath is null) SaveAs(); else SaveTo(_filePath); }
    private void SaveAs_Click(object sender, RoutedEventArgs e) => SaveAs();
    private void SaveAs()
    {
        var dialog = new SaveFileDialog { Filter = "Markdown files|*.md|All files|*.*", FileName = _filePath is null ? "document.md" : Path.GetFileName(_filePath), InitialDirectory = ValidDirectory(_settings.LastDirectory) };
        if (dialog.ShowDialog() == true) SaveTo(dialog.FileName);
    }
    private void SaveTo(string path) { File.WriteAllText(path, MarkdownEditor.Text, new UTF8Encoding(false)); _filePath = path; _dirty = false; RememberFile(path); Title = $"MDRead — {Path.GetFileName(path)}"; StatusText.Text = $"Saved: {path}"; }
    private void ExportHtml_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "HTML files|*.html", FileName = _filePath is null ? "document.html" : Path.ChangeExtension(Path.GetFileName(_filePath), ".html"), InitialDirectory = ValidDirectory(_settings.LastDirectory) };
        if (dialog.ShowDialog() == true) { File.WriteAllText(dialog.FileName, BuildHtml(MarkdownEditor.Text), new UTF8Encoding(false)); RememberDirectory(dialog.FileName); StatusText.Text = $"Exported: {dialog.FileName}"; }
    }
    private void PreviewBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        // This navigation was launched by MDRead itself to refresh the preview.
        if (_renderingPreview)
        {
            _renderingPreview = false;
            return;
        }
        e.Cancel = true;
        var target = ResolveExternalTarget(e.Uri);
        if (ShowDialog($"Open this link in your default browser?\n\n{target}", "MDRead", DialogButtons.YesNo) == MessageBoxResult.Yes)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true });
    }
    private string ResolveExternalTarget(Uri? uri)
    {
        var target = uri?.ToString() ?? "";
        if (target.StartsWith("about:", StringComparison.OrdinalIgnoreCase) && _filePath is not null)
        {
            var relative = target[6..];
            var localPath = Path.Combine(Path.GetDirectoryName(_filePath)!, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath)) return localPath;
        }
        return target;
    }
    private void Exit_Click(object s, RoutedEventArgs e) => Close();
    private void About_Click(object s, RoutedEventArgs e) => ShowDialog("MDRead\nMarkdown reader, editor and standalone HTML exporter.\n\nFreeware — Olivier Dahan © 2026", "About MDRead", DialogButtons.Ok);
    private void Window_KeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape && !EditModeMenuItem.IsChecked) Close(); else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) Open_Click(s, e); else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) Save_Click(s, e); }
    private void Window_Closing(object? sender, CancelEventArgs e) { if (!_dirty) return; var choice = ShowDialog("Save your Markdown changes before closing?", "MDRead", DialogButtons.YesNoCancel); if (choice == MessageBoxResult.Cancel) e.Cancel = true; else if (choice == MessageBoxResult.Yes) Save_Click(sender!, new RoutedEventArgs()); }
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
    public static string ToHtml(string markdown)
    {
        var outp = new StringBuilder(); var lines = markdown.Replace("\r\n", "\n").Split('\n'); var paragraph = new StringBuilder(); var list = false; var code = false;
        void Flush() { if (paragraph.Length > 0) { outp.Append("<p>").Append(Inline(paragraph.ToString().Trim())).AppendLine("</p>"); paragraph.Clear(); } }
        void EndList() { if (list) { outp.AppendLine("</ul>"); list = false; } }
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd(); if (line.StartsWith("```")) { Flush(); EndList(); outp.AppendLine(code ? "</code></pre>" : "<pre><code>"); code = !code; continue; }
            if (code) { outp.AppendLine(WebUtility.HtmlEncode(raw)); continue; }
            var h = Regex.Match(line, "^(#{1,6})\\s+(.+)$"); if (h.Success) { Flush(); EndList(); var n = h.Groups[1].Length; outp.AppendLine($"<h{n}>{Inline(h.Groups[2].Value)}</h{n}>"); continue; }
            if (Regex.IsMatch(line, "^[-*+]\\s+")) { Flush(); if (!list) { outp.AppendLine("<ul>"); list = true; } outp.AppendLine($"<li>{Inline(Regex.Replace(line, "^[-*+]\\s+", ""))}</li>"); continue; }
            if (line.StartsWith("> ")) { Flush(); EndList(); outp.AppendLine($"<blockquote><p>{Inline(line[2..])}</p></blockquote>"); continue; }
            if (string.IsNullOrWhiteSpace(line)) { Flush(); EndList(); continue; }
            if (line is "---" or "***") { Flush(); EndList(); outp.AppendLine("<hr>"); continue; }
            if (paragraph.Length > 0) paragraph.Append(' '); paragraph.Append(line);
        }
        Flush(); EndList(); if (code) outp.AppendLine("</code></pre>"); return outp.ToString();
    }
    private static string Inline(string text)
    {
        var x = WebUtility.HtmlEncode(text); x = Regex.Replace(x, @"!\[([^]]*)\]\(([^ )]+)\)", "<img src=\"$2\" alt=\"$1\">"); x = Regex.Replace(x, @"\[([^]]+)\]\(([^ )]+)\)", "<a href=\"$2\">$1</a>"); x = Regex.Replace(x, @"`([^`]+)`", "<code>$1</code>"); x = Regex.Replace(x, @"\*\*([^*]+)\*\*|__([^_]+)__", "<strong>$1$2</strong>"); x = Regex.Replace(x, @"~~([^~]+)~~", "<del>$1</del>"); x = Regex.Replace(x, @"&lt;(/?u)&gt;", "<$1>"); return Regex.Replace(x, @"(?<!\*)\*([^*]+)\*(?!\*)|(?<!_)_([^_]+)_(?!_)", "<em>$1$2</em>");
    }
}
