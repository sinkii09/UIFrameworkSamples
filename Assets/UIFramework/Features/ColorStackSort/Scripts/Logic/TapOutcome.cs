namespace ColorStackSort.Logic
{
    /// <summary>What a tap did. The view drives all of its feedback off this.</summary>
    public enum TapOutcome
    {
        /// <summary>Nothing happened — an empty tube with no selection, or an index out of range.</summary>
        None,

        /// <summary>A tube became the selected source; its top run should lift.</summary>
        Selected,

        /// <summary>The selected tube was tapped again; its run should drop back.</summary>
        Deselected,

        /// <summary>A legal move ran and the board has already mutated.</summary>
        Moved,

        /// <summary>An illegal destination. The selection is deliberately KEPT — the destination shakes.</summary>
        Rejected
    }
}
