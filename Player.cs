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

        public void Update(GameTime gt, ICollider[] walls)
        {
            dir = Vector2.Zero;
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;

            if (Globals.Input.IsPressed("MoveUp")) dir.Y -= 1;
            if (Globals.Input.IsPressed("MoveDown")) dir.Y += 1;
            if (Globals.Input.IsPressed("MoveLeft")) dir.X -= 1;
            if (Globals.Input.IsPressed("MoveRight")) dir.X += 1;

            if (dir != Vector2.Zero) dir.Normalize();
            pos += dir * dt * speed;
            rect.UpdateRect((int)Math.Round(pos.X), (int)Math.Round(pos.Y));
            foreach (ICollider wall in walls)
                wall.ResolveAgainst(rect);
            pos = rect.Center - new Vector2(0, 25);
        }
    }
}
