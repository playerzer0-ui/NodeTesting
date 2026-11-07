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

        List<Tree> trees;
        List<Coin> coins;

        // collision.csv kinds: -1 = empty, 0 = wall, 1 = ice
        const int Wall = 0;
        const int Ice = 1;
        CollisionMap collisionMap;

        Lighting lighting;
        PointLight playerLight;
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
            //camera.SetBounds(tilemap.PixelBounds);

            WallRect = new CollisionRect(500, 500, 100, 100);
            WallCircle = new CollisionCircle(600, 500, 50);
            WallRect.IsStatic = true;
            WallCircle.IsStatic = true;

            // Objects placed by a CSV: 1 = tree, 2 = coin
            var placements = new ObjectMap("objects.csv", 16, 16);
            trees = placements.Spawn(1, position => new Tree(position));
            coins = placements.Spawn(2, position => new Coin(position));

            // Lighting: a dark night, a warm light that follows the player,
            // a small golden glow on every coin, and two colored lamps among the trees
            lighting = new Lighting(canvas, GraphicsDevice) { Ambient = new Color(25, 25, 45) };
            playerLight = lighting.Add(new PointLight(player.Pos, 180, new Color(255, 230, 190)));
            foreach (Vector2 coinCell in placements.GetPositions(2))
                lighting.Add(new PointLight(coinCell - new Vector2(0, 8), 28, new Color(255, 210, 90), 0.8f)
                             { CastsShadows = false });   // tiny glows don't need shadows
            lighting.Add(new PointLight(new Vector2(390, 130), 110, new Color(255, 140, 60)));   // orange lamp
            lighting.Add(new PointLight(new Vector2(470, 210), 90, new Color(90, 150, 255)));    // blue lamp
            lighting.Add(new PointLight(new Vector2(550, 420), 260, new Color(230, 230, 255)));  // lamp above the two walls
            lighting.Add(new PointLight(new Vector2(3000, 3000), 200, Color.Red));                // far away: never on screen, so never drawn

            // Walls and ice painted as tiles; each kind is merged into a few big rectangles
            collisionMap = new CollisionMap("tilemap", 16, 16, "collision.csv");

            // Everything the player bumps into: each tree's trunk and the collision map's walls.
            // Ice is a floor, so it isn't added: the player walks over it.
            var wallList = new List<ICollider> { WallRect, WallCircle };
            foreach (Tree tree in trees)
                wallList.Add(tree.Wall);
            wallList.AddRange(collisionMap.GetCollisionRects(Wall));
            walls = wallList.ToArray();

            // The same walls and trunks block light
            lighting.AddOccluders(walls);

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
            playerLight.Position = player.Pos;

            // Press L to switch lighting on and off
            if (Globals.Input.KeyJustDown(Keys.L))
                lighting.Enabled = !lighting.Enabled;
            foreach (Tree tree in trees)
                tree.Update(player);
            foreach (Coin coin in coins)
                coin.Update(gameTime);
            bubble.Update(gameTime);
            camera.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // ── Pass 1: flat things (floor, wall tops, debug shapes) ─────────────
            // These are lit pixel by pixel by the light map.
            canvas.Activate();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.Transform());

            tilemap.Draw(camera.VisibleArea);
            collisionMap.DrawCollisionRects(Ice, new Color(140, 210, 255));   // ice floor
            foreach (Coin coin in coins)
                coin.Draw();

            player.Rect.Draw(new Color(255, 0, 0, 128));
            stepHere.Draw(new Color(0, 0, 255, 128));
            //rectangle.Draw(Color.White);
            bubble.Draw();
            circle.Draw(Color.White);
            foreach (ICollider wall in walls)
                wall.Draw(Color.Purple);

            // Show how the collision map's walls were merged: each rectangle gets its own color
            Color[] mergeColors = { Color.IndianRed, Color.Goldenrod, Color.MediumSeaGreen, Color.SteelBlue, Color.Orchid };
            List<CollisionRect> wallRects = collisionMap.GetCollisionRects(Wall);
            for (int i = 0; i < wallRects.Count; i++)
                wallRects[i].Draw(mergeColors[i % mergeColors.Length]);

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

            // Darken the world and apply the lights (game layer only, not the UI)
            lighting.Draw(_spriteBatch, camera.Transform());

            // ── Pass 2: standing things (characters, trees) ──────────────────────
            // Drawn after the lighting, each tinted by the light at its feet,
            // so the whole sprite is lit or dark together.
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: camera.Transform());

            // Trees further up the screen than the player's feet are behind the player:
            // draw them first. The rest are in front: draw them after the player.
            float playerFeetY = player.Rect.Rect.Bottom;
            foreach (Tree tree in trees)
                if (tree.BaseY <= playerFeetY) tree.Draw(lighting.GetLightAt(tree.BasePosition));

            // Light at the player's feet, tinted light blue while standing on ice
            Color playerLight = lighting.GetLightAt(player.Rect.Center);
            bool onIce = collisionMap.GetKindAt(player.Rect.Center) == Ice;
            if (onIce)
                playerLight = new Color(playerLight.ToVector3() * Color.LightBlue.ToVector3());
            player.Draw(playerLight);

            foreach (Tree tree in trees)
                if (tree.BaseY > playerFeetY) tree.Draw(lighting.GetLightAt(tree.BasePosition));

            platform.Draw(lighting.GetLightAt(platform.Feet));

            _spriteBatch.End();

            // 2. UI layer: 1920x1080 coordinates, stays crisp at any window size
            canvas.BeginUI(_spriteBatch);
            _spriteBatch.DrawString(font, "Hello", new Vector2(40, 40), Color.White);
            _spriteBatch.DrawString(font, $"Lights drawn: {lighting.DrawnLightCount} / {lighting.Lights.Count}",
                                    new Vector2(40, 980), Color.White, 0f, Vector2.Zero, 0.3f, SpriteEffects.None, 0f);
            canvas.EndUI(_spriteBatch);

            // 3. Composite both to the screen (optionally pass a shader for the game layer)
            canvas.Draw(_spriteBatch);

            base.Draw(gameTime);
        }
    }
}