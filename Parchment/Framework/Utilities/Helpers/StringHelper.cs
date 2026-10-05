using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class StringHelper
    {
        public static void DrawLines(SpriteBatch spriteBatch, Element element, WrappedText wrappedText, Rectangle bounds, AlignmentType alignment, Color textColor, float scale)
        {
            // Every text element draws through here, so this is the only place text has to be told about a fade
            Color fadedColor = textColor * element.DrawAlpha;

            // Resolved against the faded color, so a default shadow follows the text down while a given one keeps the strength it was given
            Color shadowColor = element.GetShadowColor(fadedColor);

            float currentY = bounds.Y;
            foreach (WrappedLine line in wrappedText.Lines)
            {
                if (element.Font is null)
                {
                    continue;
                }

                if (line.Text.Length > 0)
                {
                    // The caller's alignment rather than the element's, since Alignment places the element itself while an Image caption, Banner or Button lines its text up on its own terms
                    float lineX = AlignmentHelper.GetAlignedX(bounds, line.Size.X, alignment);

                    // SpriteText keeps its own color, so a colored line draws whole there the same as any other
                    if (line.Segments is null || element.Font is SpriteTextAdapter)
                    {
                        element.Font.DrawString(spriteBatch, line.Text, new Vector2(lineX, currentY), fadedColor, shadowColor, scale);
                    }
                    else
                    {
                        DrawSegments(spriteBatch, element, line.Segments, new Vector2(lineX, currentY), fadedColor, shadowColor, scale);
                    }
                }

                currentY += line.Size.Y;
            }
        }

        /// <summary>Draws a line one color at a time, each segment from where it was measured to start.
        /// A colored segment keeps the element's shadow, resolved against the segment's own color so an unset shadow follows its alpha the way it follows the element's.
        /// </summary>
        private static void DrawSegments(SpriteBatch spriteBatch, Element element, IReadOnlyList<TextSegment> segments, Vector2 linePosition, Color fadedColor, Color shadowColor, float scale)
        {
            foreach (TextSegment segment in segments)
            {
                if (segment.Text.Length is 0)
                {
                    continue;
                }

                Color segmentColor = segment.Color is Color runColor ? runColor * element.DrawAlpha : fadedColor;
                Color segmentShadowColor = segment.Color is null ? shadowColor : element.GetShadowColor(segmentColor);

                element.Font!.DrawString(spriteBatch, segment.Text, new Vector2(linePosition.X + segment.OffsetX, linePosition.Y), segmentColor, segmentShadowColor, scale);
            }
        }
    }
}
