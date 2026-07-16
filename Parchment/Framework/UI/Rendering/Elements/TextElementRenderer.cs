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
        protected abstract string GetText(TElement data);

        protected override Vector2 Measure(TElement data, Element element, ElementRenderContext context)
        {
            if (element.Font is null)
            {
                Parchment.monitor.Log($"{this.GetType().Name} has no resolved font (element will not render).", LogLevel.Warn);
                element.LayoutState = null;
                return Vector2.Zero;
            }

            WrappedText wrappedText = TextWrapper.Wrap(this.GetText(data), element.Font, context.AvailableWidth, element.Data.Scale);
            element.LayoutState = wrappedText;

            return wrappedText.Size;
        }

        protected bool TryGetWrappedText(Element element, out WrappedText wrappedText)
        {
            wrappedText = element.LayoutState as WrappedText;

            return wrappedText is not null;
        }
    }
}
