using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// A colour on the board, identified by index. Wrapping the byte stops a colour index from
    /// being confused with a container index — both are small integers that flow through the same
    /// call sites, and swapping them silently produces a valid-looking but wrong board.
    /// </summary>
    public readonly struct ColorId : IEquatable<ColorId>
    {
        public readonly byte Value;

        public ColorId(byte value) => Value = value;

        public ColorId(int value)
        {
            if (value < 0 || value > byte.MaxValue)
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Colour index must fit in a byte (0..255).");

            Value = (byte)value;
        }

        public bool Equals(ColorId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ColorId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"C{Value}";

        public static bool operator ==(ColorId left, ColorId right) => left.Value == right.Value;
        public static bool operator !=(ColorId left, ColorId right) => left.Value != right.Value;
    }
}
