using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NodeTesting.models
{
    /// <summary>
    /// Frame-based animation from a single-row sprite strip: one texture with
    /// equally sized frames laid out left to right. Use one strip per animation.
    /// </summary>
    public class SpriteAnimation
    {
        protected Texture2D Texture;
        public Vector2 Position = Vector2.Zero;
        public Color Color = Color.White;
        public Vector2 Origin;
        public float Rotation = 0f;
        public float Scale = 1f;

        /// <summary>
        /// Draw order when the SpriteBatch uses <c>SpriteSortMode.FrontToBack</c>: higher is drawn
        /// later (on top). For top-down y-sorting set it from the feet: <c>Depth = YSort.Depth(feetY)</c>.
        /// Ignored with the default sort mode, where things are drawn in call order.
        /// </summary>
        public float Depth = 0f;
        public SpriteEffects SpriteEffect;

        protected int FrameWidth;
        protected int FrameHeight;
        protected Rectangle[] Rectangles;

        // Frame tracking
        public int FrameIndex = 0;
        protected float TimeElapsed = 0f;
        protected float TimeToUpdate;

        public bool IsFinished = false;
        public bool IsReversed = false;

        /// <summary>
        /// Creates an animation from a horizontal strip.
        /// </summary>
        /// <param name="texturePath">The path to the texture in the Content folder.</param>
        /// <param name="frames">How many frames are in the strip. Frame width = texture width ÷ frames.</param>
        /// <param name="fps">Playback speed in frames per second.</param>
        public SpriteAnimation(string texturePath, int frames, int fps)
        {
            Texture = Globals.Content.Load<Texture2D>(texturePath);
            FrameWidth = Texture.Width / frames;
            FrameHeight = Texture.Height;

            Rectangles = new Rectangle[frames];
            for (int i = 0; i < frames; i++)
                Rectangles[i] = new Rectangle(i * FrameWidth, 0, FrameWidth, FrameHeight);

            Origin = new Vector2(FrameWidth / 2f, FrameHeight / 2f);
            TimeToUpdate = 1f / fps;
        }

        /// <summary>
        /// Advances the animation, looping back to the start (or end, when reversed).
        /// </summary>
        public void Update(GameTime gameTime)
        {
            TimeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (TimeElapsed > TimeToUpdate)
            {
                TimeElapsed -= TimeToUpdate;

                if (!IsReversed)
                    FrameIndex = FrameIndex < Rectangles.Length - 1 ? FrameIndex + 1 : 0;
                else
                    FrameIndex = FrameIndex > 0 ? FrameIndex - 1 : Rectangles.Length - 1;
            }
        }

        /// <summary>
        /// Advances the animation without looping. Stops on the last frame
        /// (or the first, when reversed) and sets <see cref="IsFinished"/>.
        /// </summary>
        public void UpdateOnce(GameTime gameTime)
        {
            TimeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (TimeElapsed > TimeToUpdate)
            {
                TimeElapsed -= TimeToUpdate;

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

        /// <summary>Draws the current frame.</summary>
        public void Draw()
        {
            Globals.spriteBatch.Draw(Texture, Position, Rectangles[FrameIndex], Color,
                Rotation, Origin, Scale, SpriteEffect, Depth);
        }

        /// <summary>Draws a specific frame (0-based).</summary>
        public void DrawFrame(int index)
        {
            if (index < 0 || index >= Rectangles.Length)
                return;

            Globals.spriteBatch.Draw(Texture, Position, Rectangles[index], Color,
                Rotation, Origin, Scale, SpriteEffect, Depth);
        }

        /// <summary>The source rectangle of the current frame in the texture.</summary>
        public Rectangle GetCurrentFrameRect() => Rectangles[FrameIndex];

        /// <summary>The source rectangle of a frame, or <see cref="Rectangle.Empty"/> if out of range.</summary>
        public Rectangle GetFrameRect(int index)
        {
            if (index < 0 || index >= Rectangles.Length)
                return Rectangle.Empty;

            return Rectangles[index];
        }

        /// <summary>Jumps to a frame (0-based).</summary>
        public void SetFrame(int frameIndex)
        {
            if (frameIndex >= 0 && frameIndex < Rectangles.Length)
                FrameIndex = frameIndex;
        }

        public bool IsAnimationFinished() => IsFinished;

        /// <summary>Goes back to the first frame and clears <see cref="IsFinished"/>.</summary>
        public void Reset()
        {
            FrameIndex = 0;
            TimeElapsed = 0f;
            IsFinished = false;
        }

        /// <summary>Plays backwards / forwards.</summary>
        public void ToggleReverse() => IsReversed = !IsReversed;

        public int GetTotalFrames() => Rectangles.Length;
    }
}
