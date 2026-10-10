using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace NodeTesting.models;

/// <summary>
/// Represents a rendering canvas that uses an internal <see cref="RenderTarget2D"/> 
/// to render the game scene at a fixed resolution and scale it to fit the window.
/// </summary>
/// <remarks>
/// This class helps maintain a consistent aspect ratio and resolution across different 
/// screen sizes by rendering the game to an off-screen texture and then scaling it 
/// appropriately during display.
/// </remarks>
public class Canvas
{
    private readonly RenderTarget2D _target;
    private readonly GraphicsDevice _graphicsDevice;
    private Rectangle _destinationRectangle;
    private float _scale;

    // UI layer (Celeste-style hi-res HUD). Laid out in a fixed UiWidth x UiHeight
    // coordinate space, but rendered at max(UiWidth, on-screen size) so it is only
    // ever downscaled (sharp) and never stretched from a small texture (blurry).
    private RenderTarget2D _uiTarget;
    private Matrix _uiMatrix;
    private bool _uiDrawnThisFrame;

    /// <summary>Logical width of the UI coordinate space (default 1920).</summary>
    public int UiWidth { get; }

    /// <summary>Logical height of the UI coordinate space (default 1080).</summary>
    public int UiHeight { get; }

    /// <summary>Logical width of the game (low-res) layer.</summary>
    public int Width => _target.Width;

    /// <summary>Logical height of the game (low-res) layer.</summary>
    public int Height => _target.Height;

    /// <summary>
    /// Initializes a new instance of the <see cref="Canvas"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="width">The internal rendering width of the canvas.</param>
    /// <param name="height">The internal rendering height of the canvas.</param>
    /// <remarks>
    /// The specified width and height define the logical resolution of the game world. 
    /// The rendered image will later be scaled to fit the actual window or screen.
    /// UI is laid out in <paramref name="uiWidth"/> x <paramref name="uiHeight"/> space; author
    /// fonts and UI art at that size (e.g. a 48px SpriteFont for 1080p).
    /// </remarks>
    public Canvas(GraphicsDevice graphicsDevice, GameWindow window, int width, int height,
                  int uiWidth = 1920, int uiHeight = 1080)
    {
        _graphicsDevice = graphicsDevice;
        _target = new RenderTarget2D(_graphicsDevice, width, height);
        UiWidth = uiWidth;
        UiHeight = uiHeight;
        SetDestinationRectangle();

        window.ClientSizeChanged += (_, __) => SetDestinationRectangle();
    }

    /// <summary>
    /// Gets the internal render target texture for use in shaders.
    /// </summary>
    public Texture2D GetTexture()
    {
        return _target;
    }

    /// <summary>
    /// Calculates and sets the destination rectangle used to draw the canvas 
    /// on the screen while maintaining aspect ratio.
    /// </summary>
    /// <remarks>
    /// This method computes a scaled rectangle centered within the current 
    /// presentation bounds of the graphics device. It ensures the rendered 
    /// image is scaled to the largest possible size without distortion.
    /// </remarks>
    public void SetDestinationRectangle()
    {
        var screenSize = _graphicsDevice.PresentationParameters.Bounds;

        float scaleX = (float)screenSize.Width / _target.Width;
        float scaleY = (float)screenSize.Height / _target.Height;
        _scale = Math.Min(scaleX, scaleY);

        int newWidth = (int)MathF.Round(_target.Width * _scale);
        int newHeight = (int)MathF.Round(_target.Height * _scale);

        int posX = (screenSize.Width - newWidth) / 2;
        int posY = (screenSize.Height - newHeight) / 2;

        _destinationRectangle = new Rectangle(posX, posY, newWidth, newHeight);

        RebuildUiTarget();
    }

    /// <summary>
    /// (Re)creates the UI render target. It is never smaller than the logical UI size,
    /// so small windows get a downscaled 1080p HUD, and big windows get a HUD rendered
    /// at their native size (fonts/art scale up via the matrix, no texture stretching).
    /// </summary>
    private void RebuildUiTarget()
    {
        // UI uses the same aspect ratio as the game layer, so fit it to the same rect.
        float fit = Math.Min((float)_destinationRectangle.Width / UiWidth,
                             (float)_destinationRectangle.Height / UiHeight);
        float renderScale = Math.Max(1f, fit);

        int w = Math.Max(1, (int)MathF.Ceiling(UiWidth * renderScale));
        int h = Math.Max(1, (int)MathF.Ceiling(UiHeight * renderScale));

        if (_uiTarget == null || _uiTarget.Width != w || _uiTarget.Height != h)
        {
            _uiTarget?.Dispose();
            _uiTarget = new RenderTarget2D(_graphicsDevice, w, h);
        }

        _uiMatrix = Matrix.CreateScale(renderScale);
    }

    /// <summary>
    /// Converts a raw screen-space mouse position into virtual canvas space
    /// (the fixed 960x640 coordinate system the game is drawn in).
    ///
    /// This accounts for both the letterbox offset (black bars) and the scale
    /// factor, so the result is always correct regardless of window size.
    /// </summary>
    public Vector2 ScreenToCanvas(Vector2 screenPos)
    {
        return new Vector2(
            (screenPos.X - _destinationRectangle.X) / _scale,
            (screenPos.Y - _destinationRectangle.Y) / _scale
        );
    }

    /// <summary>
    /// Converts a raw screen-space position into UI space (the fixed UiWidth x UiHeight
    /// coordinates used between <see cref="BeginUI"/> and <see cref="EndUI"/>).
    /// </summary>
    public Vector2 ScreenToUI(Vector2 screenPos)
    {
        return new Vector2(
            (screenPos.X - _destinationRectangle.X) * UiWidth / _destinationRectangle.Width,
            (screenPos.Y - _destinationRectangle.Y) * UiHeight / _destinationRectangle.Height
        );
    }

    /// <summary>
    /// Converts a raw screen-space mouse position all the way into world space,
    /// applying both the canvas letterbox correction and the camera transform.
    ///
    /// Use this everywhere you need mouse-in-world coordinates. You will never
    /// have to think about scaling again.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPos, Matrix cameraTransform)
    {
        Vector2 canvasPos = ScreenToCanvas(screenPos);
        return Vector2.Transform(canvasPos, Matrix.Invert(cameraTransform));
    }

    /// <summary>Convenience overload that reads the mouse position for you.</summary>
    public Vector2 ScreenToWorld(Matrix cameraTransform)
    {
        MouseState mouse = Mouse.GetState();
        return ScreenToWorld(new Vector2(mouse.X, mouse.Y), cameraTransform);
    }

    /// <summary>
    /// Activates the canvas as the current render target, 
    /// redirecting all drawing to its internal <see cref="RenderTarget2D"/>.
    /// </summary>
    /// <remarks>
    /// Call this method before beginning your main game rendering logic.
    /// Once all drawing to the canvas is finished, use <see cref="Draw(SpriteBatch)"/> 
    /// to display the result on the screen.
    /// </remarks>
    public void Activate()
    {
        _graphicsDevice.SetRenderTarget(_target);
        _graphicsDevice.Clear(Color.DarkGray);
    }

    /// <summary>
    /// Starts the hi-res UI layer. Everything drawn until <see cref="EndUI"/> uses
    /// UiWidth x UiHeight coordinates (1920x1080 by default) and stays crisp at any
    /// window size. Use this for text, HUD and painted (non pixel-art) images.
    /// </summary>
    /// <remarks>
    /// Call after you've finished drawing the game layer and before <see cref="Draw"/>.
    /// Passing a <paramref name="transform"/> lets you add your own offset/scale on top.
    /// </remarks>
    public void BeginUI(SpriteBatch spriteBatch, Matrix? transform = null,
                        BlendState blendState = null, Effect effect = null)
    {
        _graphicsDevice.SetRenderTarget(_uiTarget);
        _graphicsDevice.Clear(Color.Transparent);
        _uiDrawnThisFrame = true;

        Matrix matrix = transform.HasValue ? transform.Value * _uiMatrix : _uiMatrix;
        spriteBatch.Begin(blendState: blendState ?? BlendState.AlphaBlend,
                          samplerState: SamplerState.LinearClamp,
                          effect: effect,
                          transformMatrix: matrix);
    }

    /// <summary>Ends the UI layer started by <see cref="BeginUI"/>.</summary>
    public void EndUI(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
    }

    /// <summary>
    /// Composites both layers to the back buffer: the game layer upscaled with
    /// point sampling (with an optional post-processing effect such as CRT),
    /// then the UI layer on top with linear sampling.
    /// </summary>
    /// <param name="spriteBatch">The <see cref="SpriteBatch"/> used to draw the canvas.</param>
    /// <param name="effect">Optional full-screen shader applied to the game layer only.</param>
    /// <remarks>The UI layer is only drawn if <see cref="BeginUI"/> was called this frame.</remarks>
    public void Draw(SpriteBatch spriteBatch, Effect effect = null)
    {
        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Clear(Color.Black);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: effect);
        spriteBatch.Draw(_target, _destinationRectangle, Color.White);
        spriteBatch.End();

        if (_uiDrawnThisFrame)
        {
            // UI target is premultiplied (SpriteBatch output), so AlphaBlend is correct.
            spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);
            spriteBatch.Draw(_uiTarget, _destinationRectangle, Color.White);
            spriteBatch.End();
            _uiDrawnThisFrame = false;
        }
    }

    /// <summary>
    /// Updates the preferred screen resolution and recalculates the canvas scaling.
    /// </summary>
    /// <param name="width">The desired screen width in pixels.</param>
    /// <param name="height">The desired screen height in pixels.</param>
    /// <remarks>
    /// This method modifies the game window resolution and ensures that 
    /// the canvas maintains its correct aspect ratio after the change.
    /// </remarks>
    public void SetResolution(int width, int height)
    {
        Globals.graphics.PreferredBackBufferWidth = width;
        Globals.graphics.PreferredBackBufferHeight = height;
        Globals.graphics.ApplyChanges();
        SetDestinationRectangle();
    }
}

