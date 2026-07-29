using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public abstract class TextElementRenderer<TElement> : ElementRenderer<TElement> where TElement : ElementData
    {
        protected abstract string GetText(TElement data, ElementRenderContext context);

        /// <summary>
        /// The width this element reserves, in screen pixels. Text wraps at it and the measured element is exactly this wide, so alignment and hit testing use the whole box.
        /// Returns null when the element has no width of its own, in which case text wraps at the width available and the element measures as wide as its longest line.
        /// </summary>
        protected virtual float? GetExplicitWidth(TElement data, ElementRenderContext context)
        {
            return null;
        }

        protected override Vector2 Measure(TElement data, Element element, ElementRenderContext context)
        {
            if (element.Font is null)
            {
                Parchment.monitor.Log($"{this.GetType().Name} has no resolved font (element will not render).", LogLevel.Warn);
                element.LayoutState = null;
                return Vector2.Zero;
            }

            float? explicitWidth = this.GetExplicitWidth(data, context);

            WrappedText wrappedText = TextWrapper.Wrap(this.GetText(data, context), element.Font, explicitWidth ?? context.AvailableWidth, element.Data.Scale);
            element.LayoutState = wrappedText;

            return new Vector2(explicitWidth ?? wrappedText.Size.X, wrappedText.Size.Y);
        }

        protected bool TryGetWrappedText(Element element, out WrappedText wrappedText)
        {
            wrappedText = element.LayoutState as WrappedText;

            return wrappedText is not null;
        }
    }
}
