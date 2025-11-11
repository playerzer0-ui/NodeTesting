using Microsoft.Xna.Framework;


namespace NodeTesting.models
{
    /// <summary>
    /// Represents a 2D camera used to control the view transformation of the game world.
    /// </summary>
    /// <remarks>
    /// The camera allows you to pan, zoom, and rotate the visible area of the game world.
    /// Use the <see cref="Transform"/> method to obtain the transformation matrix 
    /// applied during sprite rendering.
    /// </remarks>
    public class Camera
    {
        private Vector2 position;
        private float zoom;
        private float rotation;
        private Vector2 origin;
        public Camera()
        {
            position = Vector2.Zero;
            zoom = 1f;
            rotation = 0f;
            origin = new Vector2(Globals.graphics.GraphicsDevice.Viewport.Width / 2, Globals.graphics.GraphicsDevice.Viewport.Height / 2);
        }
        public Vector2 Origin { get => origin; set => origin = value; }
        public Vector2 Position { get => position; set => position = value; }
        public float Zoom { get => zoom; set => zoom = value; }
        public float Rotation { get => rotation; set => rotation = value; }

        /// <summary>
        /// Creates and returns the transformation matrix used by the camera. <br />
        /// _spriteBatch.Begin(transformMatrix: camera.Transform());
        /// </summary>
        /// <returns>
        /// A <see cref="Matrix"/> that applies translation, rotation, scaling, and origin offset
        /// in the correct order for rendering the game world from the camera's perspective.
        /// </returns>
        public Matrix Transform()
        {
            return Matrix.CreateTranslation(new Vector3(-position, 0f)) *
                   Matrix.CreateRotationZ(rotation) *
                   Matrix.CreateScale(zoom) *
                   Matrix.CreateTranslation(new Vector3(origin, 0f));
        }
    }
}
