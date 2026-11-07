using Microsoft.Xna.Framework;
using NodeTesting.models;

namespace NodeTesting
{
    /// <summary>A spinning 16x16 coin (12-frame strip).</summary>
    public class Coin
    {
        private readonly SpriteAnimation anim;

        /// <param name="cellBottom">Bottom-center of the cell, as given by ObjectMap.Spawn.</param>
        public Coin(Vector2 cellBottom)
        {
            anim = new SpriteAnimation("coin", 12, 12);   // 12 frames at 12 fps: one spin per second

            // The animation is drawn centered on its Position, so lift it half a tile
            // to sit in the middle of the cell
            anim.Position = cellBottom - new Vector2(0, 8);
        }

        public void Update(GameTime gameTime) => anim.Update(gameTime);

        public void Draw() => anim.Draw();
    }
}
