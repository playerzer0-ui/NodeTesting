using Microsoft.Xna.Framework;
using NodeTesting.models;

namespace NodeTesting
{
    /// <summary>
    /// A 16x32 tree standing on a spot. The trunk base is a wall; the leaves are a
    /// canopy area that fades the tree to half opacity while the player is behind it.
    /// </summary>
    public class Tree
    {
        private const int ImageWidth = 16;
        private const int ImageHeight = 32;

        // Trunk base that blocks the player. Change these to fit your tree art.
        private const int TrunkWidth = 8;
        private const int TrunkHeight = 6;

        private readonly Sprite sprite;
        private float opacity = 1f;

        /// <summary>The trunk base. Add it to the walls the player collides with.</summary>
        public CollisionRect Wall { get; }

        /// <summary>The leaves. Only checked against the player, never pushes anything.</summary>
        public CollisionRect Canopy { get; }

        /// <summary>Where the tree touches the ground (bottom of the image), in world Y.</summary>
        public float BaseY { get; }

        /// <param name="basePosition">Bottom-center of the tree: where its trunk meets the ground.</param>
        public Tree(Vector2 basePosition)
        {
            int x = (int)basePosition.X;
            int y = (int)basePosition.Y;
            BaseY = y;

            // Draw the image so its bottom-middle sits on basePosition
            sprite = new Sprite("tree", basePosition);
            sprite.Origin = new Vector2(ImageWidth / 2f, ImageHeight);

            // CollisionRect takes a center, so these are offset up from the base
            Wall = new CollisionRect(x, y - TrunkHeight / 2, TrunkWidth, TrunkHeight) { IsStatic = true };

            int canopyHeight = ImageHeight - TrunkHeight;   // everything above the trunk base
            Canopy = new CollisionRect(x, y - TrunkHeight - canopyHeight / 2, ImageWidth, canopyHeight);
        }

        public void Update(Player player)
        {
            // In front of the player (its base is lower on screen) and covering their feet
            bool inFrontOfPlayer = BaseY > player.Rect.Rect.Bottom;
            bool playerUnderLeaves = player.Rect.Intersects(Canopy);

            float target = inFrontOfPlayer && playerUnderLeaves ? 0.5f : 1f;

            // Ease toward the target instead of popping
            opacity = MathHelper.Lerp(opacity, target, 0.2f);
        }

        public void Draw()
        {
            sprite.Draw(Color.White * opacity);
        }
    }
}
