using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Interfaces;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Fonts
{
    public class SpriteTextAdapter : IFont
    {
        // This mirrors how SpriteText uses 
        public Vector2 MeasureString(string text, float scale)
        {
            float previousFontPixelZoom = SpriteText.fontPixelZoom;
            SpriteText.fontPixelZoom = previousFontPixelZoom * scale;

            try
            {
                float maxLineWidth = 0f;
                float totalHeight = 0f;

                foreach (string line in text.Split('\n'))
                {
                    maxLineWidth = Math.Max(maxLineWidth, SpriteText.getWidthOfString(line));
                    totalHeight += SpriteText.getHeightOfString(line);
                }

                return new Vector2(maxLineWidth, totalHeight);
            }
            finally
            {
                SpriteText.fontPixelZoom = previousFontPixelZoom;
            }
        }

        // Color does nothing for SpriteText
        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
        {
            float previousFontPixelZoom = SpriteText.fontPixelZoom;
            SpriteText.fontPixelZoom = previousFontPixelZoom * scale;

            try
            {
                float currentY = position.Y;

                foreach (string line in text.Split('\n'))
                {
                    SpriteText.drawString(spriteBatch, line, (int)position.X, (int)currentY);
                    currentY += SpriteText.getHeightOfString(line);
                }
            }
            finally
            {
                SpriteText.fontPixelZoom = previousFontPixelZoom;
            }
        }
    }
}
