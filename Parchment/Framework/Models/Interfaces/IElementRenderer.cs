using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Interfaces
{
    public interface IElementRenderer
    {
        Vector2 Measure(ElementData element);
        void Draw(SpriteBatch spriteBatch, ElementData element, Rectangle bounds);
    }
}
