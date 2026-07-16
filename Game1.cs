using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NodeTesting.models;
using System;

namespace NodeTesting
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont font;
        private Song song;

        Player player;
        Bubble bubble;
        Platform platform;
        CollisionCircle circle;
        CollisionRect rectangle;
        CollisionRect stepHere;

        CollisionRect WallRect;
        CollisionCircle WallCircle;
        Effect monochromeShader;
        Effect CRT;
        Effect rain;
        ICollider[] walls;

        Camera camera;
        Canvas canvas;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Window.AllowUserResizing= true;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Globals.Content = Content;
            Globals.spriteBatch = _spriteBatch;
            Globals.graphics = _graphics;
            // TODO: use this.Content to load your game content here

            player = new Player("player", new Vector2(300, 300));
            bubble = new Bubble(400, 10);
            platform = new Platform();
            circle = new CollisionCircle(600, 300, 20);
            font = Content.Load<SpriteFont>("File");
            rectangle = new CollisionRect(500, 100, 200, 200);
            stepHere = new CollisionRect(100, 200, 200, 200);
            camera = new Camera();
            canvas = new Canvas(_graphics.GraphicsDevice, Window, 1280, 720);

            WallRect = new CollisionRect(500, 500, 100, 100);
            WallCircle = new CollisionCircle(600, 500, 50);
            WallRect.IsStatic = true;
            WallCircle.IsStatic = true;

            //shaders
            monochromeShader = Content.Load<Effect>("FileX");
            CRT = Content.Load<Effect>("CRT_TV");
            rain = Content.Load<Effect>("Rain");

            //sounds
            song = Content.Load<Song>("sounds/nature");

            //controls
            Globals.Input.Register("MoveUp", Keys.W, Buttons.DPadUp);
            Globals.Input.Register("MoveDown", Keys.S, Buttons.DPadDown);
            Globals.Input.Register("MoveLeft", Keys.A, Buttons.DPadLeft);
            Globals.Input.Register("MoveRight", Keys.D, Buttons.DPadRight);


        }

        protected override void Update(GameTime gameTime)
        {
            Globals.Input.Update();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            Vector2 viewportCenter = new Vector2(
            Globals.graphics.GraphicsDevice.Viewport.Width / 2f,
            Globals.graphics.GraphicsDevice.Viewport.Height / 2f);

            walls = new ICollider[]
            {
                WallRect,   // IsStatic = true
                WallCircle  // IsStatic = true
            };

            // TODO: Add your update logic here
            //canvas.SetResolution(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);

            if (player.Rect.Intersects(stepHere))
            {
                camera.MoveTo(new CameraFocus(rectangle.Center, targetZoom: 1f, transitionSpeed: 0.08f));
            }
            else if (player.Rect.Intersects(circle))
            {
                camera.MoveTo(new CameraFocus(bubble.Circle.Center, targetZoom: 1f, transitionSpeed: 0.3f));
            }
            else
            {
                camera.MoveTo(new CameraFocus(player.Pos, targetZoom: 1f, anchor: FocusAnchor.Persistent));
            }


            platform.Update(gameTime);
            player.Update(gameTime, walls);
            bubble.Update(gameTime);
            camera.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // ── Pass 1: draw everything cleanly onto the canvas ───────────────────
            canvas.Activate();
            _spriteBatch.Begin(transformMatrix: camera.Transform()); // no shader here
            player.Draw(Color.White);
            player.Rect.Draw(new Color(255, 0, 0, 128));
            stepHere.Draw(new Color(0, 0, 255, 128));
            rectangle.Draw(Color.White);
            bubble.Draw();
            circle.Draw(Color.White);
            foreach (ICollider wall in walls)
                wall.Draw(Color.Purple);
            platform.Draw();
            _spriteBatch.DrawString(font, "distance: " + Vector2.Distance(circle.Center, player.Rect.Pos), new Vector2(10, 10), Color.White);
            _spriteBatch.DrawString(font, "left: " + player.Rect.Rect.Left, new Vector2(10, 30), Color.White);
            _spriteBatch.End();

            // -- Pass 2: draw the canvas to the screen with CRT applied ----------
            //CRT.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
            //CRT.Parameters["Resolution"].SetValue(new Vector2(1280, 720));
            //CRT.Parameters["CurvatureAmount"].SetValue(0.5f);
            //CRT.Parameters["ScanlineStrength"].SetValue(0.2f);
            //CRT.Parameters["VignetteStrength"].SetValue(0.5f);
            //CRT.Parameters["AberrationAmount"].SetValue(0.004f);
            //CRT.Parameters["NoiseStrength"].SetValue(0.03f);
            //canvas.Draw(_spriteBatch); // apply CRT to the whole canvas at once

            // In your Draw method - use these values:
            rain.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
            rain.Parameters["Resolution"].SetValue(new Vector2(1280, 720));
            rain.Parameters["RainSpeed"].SetValue(3.0f);     // Much faster! Try 2.0-5.0
            rain.Parameters["RainDensity"].SetValue(25f);
            
            rain.Parameters["DropLength"].SetValue(0.2f);    // Longer drops
            rain.Parameters["DropWidth"].SetValue(0.02f);
            rain.Parameters["RainOpacity"].SetValue(0.8f);
            rain.Parameters["RippleStrength"].SetValue(0.3f);

            canvas.Draw(_spriteBatch, rain);

            base.Draw(gameTime);
        }
    }
}