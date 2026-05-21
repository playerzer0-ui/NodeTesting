using Microsoft.Xna.Framework;
using NodeTesting.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeTesting
{
    public class Bubble
    {
        private CollisionCircle circle;
        private Vector2 dir = Vector2.Zero;
        private float timer = 3f;
        private float maxTimer = 3f;
        private int speed = 200;
        private bool flip = false;

        public CollisionCircle Circle { get => circle; set => circle = value; }

        public Bubble(int x, int y)
        {
            Circle = new CollisionCircle(x, y, 20);
        }

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            timer -= dt;

            if (timer <= 0)
            {
                flip = !flip;
                timer = maxTimer;
            }

            dir.X = flip ? -1 : 1;

            float newX = Circle.Center.X + dir.X * speed * dt;
            Circle.UpdateCenter((int)Math.Round(newX), (int)Math.Round(Circle.Center.Y));
        }

        public void Draw()
        {
            Circle.Draw(Color.Black);
        }
    }
}
