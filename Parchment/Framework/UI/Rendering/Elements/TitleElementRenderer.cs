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
    public class TitleElementRenderer : ElementRenderer<TitleElementData>
    {
        protected override Vector2 Measure(TitleElementData element)
        {
            string text = element.Text ?? string.Empty;

            return new Vector2(SpriteText.getWidthOfString(text), SpriteText.getHeightOfString(text));
        }

        protected override void Draw(SpriteBatch spriteBatch, TitleElementData element, Rectangle bounds)
        {
            string text = element.Text ?? string.Empty;
            float textWidth = SpriteText.getWidthOfString(text);
            float x = GetAlignedX(bounds, textWidth, element.Alignment);
            SpriteText.drawString(spriteBatch, text, (int)x, (int)bounds.Y);
        }
    }
}
