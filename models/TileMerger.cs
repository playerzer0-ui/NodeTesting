using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace NodeTesting.models
{
    /// <summary>
    /// Turns painted tiles of one kind into as few rectangles as possible, so a long wall
    /// becomes one rectangle instead of one per tile. Tiles only merge with tiles of the same kind.
    /// </summary>
    internal static class TileMerger
    {
        /// <summary>
        /// Merges every tile containing <paramref name="kind"/> into rectangles,
        /// measured in tiles (X = column, Y = row, Width/Height = tile counts).
        /// </summary>
        public static List<Rectangle> Merge(int[,] data, int kind)
        {
            int height = data.GetLength(0);
            int width = data.GetLength(1);
            var rects = new List<Rectangle>();
            var used = new bool[height, width];   // tiles already inside a rectangle

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    if (used[row, col] || data[row, col] != kind)
                        continue;

                    // Step 1: grow right while the next tile is the same kind
                    int spanWidth = 1;
                    while (col + spanWidth < width
                           && data[row, col + spanWidth] == kind
                           && !used[row, col + spanWidth])
                        spanWidth++;

                    // Step 2: grow down while the whole row below matches that span
                    int spanHeight = 1;
                    while (row + spanHeight < height
                           && RowMatches(data, used, row + spanHeight, col, spanWidth, kind))
                        spanHeight++;

                    // Mark those tiles as taken so they aren't merged twice
                    for (int r = row; r < row + spanHeight; r++)
                        for (int c = col; c < col + spanWidth; c++)
                            used[r, c] = true;

                    rects.Add(new Rectangle(col, row, spanWidth, spanHeight));
                }
            }

            return rects;
        }

        // Is every tile in this row, from col to col + width, the given kind and not taken yet?
        private static bool RowMatches(int[,] data, bool[,] used, int row, int col, int width, int kind)
        {
            for (int c = col; c < col + width; c++)
                if (data[row, c] != kind || used[row, c])
                    return false;
            return true;
        }
    }
}
