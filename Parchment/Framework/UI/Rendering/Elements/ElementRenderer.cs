using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        protected abstract Vector2 Measure(TElement element);
        protected abstract void Draw(SpriteBatch spriteBatch, TElement element, Rectangle bounds);

        Vector2 IElementRenderer.Measure(ElementData element)
        {
            if (element is TElement typedElement)
            {
                return this.Measure(typedElement);
            }

            return Vector2.Zero;
        }

        void IElementRenderer.Draw(SpriteBatch spriteBatch, ElementData element, Rectangle bounds)
        {
            if (element is TElement typedElement)
            {
                this.Draw(spriteBatch, typedElement, bounds);
            }
        }

        public float GetAlignedX(Rectangle bounds, float contentWidth, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Center:
                    return bounds.X + (bounds.Width - contentWidth) / 2f;
                case AlignmentType.Right:
                    return bounds.Right - contentWidth;
            }

            return bounds.X;
        }
    }
}
