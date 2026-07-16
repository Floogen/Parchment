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
    public class HeadingElementRenderer : ElementRenderer<HeadingElementData>
    {
        protected override Vector2 Measure(HeadingElementData element)
        {
            string text = element.Text ?? string.Empty;

            return Game1.dialogueFont.MeasureString(text);
        }

        protected override void Draw(SpriteBatch spriteBatch, HeadingElementData element, Rectangle bounds)
        {
            string text = element.Text ?? string.Empty;
            Vector2 size = Game1.dialogueFont.MeasureString(text);
            float x = GetAlignedX(bounds, size.X, element.Alignment);

            Utility.drawTextWithShadow(spriteBatch, text, Game1.dialogueFont, new Vector2(x, bounds.Y), Game1.textColor);
        }
    }
}
