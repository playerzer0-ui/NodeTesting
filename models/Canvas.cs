using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="Canvas"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="width">The internal rendering width of the canvas.</param>
    /// <param name="height">The internal rendering height of the canvas.</param>
    /// <remarks>
    /// The specified width and height define the logical resolution of the game world. 
    /// The rendered image will later be scaled to fit the actual window or screen.
    /// </remarks>
    public Canvas(GraphicsDevice graphicsDevice, int width, int height)
    {
        _graphicsDevice = graphicsDevice;
        _target = new RenderTarget2D(_graphicsDevice, width, height);
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
        float scale = Math.Min(scaleX, scaleY);

        int newWidth = (int)(_target.Width * scale);
        int newHeight = (int)(_target.Height * scale);
        int posX = (screenSize.Width - newWidth) / 2;
        int posY = (screenSize.Height - newHeight) / 2;

        _destinationRectangle = new Rectangle(posX, posY, newWidth, newHeight);
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
    /// Draws the contents of the canvas to the back buffer (screen).
    /// </summary>
    /// <param name="spriteBatch">The <see cref="SpriteBatch"/> used to draw the canvas.</param>
    /// <remarks>
    /// This method ends rendering to the off-screen target and begins drawing it 
    /// to the actual display surface, scaled according to the destination rectangle.
    /// </remarks>
    public void Draw(SpriteBatch spriteBatch)
    {
        _graphicsDevice.SetRenderTarget(null);
        _graphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();
        spriteBatch.Draw(_target, _destinationRectangle, Color.White);
        spriteBatch.End();
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

