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
        // Read on every use rather than kept, as the game loads a new font object when the language changes and the old one has no glyphs for the new language
        private readonly Func<SpriteFont> _getSpriteFont;

        // The font the cached glyph data below was read from, so a language change clears it
        private SpriteFont? _readFont;

        // Built on first use, as the font's own list is walked from the start for every lookup
        private HashSet<char>? _characters;

        // Read from the font's glyphs on first use, being where its letters sit for [junimo] text to be sized to
        private JunimoScript.LetterBounds? _letterBounds;
        private bool _hasReadLetterBounds;

        internal SpriteFontAdapter(Func<SpriteFont> getSpriteFont)
        {
            _getSpriteFont = getSpriteFont;
        }

        private SpriteFont CurrentFont
        {
            get
            {
                SpriteFont spriteFont = _getSpriteFont();
                if (ReferenceEquals(spriteFont, _readFont) is false)
                {
                    _readFont = spriteFont;
                    _characters = null;
                    _letterBounds = null;
                    _hasReadLetterBounds = false;
                }

                return spriteFont;
            }
        }

        public Vector2 MeasureString(string text, float scale)
        {
            // The font has no glyphs for a [junimo] tag's characters, so those stretches are measured in the script instead
            if (JunimoScript.Contains(text))
            {
                return JunimoScript.Measure(text, scale, GetLetterBounds(), plainText => CurrentFont.MeasureString(plainText) * scale);
            }

            return CurrentFont.MeasureString(text) * scale;
        }

        /// <summary>Draws the text with its shadow behind it, both exactly as given.
        /// How strongly the shadow comes through is settled by the element before it reaches here, since an authored shadow color and the game's own follow the text's alpha differently.
        /// The shadows mirror the game's Utility.drawTextWithColoredShadow, offsets and order included, so text drawn here matches text the game draws. A rotation turns the text and its shadows about the text's center.
        /// Any of it a [junimo] tag swapped in is written in the Junimo script from SpriteText's sheet, whose glyphs carry their own drop shadow.
        /// </summary>
        public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, float scale, int boldness = 0, float rotation = 0f, bool isHalo = false, bool isColored = false)
        {
            text ??= string.Empty;

            if (JunimoScript.Contains(text))
            {
                JunimoScript.Draw(spriteBatch, text, position, color, shadowColor, isColored, boldness, rotation, isHalo, scale, GetLetterBounds(), plainText => CurrentFont.MeasureString(plainText) * scale, (plainText, plainPosition) => DrawPlain(spriteBatch, plainText, plainPosition, color, shadowColor, scale, boldness, rotation));
                return;
            }

            DrawPlain(spriteBatch, text, position, color, shadowColor, scale, boldness, rotation);
        }

        private JunimoScript.LetterBounds? GetLetterBounds()
        {
            // Read first, so a font swapped by a language change clears what was read from the old one
            SpriteFont spriteFont = CurrentFont;
            if (_hasReadLetterBounds is false)
            {
                _hasReadLetterBounds = true;
                _letterBounds = JunimoScript.ReadLetterBounds(spriteFont);
            }

            return _letterBounds;
        }

        private void DrawPlain(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, float scale, int boldness, float rotation)
        {
            // Left at the corner for upright text, so it lands exactly where the game's helper would put it
            Vector2 origin = rotation == 0f ? Vector2.Zero : CurrentFont.MeasureString(text) / 2f;
            Vector2 pivot = position + origin * scale;
            float layerDepth = position.Y / 10000f;

            if (boldness <= 0)
            {
                DrawShadows(spriteBatch, text, pivot, shadowColor, scale, rotation, origin, layerDepth);
                spriteBatch.DrawString(CurrentFont, text, pivot, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
                return;
            }

            Vector2 boldPivot = pivot + new Vector2(boldness, 0f);

            DrawShadows(spriteBatch, text, pivot, shadowColor, scale, rotation, origin, layerDepth);
            DrawShadows(spriteBatch, text, boldPivot, shadowColor, scale, rotation, origin, layerDepth);
            spriteBatch.DrawString(CurrentFont, text, pivot, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
            spriteBatch.DrawString(CurrentFont, text, boldPivot, color, rotation, origin, scale, SpriteEffects.None, layerDepth);
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
            bool isCompact = ReferenceEquals(CurrentFont, Game1.smallFont) || language is LocalizedContentManager.LanguageCode.ru or LocalizedContentManager.LanguageCode.de;
            int horizontalShadowOffset = isCompact ? -2 : -3;
            int verticalShadowOffset = isCompact ? 2 : 3;

            spriteBatch.DrawString(CurrentFont, text, pivot + new Vector2(horizontalShadowOffset, verticalShadowOffset), shadowColor, rotation, origin, scale, SpriteEffects.None, layerDepth - 0.0001f);
            spriteBatch.DrawString(CurrentFont, text, pivot + new Vector2(0f, verticalShadowOffset), shadowColor, rotation, origin, scale, SpriteEffects.None, layerDepth - 0.0003f);
        }

        public bool CanDraw(char character)
        {
            if (JunimoScript.IsGlyph(character))
            {
                return true;
            }

            // Read first, so a font swapped by a language change clears the old font's characters
            SpriteFont spriteFont = CurrentFont;
            _characters ??= new HashSet<char>(spriteFont.Characters);

            return _characters.Contains(character);
        }
    }
}
