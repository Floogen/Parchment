using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Interfaces;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Fonts
{
    public class SpriteFontAdapter : IFont
    {
        private readonly SpriteFont _spriteFont;

        internal SpriteFontAdapter(SpriteFont spriteFont)
        {
            _spriteFont = spriteFont;
        }

        public Vector2 MeasureString(string text, float scale)
        {
            return _spriteFont.MeasureString(text) * scale;
        }

        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
        {
            Utility.drawTextWithShadow(spriteBatch, text, _spriteFont, position, color, scale);
        }
    }
}
