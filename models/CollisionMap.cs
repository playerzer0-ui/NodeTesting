using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace NodeTesting.models
{
    /// <summary>
    /// A map used for collision instead of visuals. Every number 0 or above in its CSV is
    /// solid, and the number is the tile's <b>kind</b> (e.g. 0 = wall, 1 = ice, 2 = mud).
    /// Negative numbers (e.g. -1) are empty.
    /// </summary>
    /// <remarks>
    /// When the map loads, touching tiles of the same kind are merged into as few rectangles
    /// as possible, so a 40-tile wall is one <see cref="CollisionRect"/>, not 40. Tiles of
    /// different kinds never merge with each other.
    /// </remarks>
    public class CollisionMap : Map
    {
        private readonly List<CollisionRect> allRects = new List<CollisionRect>();
        private readonly Dictionary<int, List<CollisionRect>> rectsByKind = new Dictionary<int, List<CollisionRect>>();

        public CollisionMap(string texturePath, int tileWidth, int tileHeight, string csvPath)
            : base(texturePath, tileWidth, tileHeight)
        {
            LoadCSV(csvPath);
            BuildMergedRects();
        }

        /// <summary>Every kind (number 0 or above) that appears in the map.</summary>
        public IEnumerable<int> Kinds => rectsByKind.Keys;

        private void BuildMergedRects()
        {
            // Find every kind used in the map
            var kinds = new SortedSet<int>();
            foreach (int value in MapData)
                if (value >= 0)
                    kinds.Add(value);

            foreach (int kind in kinds)
            {
                var rects = new List<CollisionRect>();

                foreach (Rectangle tiles in TileMerger.Merge(MapData, kind))
                {
                    int pixelWidth = tiles.Width * TileWidth;
                    int pixelHeight = tiles.Height * TileHeight;

                    // CollisionRect takes a center
                    var rect = new CollisionRect(
                        tiles.X * TileWidth + pixelWidth / 2,
                        tiles.Y * TileHeight + pixelHeight / 2,
                        pixelWidth, pixelHeight) { IsStatic = true };

                    rects.Add(rect);
                    allRects.Add(rect);
                }

                rectsByKind[kind] = rects;
            }
        }

        /// <summary>All solid rectangles, of every kind, already merged.</summary>
        public List<CollisionRect> GetCollisionRects() => allRects;

        /// <summary>The merged rectangles of one kind, e.g. only walls or only ice. Empty if the kind isn't used.</summary>
        public List<CollisionRect> GetCollisionRects(int kind) =>
            rectsByKind.TryGetValue(kind, out var rects) ? rects : new List<CollisionRect>();

        /// <summary>
        /// The kind of the tile at a world position, or -1 if it's empty or outside the map.
        /// Handy for "what am I standing on?", e.g. <c>GetKindAt(player.Feet) == Ice</c>.
        /// </summary>
        public int GetKindAt(Vector2 worldPosition)
        {
            int col = FloorDiv((int)System.MathF.Floor(worldPosition.X), TileWidth);
            int row = FloorDiv((int)System.MathF.Floor(worldPosition.Y), TileHeight);

            if (col < 0 || col >= MapWidth || row < 0 || row >= MapHeight)
                return -1;

            int value = MapData[row, col];
            return value >= 0 ? value : -1;
        }

        /// <summary>
        /// Whether the collider touches a solid tile. Pass <paramref name="kind"/> to only
        /// check one kind (e.g. only walls); leave it out to check all of them.
        /// </summary>
        public bool CheckCollision(CollisionRect collider, int? kind = null)
        {
            foreach (CollisionRect rect in RectsFor(kind))
                if (collider.Intersects(rect))
                    return true;
            return false;
        }

        /// <summary>Same as <see cref="CheckCollision(CollisionRect, int?)"/>, for a plain Rectangle.</summary>
        public bool CheckCollision(Rectangle rectangle, int? kind = null)
        {
            foreach (CollisionRect rect in RectsFor(kind))
                if (rectangle.Intersects(rect.Rect))
                    return true;
            return false;
        }

        /// <summary>
        /// Grid positions of the solid tiles a rectangle overlaps. Pass <paramref name="kind"/>
        /// to only count one kind.
        /// </summary>
        public List<Point> GetIntersectingTiles(Rectangle target, int? kind = null)
        {
            var intersections = new List<Point>();

            int leftTile = FloorDiv(target.Left, TileWidth);
            int rightTile = FloorDiv(target.Right - 1, TileWidth);
            int topTile = FloorDiv(target.Top, TileHeight);
            int bottomTile = FloorDiv(target.Bottom - 1, TileHeight);

            for (int x = leftTile; x <= rightTile; x++)
            {
                for (int y = topTile; y <= bottomTile; y++)
                {
                    if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight && IsSolid(MapData[y, x], kind))
                        intersections.Add(new Point(x, y));
                }
            }

            return intersections;
        }

        /// <summary>
        /// Pushes <paramref name="rect"/> out of solid tiles on the X axis. Move and resolve
        /// one axis at a time. Pass <paramref name="kind"/> to only collide with one kind.
        /// </summary>
        public void ResolveCollisionHorizontal(ref Rectangle rect, float velocityX, int? kind = null)
        {
            foreach (Point tilePos in GetIntersectingTiles(rect, kind))
            {
                Rectangle tileRect = new Rectangle(tilePos.X * TileWidth, tilePos.Y * TileHeight, TileWidth, TileHeight);

                if (velocityX > 0)       // Moving right
                    rect.X = tileRect.Left - rect.Width;
                else if (velocityX < 0)  // Moving left
                    rect.X = tileRect.Right;
            }
        }

        /// <summary>
        /// Pushes <paramref name="rect"/> out of solid tiles on the Y axis. Move and resolve
        /// one axis at a time. Pass <paramref name="kind"/> to only collide with one kind.
        /// </summary>
        public void ResolveCollisionVertical(ref Rectangle rect, float velocityY, int? kind = null)
        {
            foreach (Point tilePos in GetIntersectingTiles(rect, kind))
            {
                Rectangle tileRect = new Rectangle(tilePos.X * TileWidth, tilePos.Y * TileHeight, TileWidth, TileHeight);

                if (velocityY > 0)       // Moving down
                    rect.Y = tileRect.Top - rect.Height;
                else if (velocityY < 0)  // Moving up
                    rect.Y = tileRect.Bottom;
            }
        }

        /// <summary>Draws every merged collision rectangle (debugging).</summary>
        public void DrawAllCollisionRects(Color color)
        {
            foreach (CollisionRect rect in allRects)
                rect.Draw(color);
        }

        /// <summary>Draws the merged rectangles of one kind (debugging).</summary>
        public void DrawCollisionRects(int kind, Color color)
        {
            foreach (CollisionRect rect in GetCollisionRects(kind))
                rect.Draw(color);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private List<CollisionRect> RectsFor(int? kind) =>
            kind.HasValue ? GetCollisionRects(kind.Value) : allRects;

        private static bool IsSolid(int value, int? kind) =>
            value >= 0 && (!kind.HasValue || value == kind.Value);
    }
}
