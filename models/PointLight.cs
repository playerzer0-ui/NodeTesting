using Microsoft.Xna.Framework;

namespace NodeTesting.models
{
    /// <summary>
    /// A round glow that's brightest at its center and fades to nothing at
    /// <see cref="Radius"/>. Add it to a <see cref="Lighting"/> to make it shine.
    /// </summary>
    public class PointLight
    {
        /// <summary>Center of the light, in world pixels. Move it to make the light follow something.</summary>
        public Vector2 Position;

        /// <summary>How far the light reaches, in world pixels.</summary>
        public float Radius;

        /// <summary>The light's color. White shows things in their true colors; warm or cool tints color them.</summary>
        public Color Color;

        /// <summary>Brightness from 0 (off) to 1 (full).</summary>
        public float Intensity;

        /// <summary>Set false to switch the light off without removing it.</summary>
        public bool Enabled = true;

        /// <summary>
        /// Whether occluders block this light. Turn it off for small glows (coins, sparks)
        /// that shouldn't cast shadows; it also makes them cheaper to draw.
        /// </summary>
        public bool CastsShadows = true;

        /// <param name="position">Center of the light, in world pixels.</param>
        /// <param name="radius">How far the light reaches, in world pixels.</param>
        /// <param name="color">The light's color. Defaults to white.</param>
        /// <param name="intensity">Brightness from 0 to 1.</param>
        public PointLight(Vector2 position, float radius, Color? color = null, float intensity = 1f)
        {
            Position = position;
            Radius = radius;
            Color = color ?? Color.White;
            Intensity = intensity;
        }
    }
}
