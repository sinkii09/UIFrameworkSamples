using Sinkii09.UIFramework;

namespace RecyclerShowcase.Editor
{
    /// <summary>The RecyclerView settings a showcase list is built with (written into private serialized fields).</summary>
    internal readonly struct RecyclerSpec
    {
        public readonly ScrollDirection Direction;
        public readonly float CellSize;
        public readonly float Spacing;
        public readonly int CrossAxisCount;
        public readonly float CrossSpacing;

        public RecyclerSpec(ScrollDirection direction, float cellSize, float spacing,
                            int crossAxisCount = 1, float crossSpacing = 0f)
        {
            Direction = direction;
            CellSize = cellSize;
            Spacing = spacing;
            CrossAxisCount = crossAxisCount;
            CrossSpacing = crossSpacing;
        }
    }
}
