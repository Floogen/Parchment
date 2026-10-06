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

        /// <summary>Whether SpriteText has a sprite of its own for the character. Its sprites are laid out by character code over the printable ASCII range, so anything outside that is treated as one it can't draw.
        /// A language that loads its own SpriteText font may draw more, which this doesn't try to read, so it errs on the side of a glyph set that stays drawable everywhere.
        /// </summary>
        public bool CanDraw(char character)
        {
            return character >= ' ' && character <= '~';
        }

        // TextColor and ShadowColor do nothing for SpriteText, which draws its own outline, though how strongly to draw it still comes from the color's alpha
        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, float scale)
        {
            float previousFontPixelZoom = SpriteText.fontPixelZoom;
            SpriteText.fontPixelZoom = previousFontPixelZoom * scale;

            try
            {
                float currentY = position.Y;
                float alpha = color.A / 255f;

                foreach (string line in text.Split('\n'))
                {
                    SpriteText.drawString(spriteBatch, line, (int)position.X, (int)currentY, alpha: alpha);
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
