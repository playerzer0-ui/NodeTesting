using NodeTesting.models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NodeTesting
{
    public class Platform
    {
        private Sprite platform;
        private Path2D path2d;

        public Platform()
        {
            path2d = new Path2D();
            path2d.AddWaypoint(new Vector2(200, 400));
            path2d.AddWaypoint(new Vector2(500, 400));
            path2d.AddWaypoint(new Vector2(600, 600));
            //path2d.AddWaypoint(new Vector2(200, 700));
            //path2d.AddWaypoint(new Vector2(200, 400));
            path2d.IsPingPong = true;

            // Initialise the sprite at the path's starting position
            platform = new Sprite("player", path2d.Position);
        }

        public void Update(GameTime gt)
        {
            path2d.Update(gt);
            platform.Pos = path2d.Position; // sprite follows the path
        }

        public void Draw()
        {
            platform.Draw(Color.White);
        }
    }
}
