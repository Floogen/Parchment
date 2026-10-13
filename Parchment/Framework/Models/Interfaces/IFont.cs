using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Interfaces
{
    public interface IFont
    {
        Vector2 MeasureString(string text, float scale);

        /// <summary>Draws the text with its shadow behind it.</summary>
        /// <param name="boldness">How far to the right a second copy of the text is drawn to thicken it, in screen pixels. Zero draws it once. Every shadow goes down before any copy of the text, so one copy's shadow never covers the other's letters.</param>
        /// <param name="rotation">How far to turn the text about its own center, in radians. A font that can't rotate its letters draws them upright.</param>
        /// <param name="isHalo">Whether this is one of an outline's or a glow's copies of the text, which Junimo script draws as a flat silhouette in the given color.</param>
        /// <param name="isColored">Whether the color came from markup or a link rather than the element's own text color. Junimo script keeps the game's own look unless it does.</param>
        void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, Color shadowColor, float scale, int boldness = 0, float rotation = 0f, bool isHalo = false, bool isColored = false);

        /// <summary>Whether the font has a glyph of its own for the character, rather than drawing a stand-in for it. A character a [junimo] tag swapped in always counts, as SpriteText writes it in the Junimo script.</summary>
        bool CanDraw(char character);
    }
}
