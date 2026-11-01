using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace NodeTesting.models
{
    /// <summary>
    /// Represents a series of waypoints that a sprite can travel along.
    /// Supports looping, reversing, speed control, and per-waypoint delays.
    /// </summary>
    public class Path2D
    {
        private List<Vector2> waypoints = new List<Vector2>();
        private List<float> waypointDelays = new List<float>();

        private int currentIndex = 0;
        private float speed = 100f;           // pixels per second
        private float delayTimer = 0f;
        private bool isWaiting = false;

        public bool IsLooping = false;
        public bool IsPingPong = false;
        public bool IsReversed = false;
        public bool IsFinished = false;
        public bool IsActive = true;

        /// <summary>
        /// The current world position along the path.
        /// Assign this to your sprite's position each frame.
        /// </summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// Movement speed in pixels per second.
        /// </summary>
        public float Speed
        {
            get => speed;
            set => speed = MathHelper.Max(0f, value);
        }

        /// <summary>
        /// The index of the waypoint currently being moved toward.
        /// </summary>
        public int CurrentWaypointIndex => currentIndex;

        /// <summary>
        /// Creates an empty path. Add waypoints with <see cref="AddWaypoint"/>.
        /// </summary>
        public Path2D() { }

        /// <summary>
        /// Creates a path from an existing list of waypoints with no per-waypoint delays.
        /// The sprite will start at the first waypoint immediately.
        /// </summary>
        /// <param name="waypoints">Ordered list of positions to travel through.</param>
        /// <param name="speed">Movement speed in pixels per second.</param>
        public Path2D(List<Vector2> waypoints, float speed = 100f)
        {
            this.speed = speed;
            foreach (var wp in waypoints)
                AddWaypoint(wp, 0f);

            if (this.waypoints.Count > 0)
                Position = this.waypoints[0];
        }

        /// <summary>
        /// Appends a waypoint to the end of the path.
        /// </summary>
        /// <param name="point">The position to add.</param>
        /// <param name="delay">
        /// How many seconds to pause at this waypoint before continuing.
        /// Use 0 for no delay.
        /// </param>
        public void AddWaypoint(Vector2 point, float delay = 0f)
        {
            waypoints.Add(point);
            waypointDelays.Add(MathHelper.Max(0f, delay));

            // Initialise position to the very first waypoint added
            if (waypoints.Count == 1)
                Position = point;
        }

        // Roughly how far apart (in pixels) the points that make up a curve are
        private const float CurveStepPixels = 2f;

        /// <summary>
        /// Adds a curve from the last waypoint to <paramref name="end"/>, bent toward
        /// <paramref name="control"/> (a quadratic Bezier). The path doesn't pass through
        /// the control point; it's pulled toward it.
        /// </summary>
        /// <param name="control">The point the curve bends toward.</param>
        /// <param name="end">Where the curve ends. Becomes a normal waypoint.</param>
        /// <param name="delay">Seconds to pause at <paramref name="end"/>.</param>
        /// <exception cref="InvalidOperationException">If there's no waypoint to start the curve from.</exception>
        public void AddCurve(Vector2 control, Vector2 end, float delay = 0f)
        {
            Vector2 start = CurveStart();
            float roughLength = Vector2.Distance(start, control) + Vector2.Distance(control, end);

            AddCurvePoints(roughLength, end, delay, t =>
            {
                float u = 1f - t;
                return u * u * start + 2f * u * t * control + t * t * end;
            });
        }

        /// <summary>
        /// Adds a curve from the last waypoint to <paramref name="end"/>, shaped by two
        /// control points (a cubic Bezier). The start of the curve heads toward
        /// <paramref name="control1"/> and the end arrives from <paramref name="control2"/>,
        /// which allows S-shapes and close approximations of circles.
        /// </summary>
        /// <param name="control1">Shapes the start of the curve.</param>
        /// <param name="control2">Shapes the end of the curve.</param>
        /// <param name="end">Where the curve ends. Becomes a normal waypoint.</param>
        /// <param name="delay">Seconds to pause at <paramref name="end"/>.</param>
        /// <exception cref="InvalidOperationException">If there's no waypoint to start the curve from.</exception>
        public void AddCurve(Vector2 control1, Vector2 control2, Vector2 end, float delay = 0f)
        {
            Vector2 start = CurveStart();
            float roughLength = Vector2.Distance(start, control1)
                              + Vector2.Distance(control1, control2)
                              + Vector2.Distance(control2, end);

            AddCurvePoints(roughLength, end, delay, t =>
            {
                float u = 1f - t;
                return u * u * u * start
                     + 3f * u * u * t * control1
                     + 3f * u * t * t * control2
                     + t * t * t * end;
            });
        }

        private Vector2 CurveStart()
        {
            if (waypoints.Count == 0)
                throw new System.InvalidOperationException(
                    "Add a starting waypoint with AddWaypoint before adding a curve.");
            return waypoints[^1];
        }

        /// <summary>
        /// Turns a curve into a chain of short straight steps. Update carries leftover
        /// movement from one step to the next, so speed stays exactly <see cref="Speed"/>.
        /// </summary>
        private void AddCurvePoints(float roughLength, Vector2 end, float delay,
                                    System.Func<float, Vector2> pointAt)
        {
            // The control-point outline is always at least as long as the curve,
            // so this gives steps of about CurveStepPixels or less
            int steps = (int)MathHelper.Clamp(
                (float)System.Math.Ceiling(roughLength / CurveStepPixels), 4, 1000);

            for (int i = 1; i < steps; i++)
                AddWaypoint(pointAt((float)i / steps));

            // Land exactly on the end point, with its pause
            AddWaypoint(end, delay);
        }

        /// <summary>
        /// Removes all waypoints and resets path state.
        /// </summary>
        public void Clear()
        {
            waypoints.Clear();
            waypointDelays.Clear();
            currentIndex = 0;
            delayTimer = 0f;
            isWaiting = false;
            IsFinished = false;
        }

        /// <summary>
        /// Reverses the order of waypoints in place and turns around immediately,
        /// heading back to the waypoint it just came from. No visible jump.
        /// </summary>
        public void ReversePath()
        {
            if (waypoints.Count == 0) return;

            // Mirror the current index so it still points at the same physical waypoint
            currentIndex = (waypoints.Count - 1) - currentIndex;

            waypoints.Reverse();
            waypointDelays.Reverse();

            TurnAround();
        }

        /// <summary>
        /// Flips the travel direction without reordering the waypoints and turns around
        /// immediately, heading back to the waypoint it just came from.
        /// </summary>
        public void ReverseDirection()
        {
            IsReversed = !IsReversed;
            TurnAround();
        }

        /// <summary>
        /// Puts the path back at its first waypoint, travelling forwards, without clearing waypoints.
        /// </summary>
        public void Reset()
        {
            currentIndex = 0;
            IsReversed = false;
            delayTimer = 0f;
            isWaiting = false;
            IsFinished = false;

            if (waypoints.Count > 0)
                Position = waypoints[0];
        }

        /// <summary>
        /// Advances the path each frame. Call this in your game's Update loop,
        /// then apply <see cref="Position"/> to your sprite.
        /// </summary>
        /// <param name="gameTime">The current <see cref="GameTime"/> from MonoGame.</param>
        public void Update(GameTime gameTime)
        {
            if (!IsActive || IsFinished || waypoints.Count < 2)
                return;

            // Time this frame still has to spend. Reaching a waypoint doesn't end the frame:
            // the leftover time keeps moving toward the next one (or counts toward a pause),
            // so the path always travels at Speed, however close together the waypoints are.
            float timeLeft = (float)gameTime.ElapsedGameTime.TotalSeconds;
            int zeroLengthHops = 0;

            while (timeLeft > 0f && !IsFinished)
            {
                // ── Pausing at a waypoint ──────────────────────────────────────
                if (isWaiting)
                {
                    if (delayTimer > timeLeft)
                    {
                        delayTimer -= timeLeft;
                        return;
                    }

                    // Pause ends partway through this frame; move on with what's left
                    timeLeft -= delayTimer;
                    delayTimer = 0f;
                    isWaiting = false;
                    AdvanceIndex();
                    continue;
                }

                // ── Moving toward the current target waypoint ──────────────────
                Vector2 target = waypoints[currentIndex];
                Vector2 direction = target - Position;
                float distanceToTarget = direction.Length();
                float reachableDistance = speed * timeLeft;

                if (reachableDistance < distanceToTarget)
                {
                    // Won't get there this frame
                    direction.Normalize();
                    Position += direction * reachableDistance;
                    return;
                }

                // Reaches the waypoint, with time to spare
                Position = target;
                timeLeft -= speed > 0f ? distanceToTarget / speed : 0f;

                // Stop if it's only hopping between waypoints in the same spot
                // (e.g. every waypoint identical), which would otherwise never use up time
                zeroLengthHops = distanceToTarget > 0f ? 0 : zeroLengthHops + 1;
                if (zeroLengthHops > waypoints.Count)
                    return;

                float delay = waypointDelays[currentIndex];
                if (delay > 0f)
                {
                    // Pause here; the rest of this frame counts toward the pause
                    isWaiting = true;
                    delayTimer = delay;
                }
                else
                {
                    AdvanceIndex();
                }
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Called right after the travel direction flips. The target is still the waypoint
        /// ahead in the old direction, so retarget to the next waypoint in the new direction,
        /// which is the one it just came from.
        /// </summary>
        private void TurnAround()
        {
            IsFinished = false;
            int count = waypoints.Count;
            if (count < 2)
                return;

            // Pausing on a waypoint: when the pause ends, AdvanceIndex already
            // moves on in the new direction, so nothing to change
            if (isWaiting)
                return;

            int next = IsReversed ? currentIndex - 1 : currentIndex + 1;
            if (next < 0 || next >= count)
            {
                // Only a looping path wraps around; otherwise keep the current target
                // (it's sitting on the start, so it moves on from there next Update)
                if (!IsLooping || IsPingPong)
                    return;
                next = (next + count) % count;
            }
            currentIndex = next;

            // Already standing on that waypoint (e.g. it was just reached): head past it
            if (Position == waypoints[currentIndex])
                AdvanceIndex();
        }

        /// <summary>
        /// Moves currentIndex to the next waypoint, handling looping, ping-pong, and end-of-path.
        /// </summary>
        private void AdvanceIndex()
        {
            if (!IsReversed)
            {
                if (currentIndex < waypoints.Count - 1)
                {
                    currentIndex++;
                }
                else if (IsPingPong)
                {
                    IsReversed = true;
                    currentIndex--;
                }
                else if (IsLooping)
                {
                    currentIndex = 0;
                }
                else
                {
                    IsFinished = true;
                }
            }
            else
            {
                if (currentIndex > 0)
                {
                    currentIndex--;
                }
                else if (IsPingPong)
                {
                    IsReversed = false;
                    currentIndex++;
                }
                else if (IsLooping)
                {
                    currentIndex = waypoints.Count - 1;
                }
                else
                {
                    IsFinished = true;
                }
            }
        }
    }
}