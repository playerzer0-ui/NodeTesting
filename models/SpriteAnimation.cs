using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NodeTesting.models
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteManager"/> class.
        /// </summary>
        /// <param name="Texture">The path to the texture in the Content folder.</param>
        /// <param name="frames">The total number of frames in the texture.</param>
        /// <remarks>
        /// This constructor automatically divides the texture width evenly based on the specified frame count.
        /// </remarks>
        public SpriteManager(string Texture, int frames)
        {
            this.frames = frames;
            this.Texture = Globals.Content.Load<Texture2D>(Texture);
            int width = this.Texture.Width / frames;
            Rectangles = new Rectangle[frames];

            for (int i = 0; i < frames; i++)
                Rectangles[i] = new Rectangle(i * width, 0, width, this.Texture.Height);
        }

        /// <summary>
        /// Draws the current frame of the sprite to the screen.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> used for rendering.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Position, Rectangles[FrameIndex], Color, Rotation, Origin, Scale, SpriteEffect, 0f);
        }
    }

    /// <summary>
    /// Extends <see cref="SpriteManager"/> to add frame-based animation playback.
    /// </summary>
    public class SpriteAnimation : SpriteManager
    {
        private float timeElapsed;
        public bool IsLooping = true;
        private float timeToUpdate; //default, you may have to change it
        public int FramesPerSecond { set { timeToUpdate = 1f / value; } }
        public bool IsFinished = false;
        public bool IsReversed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteAnimation"/> class.
        /// </summary>
        /// <param name="Texture">The path to the texture in the Content folder.</param>
        /// <param name="frames">The total number of frames in the sprite sheet.</param>
        /// <param name="fps">The desired frames per second for animation playback.</param>
        public SpriteAnimation(string Texture, int frames, int fps) : base(Texture, frames)
        {
            FramesPerSecond = fps;
        }

        /// <summary>
        /// Updates the animation based on elapsed time, looping or reversing as needed.
        /// </summary>
        /// <param name="gameTime">The current <see cref="GameTime"/> instance provided by MonoGame.</param>
        /// <remarks>
        /// This method advances the animation to the next frame based on the configured <see cref="FramesPerSecond"/>.
        /// </remarks>
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

        /// <summary>
        /// Updates the animation once without looping behavior.
        /// </summary>
        /// <param name="gameTime">The current <see cref="GameTime"/> instance provided by MonoGame.</param>
        /// <remarks>
        /// This version of <see cref="Update(GameTime)"/> stops at the final frame instead of looping.
        /// </remarks>
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

        /// <summary>
        /// Sets the current animation frame manually.
        /// </summary>
        /// <param name="frame">The frame index to switch to.</param>
        public void setFrame(int frame)
        {
            FrameIndex = frame;
        }

        /// <summary>
        /// Toggles the animation playback direction.
        /// </summary>
        public void ReverseAnimation()
        {
            IsReversed = !IsReversed;
        }
    }
}