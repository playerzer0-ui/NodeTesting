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
        CollisionCircle circle;
        CollisionRect rectangle;
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
            circle = new CollisionCircle(300, 300, 20);
            font = Content.Load<SpriteFont>("File");
            rectangle = new CollisionRect(500, 100, 200, 200);
            camera = new Camera();
            canvas = new Canvas(_graphics.GraphicsDevice, 1280, 720);

            //sounds
            song = Content.Load<Song>("sounds/nature");
            

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            player.Update(gameTime);
            canvas.SetResolution(_graphics.GraphicsDevice.Viewport.Width, _graphics.GraphicsDevice.Viewport.Height);
            camera.Position = player.Pos;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            canvas.Activate();
            // TODO: Add your drawing code here
            _spriteBatch.Begin(transformMatrix: camera.Transform());
            player.Draw(Color.White);
            player.Rect.Draw(new Color(255, 0, 0, 128));
            if (circle.Collides(player.Rect.Rect))
            {
                circle.Draw(new Color(0, 255, 0, 128));
            }
            else
            {
                circle.Draw(new Color(255, 0, 0, 128));
            }

            if(rectangle.Intersects(player.Rect))
            {
                rectangle.Draw(new Color(0, 255, 0, 128));
                MediaPlayer.Play(song);
            }
            else
            {
                rectangle.Draw(new Color(255, 0, 0, 128));
            }

            _spriteBatch.DrawString(font, "distance: " + Vector2.Distance(circle.Center, player.Rect.Pos), new Vector2(10, 10), Color.White);
            _spriteBatch.DrawString(font, "left: " + player.Rect.Rect.Left, new Vector2(10, 30), Color.White);
            _spriteBatch.End();

            canvas.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}