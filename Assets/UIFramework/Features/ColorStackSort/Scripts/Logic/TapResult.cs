namespace ColorStackSort.Logic
{
    /// <summary>
    /// The outcome of one tap, plus everything the view needs to animate it.
    /// <para>
    /// <see cref="Count"/> and <see cref="Color"/> exist because the view cannot recover them after
    /// the fact: once <see cref="BoardInteraction.Tap"/> applies the move, the source no longer
    /// holds the run. They are captured before the board mutates.
    /// </para>
    /// </summary>
    public readonly struct TapResult
    {
        /// <summary>Index meaning "no container", used wherever a field does not apply.</summary>
        public const int NoContainer = -1;

        public readonly TapOutcome Outcome;

        /// <summary>The selected / source tube, or the tube that was just selected. -1 when unused.</summary>
        public readonly int From;

        /// <summary>The destination tube of a move or rejected attempt. -1 when unused.</summary>
        public readonly int To;

        /// <summary>How many balls moved. 0 unless <see cref="Outcome"/> is Moved.</summary>
        public readonly int Count;

        /// <summary>The colour that moved. Meaningless unless <see cref="Outcome"/> is Moved.</summary>
        public readonly ColorId Color;

        private TapResult(TapOutcome outcome, int from, int to, int count, ColorId color)
        {
            Outcome = outcome;
            From = from;
            To = to;
            Count = count;
            Color = color;
        }

        public static TapResult None() =>
            new TapResult(TapOutcome.None, NoContainer, NoContainer, 0, default);

        public static TapResult Selected(int container) =>
            new TapResult(TapOutcome.Selected, container, NoContainer, 0, default);

        public static TapResult Deselected(int container) =>
            new TapResult(TapOutcome.Deselected, container, NoContainer, 0, default);

        public static TapResult Rejected(int from, int to) =>
            new TapResult(TapOutcome.Rejected, from, to, 0, default);

        public static TapResult Moved(int from, int to, int count, ColorId color) =>
            new TapResult(TapOutcome.Moved, from, to, count, color);

        public override string ToString() => Outcome == TapOutcome.Moved
            ? $"{Outcome} {From}->{To} x{Count} {Color}"
            : $"{Outcome} from={From} to={To}";
    }
}
