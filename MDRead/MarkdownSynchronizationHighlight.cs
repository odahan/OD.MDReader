using MDRead.Markdown;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace MDRead;

/// <summary>
/// Temporarily marks a source range without making it a replaceable editor selection.
/// </summary>
internal sealed class MarkdownSynchronizationHighlight : Adorner
{
    private static readonly Brush Fill = CreateHighlightBrush(74);
    private static readonly Pen Underline = CreateUnderline();

    private readonly TextBox _editor;
    private readonly AdornerLayer _layer;
    private readonly MarkdownSourceRange _range;
    private readonly DispatcherTimer _timer;

    private MarkdownSynchronizationHighlight(TextBox editor, AdornerLayer layer, MarkdownSourceRange range)
        : base(editor)
    {
        _editor = editor;
        _layer = layer;
        _range = range;
        IsHitTestVisible = false;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(PreviewSynchronization.HighlightDurationMilliseconds)
        };
        _timer.Tick += Expire;
    }

    /// <summary>Shows the highlight in the editor's adorner layer.</summary>
    public static MarkdownSynchronizationHighlight? Show(TextBox editor, MarkdownSourceRange range)
    {
        var layer = AdornerLayer.GetAdornerLayer(editor);
        if (layer is null)
        {
            return null;
        }

        var highlight = new MarkdownSynchronizationHighlight(editor, layer, range);
        layer.Add(highlight);
        editor.TextChanged += highlight.ClearOnEdit;
        editor.Unloaded += highlight.Expire;
        editor.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(highlight.OnScrollChanged));
        highlight._timer.Start();
        return highlight;
    }

    /// <summary>Removes the highlight and releases its event handlers.</summary>
    public void Remove()
    {
        if (!_timer.IsEnabled)
        {
            return;
        }

        _timer.Stop();
        _timer.Tick -= Expire;
        _editor.TextChanged -= ClearOnEdit;
        _editor.Unloaded -= Expire;
        _editor.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
        _layer.Remove(this);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        var firstVisible = _editor.GetFirstVisibleLineIndex();
        if (firstVisible < 0)
        {
            return;
        }

        var firstLine = Math.Max(firstVisible, _editor.GetLineIndexFromCharacterIndex(_range.Start));
        var lastLine = Math.Min(_editor.GetLastVisibleLineIndex(), _editor.GetLineIndexFromCharacterIndex(_range.End - 1));
        var viewport = GetViewport();
        var text = _editor.Text;
        drawingContext.PushClip(new RectangleGeometry(viewport));

        for (var line = firstLine; line <= lastLine; line++)
        {
            var lineStart = _editor.GetCharacterIndexFromLineIndex(line);
            var start = Math.Max(lineStart, _range.Start);
            var end = Math.Min(lineStart + _editor.GetLineLength(line), _range.End);
            while (end > start && text[end - 1] is '\r' or '\n')
            {
                end--;
            }

            if (end <= start)
            {
                continue;
            }

            var leading = _editor.GetRectFromCharacterIndex(start);
            var trailing = _editor.GetRectFromCharacterIndex(end - 1, trailingEdge: true);
            if (leading.IsEmpty || trailing.IsEmpty)
            {
                continue;
            }

            var left = Math.Min(leading.X, trailing.X);
            var right = Math.Max(leading.X, trailing.X);
            var bounds = new Rect(left, leading.Y, Math.Max(2, right - left), leading.Height);
            drawingContext.DrawRectangle(Fill, null, bounds);
            drawingContext.DrawLine(Underline, bounds.BottomLeft, bounds.BottomRight);
        }

        drawingContext.Pop();
    }

    private Rect GetViewport()
    {
        if (_editor.Template.FindName("PART_ContentHost", _editor) is ScrollViewer scrollViewer
            && scrollViewer.Template.FindName("PART_ScrollContentPresenter", scrollViewer)
                is ScrollContentPresenter presenter)
        {
            return presenter.TransformToAncestor(_editor).TransformBounds(new Rect(presenter.RenderSize));
        }

        return new Rect(_editor.RenderSize);
    }

    private void Expire(object? sender, EventArgs eventArgs) => Remove();

    private void ClearOnEdit(object sender, TextChangedEventArgs eventArgs) => Remove();

    private void OnScrollChanged(object sender, ScrollChangedEventArgs eventArgs) => InvalidateVisual();

    private static Brush CreateHighlightBrush(byte opacity)
    {
        var brush = new SolidColorBrush(Color.FromArgb(opacity, 255, 173, 41));
        brush.Freeze();
        return brush;
    }

    private static Pen CreateUnderline()
    {
        var pen = new Pen(CreateHighlightBrush(255), 2);
        pen.Freeze();
        return pen;
    }
}
