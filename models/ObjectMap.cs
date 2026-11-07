using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace NodeTesting.models
{
    /// <summary>
    /// A CSV map that places objects instead of drawing tiles. Each number in the CSV
    /// stands for a kind of object (e.g. 1 = tree, 2 = coin); <see cref="Spawn{T}"/>
    /// creates your object at every cell with that number. Nothing is drawn by the map
    /// itself: the objects you create update and draw themselves.
    /// </summary>
    /// <example>
    /// <code>
    /// var placements = new ObjectMap("objects.csv", 16, 16);
    /// List&lt;Tree&gt; trees = placements.Spawn(1, position => new Tree(position));
    /// List&lt;Coin&gt; coins = placements.Spawn(2, position => new Coin(position));
    /// </code>
    /// </example>
    public class ObjectMap
    {
        private readonly int[,] data;

        /// <summary>Width of the map, in cells.</summary>
        public int Width { get; }

        /// <summary>Height of the map, in cells.</summary>
        public int Height { get; }

        public int TileSizeX { get; }
        public int TileSizeY { get; }

        /// <summary>The map's area in world pixels.</summary>
        public Rectangle PixelBounds => new Rectangle(0, 0, Width * TileSizeX, Height * TileSizeY);

        /// <summary>
        /// Loads a placement CSV. Use the same tile size as your <see cref="TileMap"/>
        /// so objects line up with the tiles. Empty cells can be -1 (or any number you don't spawn).
        /// </summary>
        /// <param name="csvPath">The CSV file, looked up like <see cref="TileMap"/>'s.</param>
        /// <param name="tileWidth">Width of one cell in pixels.</param>
        /// <param name="tileHeight">Height of one cell in pixels.</param>
        public ObjectMap(string csvPath, int tileWidth, int tileHeight)
        {
            data = CsvGrid.Load(csvPath);
            Height = data.GetLength(0);
            Width = data.GetLength(1);
            TileSizeX = tileWidth;
            TileSizeY = tileHeight;
        }

        /// <summary>
        /// Creates an object at every cell containing <paramref name="id"/>, going left to
        /// right, top to bottom, and returns them in that order.
        /// </summary>
        /// <param name="id">The number in the CSV that marks this kind of object.</param>
        /// <param name="create">
        /// Makes one object. It's given the <b>bottom-center</b> of the cell in world
        /// pixels: where something standing in that cell touches the ground.
        /// For something that should sit in the middle of the cell, subtract half a tile from Y.
        /// </param>
        public List<T> Spawn<T>(int id, Func<Vector2, T> create)
        {
            var objects = new List<T>();
            foreach (Vector2 position in GetPositions(id))
                objects.Add(create(position));
            return objects;
        }

        /// <summary>
        /// The bottom-center (in world pixels) of every cell containing <paramref name="id"/>,
        /// left to right, top to bottom.
        /// </summary>
        public List<Vector2> GetPositions(int id)
        {
            var positions = new List<Vector2>();

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (data[row, col] == id)
                        positions.Add(new Vector2(col * TileSizeX + TileSizeX / 2f, (row + 1) * TileSizeY));
                }
            }

            return positions;
        }
    }
}
