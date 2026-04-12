using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace NodeTesting.models
{
    public abstract class Map
    {
        protected Texture2D Texture;
        protected Dictionary<int, Rectangle> TileSources;
        protected int[,] MapData;
        protected int MapWidth;
        protected int MapHeight;
        protected int TileWidth;
        protected int TileHeight;

        public int Width => MapWidth;
        public int Height => MapHeight;
        public int TileSizeX => TileWidth;
        public int TileSizeY => TileHeight;

        protected Map(string texturePath, int tileWidth, int tileHeight)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Texture = Globals.Content.Load<Texture2D>(texturePath);
            SplitTileset();
        }

        private void SplitTileset()
        {
            TileSources = new Dictionary<int, Rectangle>();

            int tilesPerRow = Texture.Width / TileWidth;
            int tilesPerColumn = Texture.Height / TileHeight;
            int tileId = 0;

            for (int row = 0; row < tilesPerColumn; row++)
            {
                for (int col = 0; col < tilesPerRow; col++)
                {
                    TileSources.Add(tileId, new Rectangle(
                        col * TileWidth,
                        row * TileHeight,
                        TileWidth,
                        TileHeight
                    ));
                    tileId++;
                }
            }
        }

        protected void LoadCSV(string csvPath)
        {
            string fullPath = FindFile(csvPath);
            string[] lines = File.ReadAllLines(fullPath);
            MapHeight = lines.Length;

            string[] firstLine = lines[0].Split(',');
            MapWidth = firstLine.Length;

            MapData = new int[MapHeight, MapWidth];

            for (int row = 0; row < MapHeight; row++)
            {
                string[] values = lines[row].Split(',');
                for (int col = 0; col < MapWidth; col++)
                {
                    if (col < values.Length && int.TryParse(values[col].Trim(), out int value))
                    {
                        MapData[row, col] = value;
                    }
                }
            }
        }

        private string FindFile(string fileName)
        {
            string[] paths = {
                Path.Combine(Directory.GetCurrentDirectory(), fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Content", fileName),
                Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Content", fileName),
                fileName
            };

            foreach (string path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException($"Could not find file '{fileName}'");
        }

        public virtual void Draw()
        {
            for (int row = 0; row < MapHeight; row++)
            {
                for (int col = 0; col < MapWidth; col++)
                {
                    int tileId = MapData[row, col];
                    if (tileId >= 0 && TileSources.ContainsKey(tileId))
                    {
                        Vector2 position = new Vector2(col * TileWidth, row * TileHeight);
                        Globals.spriteBatch.Draw(Texture, position, TileSources[tileId], Color.White);
                    }
                }
            }
        }
    }
}