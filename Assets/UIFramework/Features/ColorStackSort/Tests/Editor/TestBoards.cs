using System.Text;
using ColorStackSort.Logic;

namespace ColorStackSort.Tests
{
    /// <summary>Shared builders so tests read as board pictures rather than setup code.</summary>
    internal static class TestBoards
    {
        internal static readonly int[] Empty = new int[0];

        /// <summary>
        /// Builds a container from colour indices listed BOTTOM to TOP, matching how a stack is
        /// drawn on screen from the base up.
        /// </summary>
        internal static StackContainer Container(int capacity, params int[] bottomToTop)
        {
            var container = new StackContainer(capacity);
            foreach (var color in bottomToTop) container.Push(new ColorId(color));

            return container;
        }

        /// <summary>Builds a board; each array is one container, listed bottom to top.</summary>
        internal static BoardState Board(int capacity, params int[][] containers)
        {
            var board = new BoardState(containers.Length, capacity);
            for (var i = 0; i < containers.Length; i++)
            {
                foreach (var color in containers[i]) board[i].Push(new ColorId(color));
            }

            return board;
        }

        /// <summary>Stable textual snapshot of a board, for equality and determinism assertions.</summary>
        internal static string Describe(BoardState board)
        {
            var text = new StringBuilder();
            for (var i = 0; i < board.ContainerCount; i++)
            {
                var container = board[i];
                for (var j = 0; j < container.Count; j++) text.Append(container[j].Value).Append(',');
                text.Append('|');
            }

            return text.ToString();
        }
    }
}
