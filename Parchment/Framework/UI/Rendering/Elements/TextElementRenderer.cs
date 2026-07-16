using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities;
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
        protected abstract SpriteFont GetFont(TElement data, ElementRenderContext context);


        protected override Vector2 Measure(TElement data, Element element, ElementRenderContext context)
        {
            SpriteFont font = this.GetFont(data, context);
            WrappedText wrappedText = TextWrapper.Wrap(this.GetText(data), font, context.AvailableWidth / data.Scale);
            element.LayoutState = wrappedText;

            return new Vector2(context.AvailableWidth, wrappedText.Size.Y * data.Scale);
        }

        protected bool TryGetWrappedText(Element element, out WrappedText wrappedText)
        {
            wrappedText = element.LayoutState as WrappedText;

            return wrappedText is not null;
        }
    }
}
