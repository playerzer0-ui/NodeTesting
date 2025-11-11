using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace NodeTesting.models
{
    public interface ICollider
    {
        bool Intersects(ICollider other);
        bool Contains(Point point);
        void Draw(Color color);
    }

}
