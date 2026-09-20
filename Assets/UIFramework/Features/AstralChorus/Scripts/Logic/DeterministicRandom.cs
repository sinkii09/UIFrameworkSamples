using System;
using System.Collections.Generic;

namespace AstralChorus.Logic
{
    /// <summary>
    /// PCG-XSH-RR 32-bit generator, implemented here on purpose.
    /// <para>
    /// <see cref="System.Random"/> is documented as NOT guaranteed to produce the same sequence
    /// across .NET versions, and Unity's Mono and IL2CPP runtimes are different implementations
    /// again. A tuning tool whose answers move when the runtime moves is worse than no tool: the
    /// numbers in GDD §10–§11 would change without anyone editing a design decision.
    /// </para>
    /// <para>
    /// A near-identical class exists in <c>UIFramework.ColorStackSort.Logic</c>. It is duplicated
    /// rather than shared because referencing that assembly would make one game's PRNG contract a
    /// build dependency of an unrelated game's economy model. The rationale there (a level's
    /// identity IS its seed) does not apply here.
    /// </para>
    /// </summary>
    public sealed class DeterministicRandom
    {
        // Must be odd; any odd constant selects a valid stream.
        private const ulong Increment = 0xDA3E39CB94B95BDBUL;
        private const ulong Multiplier = 6364136223846793005UL;

        // 2^32. Dividing by this (rather than uint.MaxValue) keeps NextDouble in [0, 1) —
        // dividing by uint.MaxValue makes 1.0 attainable, which would let a rate comparison of
        // "roll < rate" behave differently at the top of the range.
        private const double UIntRange = 4294967296.0;

        private ulong _state;

        public DeterministicRandom(int seed)
        {
            unchecked
            {
                _state = 0UL;
                NextUInt();
                _state += (uint)seed; // via uint so a negative seed does not sign-extend
                NextUInt();
            }
        }

        public uint NextUInt()
        {
            unchecked
            {
                var previous = _state;
                _state = previous * Multiplier + Increment;

                var xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
                var rotation = (int)(previous >> 59);

                return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
            }
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => NextUInt() / UIntRange;

        /// <summary>
        /// Uniform integer in [0, <paramref name="exclusiveMax"/>). Rejection sampling rather than a
        /// plain modulo, which biases low values whenever the range does not divide 2^32 evenly.
        /// </summary>
        public int Next(int exclusiveMax)
        {
            if (exclusiveMax < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax), exclusiveMax, "Upper bound must be positive.");

            var bound = (uint)exclusiveMax;
            var threshold = (uint)((0x100000000UL - bound) % bound);

            while (true)
            {
                var value = NextUInt();
                if (value >= threshold) return (int)(value % bound);
            }
        }

        /// <summary>
        /// Index into <paramref name="weights"/> chosen proportionally. Weights must be positive and
        /// their total positive; an empty or all-zero set is a programmer error, not a silent zero.
        /// </summary>
        public int NextWeightedIndex(IReadOnlyList<int> weights)
        {
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (weights.Count == 0)
                throw new ArgumentException("Cannot pick from an empty weight set.", nameof(weights));

            var total = 0;
            for (var i = 0; i < weights.Count; i++)
            {
                if (weights[i] < 0)
                    throw new ArgumentException("Weights must not be negative.", nameof(weights));
                total += weights[i];
            }

            if (total <= 0)
                throw new ArgumentException("Weights must sum to a positive value.", nameof(weights));

            var roll = Next(total);
            for (var i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0) return i;
            }

            // Unreachable while the loop above consumes the full total; kept so a future edit that
            // breaks that property fails loudly instead of returning a quietly wrong index.
            throw new InvalidOperationException("Weighted pick fell through its own total.");
        }
    }
}
