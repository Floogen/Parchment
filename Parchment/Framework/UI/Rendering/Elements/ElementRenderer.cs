using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public abstract class ElementRenderer<TElement> : IElementRenderer where TElement : ElementData
    {
        public Type DataType => typeof(TElement);
        protected abstract Vector2 Measure(TElement data, Element element, ElementRenderContext context);
        protected abstract void Draw(SpriteBatch spriteBatch, TElement data, Element element, Rectangle bounds, ElementRenderContext context);

        Vector2 IElementRenderer.Measure(Element element, ElementRenderContext context)
        {
            if (element.Data is TElement typedData)
            {
                return this.Measure(typedData, element, context);
            }

            return Vector2.Zero;
        }

        void IElementRenderer.Draw(SpriteBatch spriteBatch, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.Data is TElement typedData)
            {
                this.Draw(spriteBatch, typedData, element, bounds, context);
            }
        }
    }
}
