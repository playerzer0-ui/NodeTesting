using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using NodeTesting.models;
using System;
using System.Collections.Generic;

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
        TileMap tilemap;

        SpriteAnimation spriteAnimation;
        Camera camera;
        Canvas canvas;

        Path2D fewPoints;
        Path2D manyPoints;
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
            canvas = new Canvas(_graphics.GraphicsDevice, Window, 1280, 720);
            camera = new Camera(canvas);

            spriteAnimation = new SpriteAnimation("singlesprite", 4, 8);


            tilemap = new TileMap("tilemap", 16, 16, "tilemap.csv");
            tilemap.RegisterAnimation(20, new int[] { 20, 21, 22, 23 }, 0.15f);
            tilemap.RegisterAnimation(28, new int[] { 28, 29, 30, 31 }, 0.15f);
            camera.SetBounds(tilemap.PixelBounds);

            WallRect = new CollisionRect(500, 500, 100, 100);
            WallCircle = new CollisionCircle(600, 500, 50);
            WallRect.IsStatic = true;
            WallCircle.IsStatic = true;

            // Red: just a start and an end
            fewPoints = new Path2D(new List<Vector2> { new(100, 200), new(700, 200) }, speed: 600);

            // Yellow: the same line, with a waypoint every 4 pixels
            var dense = new List<Vector2>();
            for (int x = 100; x <= 700; x += 4)
                dense.Add(new Vector2(x, 240));
            manyPoints = new Path2D(dense, speed: 600);


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

            walls = new ICollider[]
            {
                WallRect,   // IsStatic = true
                WallCircle  // IsStatic = true
            };

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

            spriteAnimation.Update(gameTime);

            fewPoints.Update(gameTime);
            manyPoints.Update(gameTime);

            // Press R to restart the race
            if (Globals.Input.KeyJustDown(Keys.R))
            {
                fewPoints.Reset();
                manyPoints.Reset();
            }

            tilemap.Update(gameTime);
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
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.Transform());

            tilemap.Draw(camera.VisibleArea);
            player.Draw(Color.White);
            player.Rect.Draw(new Color(255, 0, 0, 128));
            stepHere.Draw(new Color(0, 0, 255, 128));
            rectangle.Draw(Color.White);
            bubble.Draw();
            circle.Draw(Color.White);
            foreach (ICollider wall in walls)
                wall.Draw(Color.Purple);
            platform.Draw();

            _spriteBatch.Draw(Globals.Pixel, new Rectangle((int)fewPoints.Position.X - 5, (int)fewPoints.Position.Y - 5, 10, 10), Color.Red);
            _spriteBatch.Draw(Globals.Pixel, new Rectangle((int)manyPoints.Position.X - 5, (int)manyPoints.Position.Y - 5, 10, 10), Color.Yellow);

            spriteAnimation.Draw();

            //_spriteBatch.DrawString(font, "distance: " + Vector2.Distance(circle.Center, player.Rect.Pos), new Vector2(10, 10), Color.White);
            //_spriteBatch.DrawString(font, "left: " + player.Rect.Rect.Left, new Vector2(10, 30), Color.White);
            //_spriteBatch.End();

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
            //rain.Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
            //rain.Parameters["Resolution"].SetValue(new Vector2(1280, 720));
            //rain.Parameters["RainSpeed"].SetValue(3.0f);     // Much faster! Try 2.0-5.0
            //rain.Parameters["RainDensity"].SetValue(25f);

            //rain.Parameters["DropLength"].SetValue(0.2f);    // Longer drops
            //rain.Parameters["DropWidth"].SetValue(0.02f);
            //rain.Parameters["RainOpacity"].SetValue(0.8f);
            //rain.Parameters["RippleStrength"].SetValue(0.3f);

            //canvas.Draw(_spriteBatch);

            // draw world sprites here
            _spriteBatch.End();

            // 2. UI layer: 1920x1080 coordinates, stays crisp at any window size
            canvas.BeginUI(_spriteBatch);
            _spriteBatch.DrawString(font, "Hello", new Vector2(40, 40), Color.White);
            canvas.EndUI(_spriteBatch);

            // 3. Composite both to the screen (optionally pass a shader for the game layer)
            canvas.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}