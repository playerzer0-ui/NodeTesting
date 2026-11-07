using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace NodeTesting.models
{
    /// <summary>
    /// 2D lighting for the <see cref="Canvas"/> game layer. The world is darkened to
    /// <see cref="Ambient"/>, each <see cref="PointLight"/> brings back the area around it,
    /// and occluders (walls, trunks...) block light and cast shadows behind them.
    /// The UI layer is never affected.
    /// </summary>
    /// <remarks>
    /// How it works: every frame a "light map" the size of the canvas is filled with the
    /// ambient color, each light adds a soft glow onto it (minus the shadows of any occluders
    /// in its range), and the game layer is then multiplied by it. Black in the light map
    /// makes the world black; white leaves it unchanged.
    /// </remarks>
    /// <example>
    /// <code>
    /// lighting = new Lighting(canvas, GraphicsDevice) { Ambient = new Color(25, 25, 45) };
    /// var torch = lighting.Add(new PointLight(new Vector2(200, 150), radius: 120, Color.Orange));
    /// lighting.AddOccluder(wall);   // any CollisionRect or CollisionCircle
    ///
    /// // Draw: after the world's SpriteBatch.End(), before canvas.BeginUI(...)
    /// lighting.Draw(_spriteBatch, camera.Transform());
    /// </code>
    /// </example>
    public class Lighting
    {
        private const int GlowTextureSize = 256;

        // Circles are treated as polygons with this many sides when casting shadows
        private const int CircleShadowSides = 16;

        // How far shadows are stretched away from the light: far past any light's reach
        private const float ShadowLength = 100000f;

        private readonly GraphicsDevice graphicsDevice;
        private readonly RenderTarget2D lightMap;
        private readonly RenderTarget2D singleLight;   // one shadowed light at a time
        private readonly Texture2D glow;
        private readonly BasicEffect shadowEffect;
        private readonly List<VertexPositionColor> shadowVertices = new List<VertexPositionColor>();
        private readonly List<Vector2> polygon = new List<Vector2>();

        // Lights add their glow on top of each other
        private static readonly BlendState AddLight = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        // Marks occluder areas in the stencil buffer without drawing any color
        private static readonly BlendState NoColor = new BlendState { ColorWriteChannels = ColorWriteChannels.None };

        private static readonly DepthStencilState MarkOccluders = new DepthStencilState
        {
            DepthBufferEnable = false,
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1,
        };

        // Shadows only land where no occluder was marked
        private static readonly DepthStencilState OutsideOccluders = new DepthStencilState
        {
            DepthBufferEnable = false,
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 0,
        };

        // Result = world color × light map color (alpha left as it was)
        private static readonly BlendState Multiply = new BlendState
        {
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,
        };

        /// <summary>
        /// The light level where no light reaches. <see cref="Color.Black"/> is pitch dark,
        /// <see cref="Color.White"/> is no darkness at all. A dark blue like (25, 25, 45) reads as night.
        /// </summary>
        public Color Ambient = new Color(25, 25, 45);

        /// <summary>Set false to turn all lighting off (the world is drawn fully lit).</summary>
        public bool Enabled = true;

        /// <summary>All lights. You can also add and remove lights here directly.</summary>
        public List<PointLight> Lights { get; } = new List<PointLight>();

        /// <summary>
        /// Everything that blocks light. <see cref="CollisionRect"/> and <see cref="CollisionCircle"/>
        /// are supported; they're read every frame, so moving one moves its shadow.
        /// </summary>
        public List<ICollider> Occluders { get; } = new List<ICollider>();

        /// <param name="canvas">The canvas whose game layer is lit. The light map matches its size.</param>
        /// <param name="graphicsDevice">The game's graphics device.</param>
        public Lighting(Canvas canvas, GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;

            // PreserveContents: the light map is switched away from and back to once per shadowed light
            lightMap = new RenderTarget2D(graphicsDevice, canvas.Width, canvas.Height, false,
                                          SurfaceFormat.Color, DepthFormat.None, 0,
                                          RenderTargetUsage.PreserveContents);
            // Has a stencil buffer: occluders are marked in it so shadows never darken them
            singleLight = new RenderTarget2D(graphicsDevice, canvas.Width, canvas.Height, false,
                                             SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            glow = CreateGlowTexture(graphicsDevice, GlowTextureSize);

            shadowEffect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true,
                Projection = Matrix.CreateOrthographicOffCenter(0, canvas.Width, canvas.Height, 0, 0, 1),
            };
        }

        /// <summary>Adds a light and returns it, so you can keep it to move or change later.</summary>
        public PointLight Add(PointLight light)
        {
            Lights.Add(light);
            return light;
        }

        /// <summary>Removes a light.</summary>
        public void Remove(PointLight light) => Lights.Remove(light);

        /// <summary>
        /// Makes a collider block light (e.g. a wall, a tree trunk). The collider itself
        /// stays lit; the shadow falls behind it, on the side away from each light.
        /// </summary>
        public void AddOccluder(ICollider occluder) => Occluders.Add(occluder);

        /// <summary>Adds several occluders at once, e.g. <c>collisionMap.GetCollisionRects()</c>.</summary>
        public void AddOccluders(IEnumerable<ICollider> occluders) => Occluders.AddRange(occluders);

        /// <summary>Stops a collider from blocking light.</summary>
        public void RemoveOccluder(ICollider occluder) => Occluders.Remove(occluder);

        /// <summary>
        /// Darkens the game layer and applies every light and shadow. Call it while the canvas
        /// is active, after the world's <c>SpriteBatch.End()</c> and before <see cref="Canvas.BeginUI"/>.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch (must not be between Begin and End).</param>
        /// <param name="cameraTransform">The same camera matrix the world was drawn with.</param>
        public void Draw(SpriteBatch spriteBatch, Matrix cameraTransform)
        {
            if (!Enabled)
                return;

            RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();

            // 1. Ambient, plus every light with nothing in its way, in one batch
            graphicsDevice.SetRenderTarget(lightMap);
            graphicsDevice.Clear(Ambient);

            var shadowedLights = new List<PointLight>();
            spriteBatch.Begin(blendState: AddLight, samplerState: SamplerState.LinearClamp,
                              transformMatrix: cameraTransform);
            foreach (PointLight light in Lights)
            {
                if (!IsVisible(light))
                    continue;

                if (light.CastsShadows && HasOccluderInRange(light))
                    shadowedLights.Add(light);
                else
                    DrawGlow(spriteBatch, light);
            }
            spriteBatch.End();

            // 2. Each light with something in its way: glow, cut out the shadows, then add it
            foreach (PointLight light in shadowedLights)
            {
                graphicsDevice.SetRenderTarget(singleLight);
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.Stencil, Color.Black, 0f, 0);

                spriteBatch.Begin(blendState: AddLight, samplerState: SamplerState.LinearClamp,
                                  transformMatrix: cameraTransform);
                DrawGlow(spriteBatch, light);
                spriteBatch.End();

                DrawShadows(light, cameraTransform);

                graphicsDevice.SetRenderTarget(lightMap);
                spriteBatch.Begin(blendState: AddLight, samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(singleLight, Vector2.Zero, Color.White);
                spriteBatch.End();
            }

            // 3. Back to the game layer, and multiply it by the light map
            graphicsDevice.SetRenderTargets(previousTargets);

            spriteBatch.Begin(blendState: Multiply, samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(lightMap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static bool IsVisible(PointLight light) =>
            light.Enabled && light.Radius > 0f && light.Intensity > 0f;

        private void DrawGlow(SpriteBatch spriteBatch, PointLight light)
        {
            float scale = light.Radius * 2f / GlowTextureSize;
            Color tint = light.Color * MathHelper.Clamp(light.Intensity, 0f, 1f);
            spriteBatch.Draw(glow, light.Position, null, tint, 0f, new Vector2(GlowTextureSize / 2f),
                             scale, SpriteEffects.None, 0f);
        }

        private bool HasOccluderInRange(PointLight light)
        {
            foreach (ICollider occluder in Occluders)
                if (IsInRange(occluder, light))
                    return true;
            return false;
        }

        private static bool IsInRange(ICollider occluder, PointLight light)
        {
            switch (occluder)
            {
                case CollisionRect r:
                    // Closest point of the rect to the light
                    float cx = MathHelper.Clamp(light.Position.X, r.Rect.Left, r.Rect.Right);
                    float cy = MathHelper.Clamp(light.Position.Y, r.Rect.Top, r.Rect.Bottom);
                    return Vector2.DistanceSquared(light.Position, new Vector2(cx, cy)) < light.Radius * light.Radius;
                case CollisionCircle c:
                    float reach = light.Radius + c.Radius;
                    return Vector2.DistanceSquared(light.Position, c.Center) < reach * reach;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Paints black over everything behind each occluder in range of <paramref name="light"/>.
        /// Each edge facing away from the light is stretched away from it into a long band;
        /// together the bands cover the occluder's whole shadow. Occluders themselves are
        /// masked out first, so a shadow never darkens a wall, even a neighboring one
        /// (e.g. the two rectangles of an L-shaped wall).
        /// </summary>
        private void DrawShadows(PointLight light, Matrix cameraTransform)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            shadowEffect.View = cameraTransform;

            // 1. Mark every occluder in range in the stencil buffer
            shadowVertices.Clear();
            foreach (ICollider occluder in Occluders)
            {
                if (!IsInRange(occluder, light) || !GetOutline(occluder))
                    continue;

                // The outlines are convex, so a fan of triangles fills them
                for (int i = 1; i < polygon.Count - 1; i++)
                    AddTriangle(polygon[0], polygon[i], polygon[i + 1]);
            }
            DrawTriangles(NoColor, MarkOccluders);

            // 2. Shadow bands, only where no occluder was marked
            shadowVertices.Clear();
            foreach (ICollider occluder in Occluders)
            {
                if (!IsInRange(occluder, light) || !GetOutline(occluder))
                    continue;

                for (int i = 0; i < polygon.Count; i++)
                {
                    Vector2 a = polygon[i];
                    Vector2 b = polygon[(i + 1) % polygon.Count];

                    // Outline points go clockwise on screen, so this normal points outward
                    Vector2 edge = b - a;
                    Vector2 outward = new Vector2(edge.Y, -edge.X);

                    // Only edges facing away from the light cast a shadow,
                    // so the occluder's own lit side stays lit
                    if (Vector2.Dot(outward, a - light.Position) <= 0f)
                        continue;

                    Vector2 aFar = a + SafeNormalize(a - light.Position) * ShadowLength;
                    Vector2 bFar = b + SafeNormalize(b - light.Position) * ShadowLength;

                    AddTriangle(a, b, bFar);
                    AddTriangle(a, bFar, aFar);
                }
            }

            DrawTriangles(BlendState.Opaque, OutsideOccluders);
        }

        private void DrawTriangles(BlendState blendState, DepthStencilState depthStencilState)
        {
            if (shadowVertices.Count == 0)
                return;

            graphicsDevice.BlendState = blendState;
            graphicsDevice.DepthStencilState = depthStencilState;

            VertexPositionColor[] vertices = shadowVertices.ToArray();
            foreach (EffectPass pass in shadowEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }
        }

        /// <summary>Fills <see cref="polygon"/> with the occluder's outline, clockwise on screen.</summary>
        private bool GetOutline(ICollider occluder)
        {
            polygon.Clear();

            switch (occluder)
            {
                case CollisionRect r:
                    Rectangle rect = r.Rect;
                    polygon.Add(new Vector2(rect.Left, rect.Top));
                    polygon.Add(new Vector2(rect.Right, rect.Top));
                    polygon.Add(new Vector2(rect.Right, rect.Bottom));
                    polygon.Add(new Vector2(rect.Left, rect.Bottom));
                    return true;

                case CollisionCircle c:
                    // Increasing angle with Y pointing down goes clockwise on screen
                    for (int i = 0; i < CircleShadowSides; i++)
                    {
                        float angle = i * MathHelper.TwoPi / CircleShadowSides;
                        polygon.Add(c.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * c.Radius);
                    }
                    return true;

                default:
                    return false;
            }
        }

        private void AddTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            shadowVertices.Add(new VertexPositionColor(new Vector3(a, 0f), Color.Black));
            shadowVertices.Add(new VertexPositionColor(new Vector3(b, 0f), Color.Black));
            shadowVertices.Add(new VertexPositionColor(new Vector3(c, 0f), Color.Black));
        }

        private static Vector2 SafeNormalize(Vector2 v)
        {
            float length = v.Length();
            return length > 0.0001f ? v / length : Vector2.UnitY;
        }

        /// <summary>
        /// A white circle that's full brightness in the middle and fades smoothly to black at the edge.
        /// </summary>
        private static Texture2D CreateGlowTexture(GraphicsDevice graphicsDevice, int size)
        {
            var pixels = new Color[size * size];
            float center = (size - 1) / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center)) / center;
                    float falloff = MathHelper.Clamp(1f - distance, 0f, 1f);
                    falloff *= falloff;   // squared: bright core, soft long fade

                    pixels[y * size + x] = new Color(falloff, falloff, falloff, falloff);
                }
            }

            var texture = new Texture2D(graphicsDevice, size, size);
            texture.SetData(pixels);
            return texture;
        }
    }
}
