using System;

namespace ColorStackSort.Logic
{
    /// <summary>
    /// One container (peg / tube / slot) holding a bounded stack of coloured items.
    /// Index 0 is the bottom, <see cref="Count"/> - 1 the top.
    /// </summary>
    public sealed class StackContainer
    {
        private readonly ColorId[] _items;
        private int _count;

        public StackContainer(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");

            _items = new ColorId[capacity];
        }

        public int Capacity => _items.Length;
        public int Count => _count;
        public bool IsEmpty => _count == 0;
        public bool IsFull => _count == _items.Length;
        public int FreeSpace => _items.Length - _count;

        public ColorId this[int index] =>
            index >= 0 && index < _count
                ? _items[index]
                : throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Valid range is 0..{_count - 1}.");

        /// <summary>
        /// The top item. Throws when empty — reading the top of an empty container is a
        /// programmer error, so it fails fast rather than returning a sentinel that would
        /// propagate silently into a move decision.
        /// </summary>
        public ColorId Top => _count > 0
            ? _items[_count - 1]
            : throw new InvalidOperationException("Container is empty; nothing on top.");

        /// <summary>Colour of the top run. Throws when empty, same reasoning as <see cref="Top"/>.</summary>
        public ColorId TopRunColor => Top;

        /// <summary>
        /// Length of the MAXIMAL run of same-coloured items at the top. 0 when empty.
        /// "Maximal" is load-bearing: the player's move always takes the entire run.
        /// </summary>
        public int TopRunLength
        {
            get
            {
                if (_count == 0) return 0;

                var color = _items[_count - 1];
                var length = 1;
                for (var i = _count - 2; i >= 0 && _items[i] == color; i--) length++;

                return length;
            }
        }

        /// <summary>True when non-empty and every item shares one colour.</summary>
        public bool IsMonochrome => _count > 0 && TopRunLength == _count;

        public void Push(ColorId color)
        {
            if (IsFull) throw new InvalidOperationException("Container is full.");
            _items[_count++] = color;
        }

        public void PushRange(ColorId color, int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
            if (amount > FreeSpace)
                throw new InvalidOperationException($"Cannot push {amount} item(s); only {FreeSpace} free.");

            for (var i = 0; i < amount; i++) _items[_count++] = color;
        }

        /// <summary>
        /// Removes <paramref name="amount"/> items from the top and returns their shared colour.
        /// Throws if the range spans more than one colour — every caller pops within a single run,
        /// so a mixed range means the caller computed the wrong length, which is exactly the bug
        /// class that makes a generated level unsolvable.
        /// </summary>
        public ColorId PopRange(int amount)
        {
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
            if (amount > _count)
                throw new InvalidOperationException($"Cannot pop {amount} item(s); only {_count} present.");

            var color = _items[_count - 1];
            for (var i = _count - amount; i < _count; i++)
            {
                if (_items[i] != color)
                    throw new InvalidOperationException(
                        $"Pop range of {amount} spans more than one colour ({_items[i]} and {color}).");
            }

            _count -= amount;
            return color;
        }

        public StackContainer Clone()
        {
            var clone = new StackContainer(Capacity);
            Array.Copy(_items, clone._items, _count);
            clone._count = _count;
            return clone;
        }
    }
}
