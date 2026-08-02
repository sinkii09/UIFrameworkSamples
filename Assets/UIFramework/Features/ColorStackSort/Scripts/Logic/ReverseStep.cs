using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// One scramble step used while GENERATING a level: move <see cref="Amount"/> items — a
    /// portion of the top run, not necessarily the whole run — from <see cref="From"/> to
    /// <see cref="To"/>.
    /// <para>
    /// The partial amount is the whole point, and is why this is not a <see cref="Move"/>.
    /// See <see cref="LevelGenerator.IsLegalReverseStep"/> for why moving only part of a run is
    /// what keeps the scramble invertible.
    /// </para>
    /// </summary>
    public readonly struct ReverseStep : IEquatable<ReverseStep>
    {
        public readonly int From;
        public readonly int To;
        public readonly int Amount;

        public ReverseStep(int from, int to, int amount)
        {
            From = from;
            To = to;
            Amount = amount;
        }

        /// <summary>The player-facing move that undoes this step: run travels back To -> From.</summary>
        public Move ToForwardMove() => new Move(To, From);

        public bool Equals(ReverseStep other) =>
            From == other.From && To == other.To && Amount == other.Amount;

        public override bool Equals(object obj) => obj is ReverseStep other && Equals(other);
        public override int GetHashCode() => ((From * 397) ^ To) * 397 ^ Amount;
        public override string ToString() => $"{From}->{To} x{Amount}";

        public static bool operator ==(ReverseStep left, ReverseStep right) => left.Equals(right);
        public static bool operator !=(ReverseStep left, ReverseStep right) => !left.Equals(right);
    }
}
