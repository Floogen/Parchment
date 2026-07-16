using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.UI.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Interfaces
{
    public interface IElementRenderer
    {
        Type DataType { get; }

        Vector2 Measure(Element element, ElementRenderContext context);
        void Draw(SpriteBatch spriteBatch, Element element, Rectangle bounds, ElementRenderContext context);
    }
}
