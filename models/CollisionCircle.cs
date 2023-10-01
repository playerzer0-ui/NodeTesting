using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace NodeTesting.models
{
    public class CollisionCircle
    {
        private Vector2 center;
        private int radius;
        private Texture2D pixel;

        public CollisionCircle(int x, int y, int radius)
        {
            this.center = new Vector2(x, y);
            this.radius = radius;
            pixel = new Texture2D(Globals.graphics.GraphicsDevice, 1, 1);
            pixel.SetData<Color>(new Color[] { Color.White });
        }

        public Vector2 Center { get => center; set => center = value; }
        public int Radius { get => radius; set => radius = value; }

        public bool Collides(Rectangle target)
        {
            // Calculate the closest point on the rectangle to the circle
            float closestX = MathHelper.Clamp(center.X, target.Left, target.Right);
            float closestY = MathHelper.Clamp(center.Y, target.Top, target.Bottom);

            // Calculate the distance between the circle center and the closest point on the rectangle
            float distanceX = center.X - closestX;
            float distanceY = center.Y - closestY;

            // Use Pythagorean theorem to get the distance between the circle center and the closest point
            float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);

            // Check if the distance is less than the squared circle radius (avoiding square root operation)
            return distanceSquared < (radius * radius);
        }

        public bool Contains(Point target)
        {
            // Calculate the distance between the point and the circle center
            float distanceX = target.X - center.X;
            float distanceY = target.Y - center.Y;
            float distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);

            // Check if the distance is less than the squared circle radius (avoiding square root operation)
            return distanceSquared < (radius * radius);
        }

        public void Draw(Color color)
        {
            int segments = 30; // Adjust this to control the circle's smoothness
            Vector2[] points = new Vector2[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = i * MathHelper.TwoPi / segments;
                float x = Center.X + Radius * (float)Math.Cos(angle);
                float y = Center.Y + Radius * (float)Math.Sin(angle);
                points[i] = new Vector2(x, y);
            }

            for (int i = 0; i < segments - 1; i++)
            {
                DrawLine(points[i], points[i + 1], color);
            }

            DrawLine(points[segments - 1], points[0], color);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 3)
        {
            Vector2 delta = end - start;
            float angle = (float)Math.Atan2(delta.Y, delta.X);
            float length = delta.Length();

            Globals.spriteBatch.Draw(
                pixel, // A 1x1 white texture
                start, // The starting position of the line
                null,
                color, // The color of the line
                angle, // The angle of the line
                Vector2.Zero,
                new Vector2(length, thickness), // The length and thickness of the line
                SpriteEffects.None,
                0
            );
        }
    }
}
