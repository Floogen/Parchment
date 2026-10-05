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

            // Read once for the whole element, so every moving character on it is drawn at the same moment
            double effectTime = AnimationHelper.GetAnimationTime();

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

                    // SpriteText keeps its own color, so a line it draws only needs cutting up when something on it moves
                    bool keepsOwnColor = element.Font is SpriteTextAdapter;

                    if (line.Segments is null || (keepsOwnColor && line.HasEffects is false))
                    {
                        element.Font.DrawString(spriteBatch, line.Text, new Vector2(lineX, currentY), fadedColor, shadowColor, scale);
                    }
                    else
                    {
                        DrawSegments(spriteBatch, element, line.Segments, new Vector2(lineX, currentY), fadedColor, shadowColor, scale, keepsOwnColor, effectTime);
                    }
                }

                currentY += line.Size.Y;
            }
        }

        /// <summary>Draws a line one color at a time, each segment from where it was measured to start.
        /// A colored segment keeps the element's shadow, resolved against the segment's own color so an unset shadow follows its alpha the way it follows the element's.
        /// A segment inside a hovered link takes the link's hover color over whatever color it would otherwise have.
        /// A segment under an effect is drawn a character at a time, each moved from where it was laid out along with its shadow.
        /// An effect that colors, such as a rainbow or a pulse, starts from the segment's own color but leaves a hovered link's alone, so the link still shows the cursor is on it.
        /// </summary>
        /// <param name="keepsOwnColor">Whether the font ignores the colors it's handed, as SpriteText does, in which case every segment takes the element's own.</param>
        private static void DrawSegments(SpriteBatch spriteBatch, Element element, IReadOnlyList<TextSegment> segments, Vector2 linePosition, Color fadedColor, Color shadowColor, float scale, bool keepsOwnColor, double effectTime)
        {
            foreach (TextSegment segment in segments)
            {
                if (segment.Text.Length is 0)
                {
                    continue;
                }

                Color? hoveredLinkColor = keepsOwnColor ? null : GetHoveredLinkColor(element, segment);
                Color? runColor = keepsOwnColor ? null : hoveredLinkColor ?? segment.Color;
                Color segmentColor = runColor is Color drawnRunColor ? drawnRunColor * element.DrawAlpha : fadedColor;
                Color segmentShadowColor = runColor is null ? shadowColor : element.GetShadowColor(segmentColor);

                if (segment.Effects is null || segment.Characters is null || segment.CharacterOffsets is null)
                {
                    element.Font!.DrawString(spriteBatch, segment.Text, new Vector2(linePosition.X + segment.OffsetX, linePosition.Y), segmentColor, segmentShadowColor, scale);
                    continue;
                }

                for (int index = 0; index < segment.Characters.Count; index++)
                {
                    string character = segment.Characters[index];

                    // A space draws nothing, so it is skipped rather than given a draw call of its own
                    if (string.IsNullOrWhiteSpace(character))
                    {
                        continue;
                    }

                    int position = segment.SourceStart + index;
                    Vector2 effectOffset = TextEffectHelper.GetOffset(segment.Effects, position, effectTime, scale);
                    Vector2 characterPosition = new Vector2(linePosition.X + segment.CharacterOffsets[index] + effectOffset.X, linePosition.Y + effectOffset.Y);

                    Color characterColor = segmentColor;
                    Color characterShadowColor = segmentShadowColor;

                    // Started from the color the character would have had, so a pulse fades from the run's own color and a rainbow keeps the run's alpha
                    if (keepsOwnColor is false && hoveredLinkColor is null && TextEffectHelper.GetColor(segment.Effects, position, effectTime, runColor ?? element.TextColor) is Color effectColor)
                    {
                        // Faded with the element, so one that is fading out takes its effect colors down with it
                        characterColor = effectColor * element.DrawAlpha;
                        characterShadowColor = element.GetShadowColor(characterColor);
                    }

                    element.Font!.DrawString(spriteBatch, character, characterPosition, characterColor, characterShadowColor, scale);
                }
            }
        }

        /// <summary>The hover color of the link a segment belongs to while the cursor is over that link. Null otherwise.</summary>
        private static Color? GetHoveredLinkColor(Element element, TextSegment segment)
        {
            if (segment.LinkIndex is not int linkIndex || linkIndex >= element.Children.Count)
            {
                return null;
            }

            Element link = element.Children[linkIndex];

            return link.IsHovered ? link.HoverTextColor : null;
        }
    }
}
