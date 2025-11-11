using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace NodeTesting.models
{
    /// <summary>
    /// Represents a rectangular collision shape used for detecting overlaps and containment.
    /// </summary>
    public class CollisionRect : ICollider
    {
        private Rectangle rect;
        private Texture2D pixel;
        private int offsetX;
        private int offsetY;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollisionRect"/> class.
        /// </summary>
        /// <param name="x">The x-coordinate of the rectangle's center.</param>
        /// <param name="y">The y-coordinate of the rectangle's center.</param>
        /// <param name="width">The width of the rectangle, in pixels.</param>
        /// <param name="height">The height of the rectangle, in pixels.</param>
        public CollisionRect(int x, int y, int width, int height)
        {
            rect = new Rectangle(x, y, width, height);
            pixel = new Texture2D(Globals.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            rect.Offset(-(width / 2), -(height / 2));
        }

        /// <summary>
        /// Gets the internal <see cref="Rectangle"/> that defines this collider's bounds.
        /// </summary>
        public Rectangle Rect => rect;

        /// <summary>
        /// Gets the top-left position of the rectangle in world space.
        /// </summary>
        public Vector2 Pos => new(rect.X, rect.Y);

        /// <summary>
        /// Checks whether this rectangle intersects with another collider.
        /// </summary>
        /// <param name="other">Another collider to test against.</param>
        /// <returns><see langword="true"/> if the colliders overlap; otherwise, <see langword="false"/>.</returns>
        public bool Intersects(ICollider other)
        {
            switch (other)
            {
                case CollisionRect r:
                    return rect.Intersects(r.Rect);
                case CollisionCircle c:
                    return c.Collides(rect);
                default:
                    throw new NotSupportedException("Unsupported collider type.");
            }
        }

        /// <summary>
        /// Checks whether a given point lies within this rectangle.
        /// </summary>
        /// <param name="target">The point to test.</param>
        /// <returns><see langword="true"/> if the point is contained within the rectangle; otherwise, <see langword="false"/>.</returns>
        public bool Contains(Point target)
        {
            return rect.Contains(target);
        }

        /// <summary>
        /// Sets an additional offset applied when updating the rectangle's position.
        /// </summary>
        /// <param name="x">The horizontal offset, in pixels.</param>
        /// <param name="y">The vertical offset, in pixels.</param>
        public void SetOffsetExtra(int x, int y)
        {
            offsetX = x;
            offsetY = y;
        }

        /// <summary>
        /// Updates the rectangle's position while keeping it centered and applying any extra offsets.
        /// </summary>
        /// <param name="x">The new x-coordinate of the rectangle's center.</param>
        /// <param name="y">The new y-coordinate of the rectangle's center.</param>
        public void UpdateRect(int x, int y)
        {
            rect.X = x;
            rect.Y = y;
            rect.Offset(-(rect.Width / 2) + offsetX, -(rect.Height / 2) + offsetY);
        }

        /// <summary>
        /// Draws the rectangle collider for debugging or visualization.
        /// </summary>
        /// <param name="color">The color to draw the collider with.</param>
        public void Draw(Color color)
        {
            Globals.spriteBatch.Draw(pixel, rect, color);
        }
    }

}
