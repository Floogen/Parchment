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

        // Built on first use, as the font's own list is walked from the start for every lookup
        private HashSet<char>? _characters;

        internal SpriteFontAdapter(SpriteFont spriteFont)
        {
            _spriteFont = spriteFont;
        }

        public Vector2 MeasureString(string text, float scale)
        {
            return _spriteFont.MeasureString(text) * scale;
        }

        /// <summary>Draws the text with its shadow behind it, both exactly as given.
        /// How strongly the shadow comes through is settled by the element before it reaches here, since an authored shadow color and the game's own follow the text's alpha differently.
        /// </summary>
        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, float scale)
        {
            Utility.drawTextWithColoredShadow(spriteBatch, text, _spriteFont, position, color, shadowColor, scale);
        }

        public bool CanDraw(char character)
        {
            _characters ??= new HashSet<char>(_spriteFont.Characters);

            return _characters.Contains(character);
        }
    }
}
