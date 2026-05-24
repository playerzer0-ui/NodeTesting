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

            //sounds
            song = Content.Load<Song>("sounds/nature");
            

        }

        protected override void Update(GameTime gameTime)
        {
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
            canvas.Activate();
            // TODO: Add your drawing code here
            _spriteBatch.Begin(transformMatrix: camera.Transform());
            player.Draw(Color.White);
            player.Rect.Draw(new Color(255, 0, 0, 128));

            stepHere.Draw(new Color(0, 0, 255, 128));
            rectangle.Draw(Color.White);

            bubble.Draw();
            circle.Draw(Color.White);

            foreach (ICollider wall in walls)
            {
                wall.Draw(Color.Purple);
            }

            platform.Draw();
            _spriteBatch.DrawString(font, "distance: " + Vector2.Distance(circle.Center, player.Rect.Pos), new Vector2(10, 10), Color.White);
            _spriteBatch.DrawString(font, "left: " + player.Rect.Rect.Left, new Vector2(10, 30), Color.White);
            _spriteBatch.End();

            canvas.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}