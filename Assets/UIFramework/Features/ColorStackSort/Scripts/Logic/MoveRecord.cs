using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// A move that has already been applied, carrying everything needed to reverse it.
    /// <para>
    /// Unlike <see cref="Move"/>, this DOES store the run length and colour — deliberately, and for
    /// the opposite reason. A <see cref="Move"/> omits them so it can never carry a stale length
    /// into <see cref="BoardState.Apply"/>; a record needs them because after the move the board no
    /// longer knows how much of the destination's top run arrived in that step. Three reds landing
    /// on two reds is indistinguishable from five arriving at once by inspecting the board alone.
    /// </para>
    /// </summary>
    public readonly struct MoveRecord : IEquatable<MoveRecord>
    {
        /// <summary>Container the run was taken from — where <see cref="BoardState.UndoMove"/> puts it back.</summary>
        public readonly int From;

        /// <summary>Container the run landed on — where <see cref="BoardState.UndoMove"/> lifts it from.</summary>
        public readonly int To;

        public readonly int Count;
        public readonly ColorId Color;

        public MoveRecord(int from, int to, int count, ColorId color)
        {
            if (from < 0) throw new ArgumentOutOfRangeException(nameof(from), from, "Index must be non-negative.");
            if (to < 0) throw new ArgumentOutOfRangeException(nameof(to), to, "Index must be non-negative.");
            if (from == to)
                throw new ArgumentException($"A move cannot start and end at container {from}.", nameof(to));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "A recorded move carries at least one item.");

            From = from;
            To = to;
            Count = count;
            Color = color;
        }

        public bool Equals(MoveRecord other) =>
            From == other.From && To == other.To && Count == other.Count && Color == other.Color;

        public override bool Equals(object obj) => obj is MoveRecord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = From * 397;
                hash = (hash ^ To) * 397;
                hash = (hash ^ Count) * 397;
                return hash ^ Color.Value;
            }
        }

        public override string ToString() => $"{From}->{To} x{Count} {Color}";

        public static bool operator ==(MoveRecord left, MoveRecord right) => left.Equals(right);
        public static bool operator !=(MoveRecord left, MoveRecord right) => !left.Equals(right);
    }
}
