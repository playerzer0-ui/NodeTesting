using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace NodeTesting.models
{
    public class CollisionRect : ICollider
    {
        private Rectangle rect;
        private Texture2D pixel;
        private int offsetX;
        private int offsetY;

        public CollisionRect(int x, int y, int width, int height)
        {
            rect = new Rectangle(x, y, width, height);
            pixel = new Texture2D(Globals.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            rect.Offset(-(width / 2), -(height / 2));
        }

        public Rectangle Rect => rect;

        public Vector2 Pos => new(rect.X, rect.Y);

        public void SetOffsetExtra(int x, int y)
        {
            offsetX = x;
            offsetY = y;
        }

        public void UpdateRect(int x, int y)
        {
            rect.X = x;
            rect.Y = y;
            rect.Offset(-(rect.Width / 2) + offsetX, -(rect.Height / 2) + offsetY);
        }

        public void Draw(Color color)
        {
            Globals.spriteBatch.Draw(pixel, rect, color);
        }

        public bool Contains(Point target)
        {
            return rect.Contains(target);
        }

        public bool Intersects(ICollider other)
        {
            switch (other)
            {
                case CollisionRect r:
                    return rect.Intersects(r.Rect);

                case CollisionCircle c:
                    return c.Collides(rect); // You already have this logic

                default:
                    throw new NotSupportedException("Unsupported collider type");
            }
        }
    }

}
