using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NodeTesting.models;
using System;

namespace NodeTesting
{
    public class Player : Sprite
    {
        private Vector2 dir;
        private int speed = 300;
        private CollisionRect rect;

        public Player(string texture, Vector2 pos) : base(texture, pos)
        {
            rect = new CollisionRect(0, 0, 80, 30);
            rect.SetOffsetExtra(0, 25);
        }
        public CollisionRect Rect { get => rect; set => rect = value; }

        public void Update(GameTime gt)
        {
            dir = Vector2.Zero;
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            KeyboardState kState = Keyboard.GetState();

            if (kState.IsKeyDown(Keys.W)) dir.Y -= 1;
            if (kState.IsKeyDown(Keys.S)) dir.Y += 1;
            if (kState.IsKeyDown(Keys.A)) dir.X -= 1;
            if (kState.IsKeyDown(Keys.D)) dir.X += 1;

            if (dir != Vector2.Zero) dir.Normalize();

            pos += dir * dt * speed;

            rect.UpdateRect((int)Math.Round(pos.X), (int)Math.Round(pos.Y));

        }
    }
}
