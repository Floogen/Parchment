using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class ParagraphElementRenderer : ElementRenderer<ParagraphElementData>
    {
        protected override Vector2 Measure(ParagraphElementData element)
        {
            // TODO: Add Context class to hold AvailableWidth and pass to MeasureString via Game1.parseText
            //string wrappedText = Game1.parseText(element.Text ?? string.Empty, Game1.smallFont, availableWidth);

            string text = element.Text ?? string.Empty;
            return Game1.smallFont.MeasureString(text);
        }

        protected override void Draw(SpriteBatch spriteBatch, ParagraphElementData element, Rectangle bounds)
        {
            string wrapped = Game1.parseText(element.Text ?? string.Empty, Game1.smallFont, bounds.Width);
            Vector2 size = Game1.smallFont.MeasureString(wrapped);
            float x = GetAlignedX(bounds, size.X, element.Alignment);

            Utility.drawTextWithShadow(spriteBatch, wrapped, Game1.smallFont, new Vector2(x, bounds.Y), Game1.textColor);
        }
    }
}
