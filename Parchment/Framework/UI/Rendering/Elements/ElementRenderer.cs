using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities.Helpers;
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

        public virtual Rectangle GetContentBounds(Element element, Rectangle bounds)
        {
            return bounds;
        }

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
                this.Draw(spriteBatch, typedData, element, ApplyFrameOffset(element, bounds), context);
            }
        }

        /// <summary>Shifts the rectangle an element draws into by whatever its current frame offsets by. Applied here rather than per renderer, so a frame moves any element and carries its children and
        /// its own layers along with it, since every renderer draws from the rectangle it is handed.
        /// The element keeps the bounds it was measured with, which is why the offset lands here and not on <see cref="Element.Bounds"/>: a moving element slides over its own footprint rather than
        /// pushing the page around or dragging its hitbox with it.
        /// </summary>
        private Rectangle ApplyFrameOffset(Element element, Rectangle bounds)
        {
            AnimationFrameData? activeFrame = AnimationHelper.GetActiveFrame(element, element.Data.FrameDuration);

            if (activeFrame?.Offset is null)
            {
                return bounds;
            }

            Vector2 frameOffset = AnimationHelper.GetFrameOffset(activeFrame, this.GetOffsetScale(element));

            return new Rectangle(bounds.X + (int)frameOffset.X, bounds.Y + (int)frameOffset.Y, bounds.Width, bounds.Height);
        }

        /// <summary>The scale a frame's Offset is measured against, which is the element's own scale for everything that draws at the scale it was authored with.
        /// Overridden by a renderer whose draw scale can differ from that, such as an Image shrunk to fit the width available.
        /// </summary>
        protected virtual float GetOffsetScale(Element element)
        {
            return element.Data.Scale;
        }
    }
}
