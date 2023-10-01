using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace NodeTesting.models
{
    public class Sprite
    {
        protected Texture2D texture;
        protected Vector2 pos;
        protected Vector2 origin;
        protected float rotation = 0f;
        protected float scale = 1f;

        public Vector2 Pos { get => pos; set => pos = value; }

        public Sprite(string texture, Vector2 pos) 
        {
            this.texture = Globals.Content.Load<Texture2D>(texture);
            this.pos = pos;
            origin = new Vector2(this.texture.Width / 2, this.texture.Height / 2);
        }

        public void Draw(Color color)
        {
            Globals.spriteBatch.Draw(texture, pos, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
