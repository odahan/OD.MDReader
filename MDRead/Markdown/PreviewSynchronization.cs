namespace MDRead.Markdown;

/// <summary>
/// Connects WebView2 selections and highlights to the preview's Markdown source ranges.
/// </summary>
internal static class PreviewSynchronization
{
    /// <summary>Gets the time a synchronization highlight remains visible.</summary>
    public const int HighlightDurationMilliseconds = 2500;

    /// <summary>Gets the styles for synchronized text and elements without selectable text.</summary>
    public const string Styles = """
        ::highlight(mdread-sync) { background-color: #ffd15c; color: #19150a; }
        .mdread-sync-target { outline: 2px solid #e99500; outline-offset: 2px; }
        """;

    /// <summary>
    /// Creates the synchronization script for a specific rendered revision.
    /// </summary>
    public static string CreateScript(long revision) => $$"""
        <script>
        window.mdreadSync = (() => {
            const revision = {{revision}};
            const selector = '[data-md-start][data-md-end]';
            const highlightName = 'mdread-sync';
            const targetClass = 'mdread-sync-target';
            let timer;
            let outlined = [];

            function clearHighlight() {
                clearTimeout(timer);
                CSS.highlights?.delete(highlightName);
                for (const element of outlined) element.classList.remove(targetClass);
                outlined = [];
            }

            function highlightLine(expectedRevision, start, end) {
                if (revision !== expectedRevision || start >= end) return false;
                clearHighlight();
                const overlaps = element => +element.dataset.mdStart < end && +element.dataset.mdEnd > start;
                let targets = Array.from(document.querySelectorAll('[data-md-text], img' + selector))
                    .filter(overlaps);

                if (!targets.length) {
                    // Structural source lines, such as a code fence, can map to a whole element.
                    const blocks = Array.from(document.querySelectorAll(selector)).filter(overlaps);
                    blocks.sort((a, b) =>
                        (a.dataset.mdEnd - a.dataset.mdStart) - (b.dataset.mdEnd - b.dataset.mdStart));
                    if (!blocks.length) return false;
                    targets = [blocks[0].closest('pre') || blocks[0]];
                }

                const ranges = [];
                for (const element of targets) {
                    if (CSS.highlights && element.hasAttribute('data-md-text') && element.textContent.trim()) {
                        const range = document.createRange();
                        range.selectNodeContents(element);
                        ranges.push(range);
                    } else {
                        element.classList.add(targetClass);
                        outlined.push(element);
                    }
                }

                if (ranges.length) CSS.highlights.set(highlightName, new Highlight(...ranges));
                targets[0].scrollIntoView({ block: 'center', inline: 'nearest' });
                timer = setTimeout(clearHighlight, {{HighlightDurationMilliseconds}});
                return true;
            }

            function readSelection(expectedRevision) {
                if (revision !== expectedRevision) return null;
                const selection = window.getSelection();
                if (!selection?.rangeCount || selection.isCollapsed || !selection.toString().trim()) return null;

                const range = selection.getRangeAt(0);
                const root = range.commonAncestorContainer;
                const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
                let node = root.nodeType === Node.TEXT_NODE ? root : walker.nextNode();
                let start = Infinity;
                let end = -1;
                for (; node; node = walker.nextNode()) {
                    if (!range.intersectsNode(node)) continue;
                    const from = node === range.startContainer ? range.startOffset : 0;
                    const to = node === range.endContainer ? range.endOffset : node.length;
                    if (from >= to || !node.data.slice(from, to).trim()) continue;

                    const element = node.parentElement?.closest(selector);
                    if (!element) continue;
                    let sourceStart = +element.dataset.mdStart;
                    let sourceEnd = +element.dataset.mdEnd;
                    if (element.dataset.mdText === 'exact' && element.textContent.length === sourceEnd - sourceStart) {
                        const prefix = document.createRange();
                        prefix.selectNodeContents(element);
                        prefix.setEnd(node, from);
                        sourceStart += prefix.toString().length;
                        sourceEnd = sourceStart + to - from;
                    }

                    start = Math.min(start, sourceStart);
                    end = Math.max(end, sourceEnd);
                }

                return start < end ? { Start: start, End: end } : null;
            }

            return { highlightLine, readSelection };
        })();
        </script>
        """;
}
