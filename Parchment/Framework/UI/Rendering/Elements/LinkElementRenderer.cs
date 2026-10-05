using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;

namespace Parchment.Framework.UI.Rendering.Elements
{
    /// <summary>Stands behind every link element without measuring or drawing anything, as the text element a link sits in places it and draws its text.
    /// Not registered with the element registry, so a link can only come from markup and never from an element authored with "Type": "Link".
    /// </summary>
    public class LinkElementRenderer : ElementRenderer<LinkElementData>
    {
        protected override Vector2 Measure(LinkElementData data, Element element, ElementRenderContext context)
        {
            return Vector2.Zero;
        }

        protected override void Draw(SpriteBatch spriteBatch, LinkElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
        }
    }
}
