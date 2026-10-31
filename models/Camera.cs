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
    /// <br /><br />
    /// Use <see cref="MoveTo"/> to direct the camera toward a <see cref="CameraFocus"/> point,
    /// and call <see cref="Update"/> each frame to drive the transition.
    /// <br /><br />
    /// The camera views the <see cref="Canvas"/> game layer, so its origin (the screen point
    /// the camera's <see cref="Position"/> is drawn at) is the center of the canvas, not the window.
    /// Use <see cref="SetBounds"/> to stop the view from showing anything outside the world.
    /// </remarks>
    public class Camera
    {
        private Vector2 position;
        private float zoom;
        private float rotation;
        private Vector2 origin;

        private CameraFocus activeFocus;
        private bool isFocusing;
        private Rectangle? bounds;

        // How close position/zoom need to be before a OneShot focus is considered "arrived"
        private const float ArrivalThreshold = 0.5f;
        private const float ZoomArrivalThreshold = 0.01f;

        /// <summary>
        /// Creates a camera that views the given canvas's game layer.
        /// </summary>
        /// <param name="canvas">The canvas the world is drawn on. Its size sets the camera's origin.</param>
        public Camera(Canvas canvas)
        {
            position = Vector2.Zero;
            zoom = 1f;
            rotation = 0f;
            origin = new Vector2(canvas.Width / 2f, canvas.Height / 2f);
        }

        public Vector2 Origin { get => origin; set => origin = value; }
        public Vector2 Position { get => position; set => position = value; }
        public float Zoom { get => zoom; set => zoom = value; }
        public float Rotation { get => rotation; set => rotation = value; }

        /// <summary>Whether the camera is currently moving toward or locked on a focus.</summary>
        public bool IsFocusing => isFocusing;

        /// <summary>The world area the view is kept inside, or null if unlimited.</summary>
        public Rectangle? Bounds => bounds;

        /// <summary>
        /// The world area currently on screen, in world pixels. Accounts for zoom and rotation
        /// (when rotated it's the box around the tilted view). Pass it to
        /// <see cref="Map.Draw(Rectangle)"/> to skip off-screen tiles.
        /// </summary>
        public Rectangle VisibleArea
        {
            get
            {
                // The canvas spans (0,0) to 2 * origin; map its corners back into the world
                Matrix toWorld = Matrix.Invert(Transform());
                Vector2 size = origin * 2f;
                Vector2 a = Vector2.Transform(Vector2.Zero, toWorld);
                Vector2 b = Vector2.Transform(new Vector2(size.X, 0), toWorld);
                Vector2 c = Vector2.Transform(new Vector2(0, size.Y), toWorld);
                Vector2 d = Vector2.Transform(size, toWorld);

                Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
                Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));

                int left = (int)System.MathF.Floor(min.X);
                int top = (int)System.MathF.Floor(min.Y);
                return new Rectangle(left, top,
                    (int)System.MathF.Ceiling(max.X) - left,
                    (int)System.MathF.Ceiling(max.Y) - top);
            }
        }

        /// <summary>
        /// Keeps the view inside the given world rectangle (e.g. <see cref="Map.PixelBounds"/>),
        /// so the camera stops following at the edges instead of showing what's outside.
        /// Pass null to remove the limit.
        /// </summary>
        /// <remarks>Assumes <see cref="Rotation"/> is 0; a rotated view can still show past the edges.</remarks>
        public void SetBounds(Rectangle? worldBounds)
        {
            bounds = worldBounds;
            position = ClampToBounds(position, zoom);
        }

        /// <summary>
        /// Directs the camera to move toward the given <see cref="CameraFocus"/>.
        /// Call <see cref="Update"/> each frame to drive the transition.
        /// </summary>
        /// <param name="focus">The focus point to move to.</param>
        public void MoveTo(CameraFocus focus)
        {
            activeFocus = focus;
            isFocusing = true;

            if (focus.Mode == FocusMode.Instant)
            {
                ApplyFocusInstant(focus);

                if (focus.Anchor == FocusAnchor.OneShot)
                    isFocusing = false;
            }
        }

        /// <summary>
        /// Releases the active focus, stopping any further tracking or transition.
        /// </summary>
        public void Release()
        {
            activeFocus = null;
            isFocusing = false;
        }

        /// <summary>
        /// Updates the camera's position and zoom toward the active focus each frame.
        /// Call this once per frame in your game's Update loop.
        /// </summary>
        /// <param name="gameTime">The current game time.</param>
        public void Update(GameTime gameTime)
        {
            if (isFocusing && activeFocus != null)
                UpdateFocus();

            // Runs every frame, so Position/Zoom set directly are kept in bounds too
            position = ClampToBounds(position, zoom);
        }

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

        public void Reset()
        {
            position = Vector2.Zero;
            zoom = 1f;
            Release();
        }

        // ─── Private helpers ────────────────────────────────────────────────

        private void UpdateFocus()
        {
            if (activeFocus.Mode == FocusMode.Instant)
            {
                // Persistent instant: keep snapping (e.g. the focus Position may be moving)
                ApplyFocusInstant(activeFocus);
                return;
            }

            // Aim for where the camera can actually reach, so a OneShot focus near
            // the edge still "arrives" instead of chasing an out-of-bounds point forever
            Vector2 target = ClampToBounds(activeFocus.Position, activeFocus.TargetZoom);

            // Smooth lerp
            float speed = activeFocus.TransitionSpeed;
            position = Vector2.Lerp(position, target, speed);
            zoom = MathHelper.Lerp(zoom, activeFocus.TargetZoom, speed);

            // For OneShot, release once close enough
            if (activeFocus.Anchor == FocusAnchor.OneShot)
            {
                bool posArrived = Vector2.Distance(position, target) < ArrivalThreshold;
                bool zoomArrived = System.MathF.Abs(zoom - activeFocus.TargetZoom) < ZoomArrivalThreshold;

                if (posArrived && zoomArrived)
                {
                    ApplyFocusInstant(activeFocus); // snap clean on arrival
                    isFocusing = false;
                    activeFocus = null;
                }
            }
        }

        /// <summary>
        /// Returns the closest camera position to <paramref name="point"/> whose view
        /// (at <paramref name="atZoom"/>) stays inside <see cref="Bounds"/>.
        /// </summary>
        private Vector2 ClampToBounds(Vector2 point, float atZoom)
        {
            if (bounds == null)
                return point;

            Rectangle b = bounds.Value;

            // How much of the world is visible either side of the camera's center
            Vector2 halfView = origin / atZoom;

            // If the world is smaller than the view on an axis, center it on that axis instead
            point.X = b.Width <= halfView.X * 2
                ? b.Center.X
                : MathHelper.Clamp(point.X, b.Left + halfView.X, b.Right - halfView.X);
            point.Y = b.Height <= halfView.Y * 2
                ? b.Center.Y
                : MathHelper.Clamp(point.Y, b.Top + halfView.Y, b.Bottom - halfView.Y);

            return point;
        }

        private void ApplyFocusInstant(CameraFocus focus)
        {
            zoom = focus.TargetZoom;
            position = ClampToBounds(focus.Position, zoom);
        }
    }
}