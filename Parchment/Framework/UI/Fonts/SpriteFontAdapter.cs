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
        /// The shadows mirror the game's Utility.drawTextWithColoredShadow, offsets and order included, so text drawn here matches text the game draws. A rotation turns the text and its shadows about the text's center.
        /// </summary>
        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, float scale, int boldness = 0, float rotation = 0f)
        {
            text ??= string.Empty;

            // Left at the corner for upright text, so it lands exactly where the game's helper would put it
            Vector2 origin = rotation == 0f ? Vector2.Zero : _spriteFont.MeasureString(text) / 2f;
            Vector2 pivot = position + origin * scale;
            float layerDepth = position.Y / 10000f;

            if (boldness <= 0)
            {
                DrawShadows(spriteBatch, text, pivot, shadowColor, scale, rotation, origin, layerDepth);
                spriteBatch.DrawString(_spriteFont, text, pivot, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
                return;
            }

            Vector2 boldPivot = pivot + new Vector2(boldness, 0f);

            DrawShadows(spriteBatch, text, pivot, shadowColor, scale, rotation, origin, layerDepth);
            DrawShadows(spriteBatch, text, boldPivot, shadowColor, scale, rotation, origin, layerDepth);
            spriteBatch.DrawString(_spriteFont, text, pivot, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
            spriteBatch.DrawString(_spriteFont, text, boldPivot, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
        }

        /// <summary>Draws the game's two shadow copies of the text, one down and to the left and one straight down. A shadow that is fully transparent would draw nothing, so it is skipped, which is what keeps an outline's or a glow's many copies cheap.</summary>
        private void DrawShadows(SpriteBatch spriteBatch, string text, Vector2 pivot, Color shadowColor, float scale, float rotation, Vector2 origin, float layerDepth)
        {
            if (shadowColor.PackedValue is 0)
            {
                return;
            }

            // The game tucks the shadow closer in for its small font and for the languages it writes longer words in
            LocalizedContentManager.LanguageCode language = Game1.content.GetCurrentLanguage();
            bool isCompact = _spriteFont.Equals(Game1.smallFont) || language is LocalizedContentManager.LanguageCode.ru or LocalizedContentManager.LanguageCode.de;
            int horizontalShadowOffset = isCompact ? -2 : -3;
            int verticalShadowOffset = isCompact ? 2 : 3;

            spriteBatch.DrawString(_spriteFont, text, pivot + new Vector2(horizontalShadowOffset, verticalShadowOffset), shadowColor, rotation, origin, scale, SpriteEffects.None, layerDepth - 0.0001f);
            spriteBatch.DrawString(_spriteFont, text, pivot + new Vector2(0f, verticalShadowOffset), shadowColor, rotation, origin, scale, SpriteEffects.None, layerDepth - 0.0003f);
        }

        public bool CanDraw(char character)
        {
            _characters ??= new HashSet<char>(_spriteFont.Characters);

            return _characters.Contains(character);
        }
    }
}
