using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// PCG-XSH-RR 32-bit generator, implemented here on purpose.
    /// <para>
    /// <see cref="System.Random"/> is explicitly documented as NOT guaranteed to produce the same
    /// sequence across .NET versions, and Unity's Mono/IL2CPP runtimes are a different
    /// implementation again. That is fatal for this game: a level's identity IS its seed — no board
    /// data is ever stored — so a runtime change would silently rewrite every level in the game,
    /// turning a player's "level 47" into a different puzzle. Owning the algorithm makes
    /// determinism a property of this file rather than of whatever runtime happens to execute it.
    /// </para>
    /// </summary>
    public sealed class DeterministicRandom
    {
        // Must be odd; any odd constant selects a valid stream.
        private const ulong Increment = 0xDA3E39CB94B95BDBUL;
        private const ulong Multiplier = 6364136223846793005UL;

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

        /// <summary>
        /// Uniform integer in [0, <paramref name="exclusiveMax"/>). Uses rejection sampling rather
        /// than a plain modulo, which would bias the low values whenever the range does not divide
        /// 2^32 evenly.
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
    }
}
