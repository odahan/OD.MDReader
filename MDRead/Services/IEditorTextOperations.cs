namespace MDRead.Services;

/// <summary>
/// Defines selection-aware editor operations implemented by the WPF view.
/// </summary>
internal interface IEditorTextOperations
{
    /// <summary>
    /// Wraps the current selection with the supplied fragments.
    /// </summary>
    /// <param name="prefix">The fragment inserted before the selection.</param>
    /// <param name="suffix">The fragment inserted after the selection.</param>
    void WrapSelection(string prefix, string suffix);

    /// <summary>
    /// Prefixes every line touched by the current selection.
    /// </summary>
    /// <param name="prefix">The fragment inserted at the start of each line.</param>
    void PrefixSelectedLines(string prefix);

    /// <summary>
    /// Moves keyboard focus to the editor.
    /// </summary>
    void FocusEditor();
}
