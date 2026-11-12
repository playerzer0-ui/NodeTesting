using Microsoft.Xna.Framework;

namespace NodeTesting.models
{
    /// <summary>
    /// Top-down draw order: things lower on screen (bigger Y) are closer to the camera and
    /// are drawn on top. Give every standing sprite a depth from its feet and let SpriteBatch sort.
    /// </summary>
    /// <example>
    /// <code>
    /// _spriteBatch.Begin(SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp,
    ///                    transformMatrix: camera.Transform());
    ///
    /// player.Depth = YSort.Depth(player.FeetY);
    /// player.Draw(Color.White);
    /// foreach (var tree in trees) tree.Draw();   // each tree set its Depth from its base
    ///
    /// _spriteBatch.End();   // sorted here: furthest back first, closest last
    /// </code>
    /// </example>
    public static class YSort
    {
        // Depth = 0.5 + y * Scale keeps a world from -1,000,000 to +1,000,000 pixels
        // inside SpriteBatch's 0..1 depth range, still telling apart Y values ~0.1px apart.
        private const float Scale = 0.5f / 1_000_000f;

        /// <summary>
        /// The depth for something standing at <paramref name="feetY"/> (world pixels).
        /// Bigger Y gives a bigger depth, so with <c>SpriteSortMode.FrontToBack</c> it's drawn later, on top.
        /// Works for any world size up to a million pixels in each direction; no setup needed.
        /// </summary>
        public static float Depth(float feetY) =>
            MathHelper.Clamp(0.5f + feetY * Scale, 0f, 1f);
    }
}
