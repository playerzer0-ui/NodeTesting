using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NodeTesting.models;

namespace CardCeption
{
    public class SpriteManager
    {
        protected Texture2D Texture;
        public Vector2 Position = Vector2.Zero;
        public Color Color = Color.White;
        public Vector2 Origin;
        public float Rotation = 0f;
        public float Scale = 1f;
        public SpriteEffects SpriteEffect;
        protected Rectangle[] Rectangles;
        public int FrameIndex = 0;
        public int frames;

        public SpriteManager(string Texture, int frames)
        {
            this.frames = frames;
            this.Texture = Globals.Content.Load<Texture2D>(Texture);
            int width = this.Texture.Width / frames;
            Rectangles = new Rectangle[frames];

            for (int i = 0; i < frames; i++)
                Rectangles[i] = new Rectangle(i * width, 0, width, this.Texture.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, Rectangles[FrameIndex], Color, Rotation, Origin, Scale, SpriteEffect, 0f);
        }
    }

    public class SpriteAnimation : SpriteManager
    {
        private float timeElapsed;
        public bool IsLooping = true;
        private float timeToUpdate; //default, you may have to change it
        public int FramesPerSecond { set { timeToUpdate = (1f / value); } }
        public bool IsFinished = false;
        public bool IsReversed = false;

        public SpriteAnimation(string Texture, int frames, int fps) : base(Texture, frames)
        {
            FramesPerSecond = fps;
        }

        public void Update(GameTime gameTime)
        {
            timeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeElapsed > timeToUpdate)
            {
                timeElapsed -= timeToUpdate;

                if (!IsReversed)
                {
                    if (FrameIndex < Rectangles.Length - 1)
                        FrameIndex++;
                    else if (IsLooping)
                        FrameIndex = 0;
                    else
                        IsFinished = true;
                }
                else
                {
                    if (FrameIndex > 0)
                        FrameIndex--;
                    else if (IsLooping)
                        FrameIndex = Rectangles.Length - 1;
                    else
                        IsFinished = true;
                }
            }
        }

        public void UpdateOnce(GameTime gameTime)
        {
            timeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (timeElapsed > timeToUpdate)
            {
                timeElapsed -= timeToUpdate;

                if (!IsReversed)
                {
                    if (FrameIndex < Rectangles.Length - 1)
                        FrameIndex++;
                    else
                        IsFinished = true;
                }
                else
                {
                    if (FrameIndex > 0)
                        FrameIndex--;
                    else
                        IsFinished = true;
                }
            }
        }

        public void setFrame(int frame)
        {
            FrameIndex = frame;
        }

        public void ReverseAnimation()
        {
            IsReversed = !IsReversed;
        }
    }
}