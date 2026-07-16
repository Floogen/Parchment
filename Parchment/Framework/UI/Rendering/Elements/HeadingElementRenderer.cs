using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities.Helpers;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class HeadingElementRenderer : TextElementRenderer<HeadingElementData>
    {
        protected override string GetText(HeadingElementData data)
        {
            return data.Text ?? string.Empty;
        }

        protected override void Draw(SpriteBatch spriteBatch, HeadingElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (this.TryGetWrappedText(element, out WrappedText wrappedText) is false || element.Font is null)
            {
                return;
            }

            Vector2 textSize = wrappedText.Size * data.Scale;

            DrawLines(spriteBatch, element, wrappedText, bounds, data.Alignment, element.Color, data.Scale);
        }
    }
}
