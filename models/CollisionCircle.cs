using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace NodeTesting.models
{
    public class CollisionCircle : ICollider
    {
        private Vector2 center;
        private int radius;
        private Texture2D pixel;

        public CollisionCircle(int x, int y, int radius)
        {
            center = new Vector2(x, y);
            this.radius = radius;
            pixel = new Texture2D(Globals.graphics.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        public Vector2 Center { get => center; set => center = value; }
        public int Radius { get => radius; set => radius = value; }

        public bool Contains(Point target)
        {
            float dx = target.X - center.X;
            float dy = target.Y - center.Y;
            return (dx * dx + dy * dy) < (radius * radius);
        }

        public bool Intersects(ICollider other)
        {
            switch (other)
            {
                case CollisionCircle c:
                    float distSq = Vector2.DistanceSquared(center, c.Center);
                    float radiusSum = radius + c.Radius;
                    return distSq < (radiusSum * radiusSum);

                case CollisionRect r:
                    return Collides(r.Rect);

                default:
                    throw new NotSupportedException("Unsupported collider type");
            }
        }

        public bool Collides(Rectangle target)
        {
            float closestX = MathHelper.Clamp(center.X, target.Left, target.Right);
            float closestY = MathHelper.Clamp(center.Y, target.Top, target.Bottom);

            float dx = center.X - closestX;
            float dy = center.Y - closestY;
            return (dx * dx + dy * dy) < (radius * radius);
        }

        public void Draw(Color color)
        {
            int segments = 30;
            Vector2[] points = new Vector2[segments];

            for (int i = 0; i < segments; i++)
            {
                float angle = i * MathHelper.TwoPi / segments;
                points[i] = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
            }

            for (int i = 0; i < segments - 1; i++)
                DrawLine(points[i], points[i + 1], color);

            DrawLine(points[^1], points[0], color);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 2)
        {
            Vector2 delta = end - start;
            float angle = (float)Math.Atan2(delta.Y, delta.X);
            float length = delta.Length();

            Globals.spriteBatch.Draw(
                pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0
            );
        }
    }
}
