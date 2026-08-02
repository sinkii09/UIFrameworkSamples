using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// A player move: take the whole maximal top run from <see cref="From"/> and place it on
    /// <see cref="To"/>.
    /// <para>
    /// The run length is deliberately NOT stored. It is always derived from the board at the
    /// moment the move is applied, so a Move can never carry a stale length that disagrees with
    /// the board it is applied to.
    /// </para>
    /// </summary>
    public readonly struct Move : IEquatable<Move>
    {
        public readonly int From;
        public readonly int To;

        public Move(int from, int to)
        {
            if (from < 0) throw new ArgumentOutOfRangeException(nameof(from), from, "Index must be non-negative.");
            if (to < 0) throw new ArgumentOutOfRangeException(nameof(to), to, "Index must be non-negative.");

            From = from;
            To = to;
        }

        public Move Inverse() => new Move(To, From);

        public bool Equals(Move other) => From == other.From && To == other.To;
        public override bool Equals(object obj) => obj is Move other && Equals(other);
        public override int GetHashCode() => (From * 397) ^ To;
        public override string ToString() => $"{From}->{To}";

        public static bool operator ==(Move left, Move right) => left.Equals(right);
        public static bool operator !=(Move left, Move right) => !left.Equals(right);
    }
}
