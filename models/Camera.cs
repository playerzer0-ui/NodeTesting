using Microsoft.Xna.Framework;


namespace NodeTesting.models
{
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

        public Matrix Transform()
        {
            return Matrix.CreateTranslation(new Vector3(-position, 0f)) *
                   Matrix.CreateRotationZ(rotation) *
                   Matrix.CreateScale(zoom) *
                   Matrix.CreateTranslation(new Vector3(origin, 0f));
        }
    }
}
