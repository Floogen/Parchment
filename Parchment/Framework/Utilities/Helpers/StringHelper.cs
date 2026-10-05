using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Layouts;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class StringHelper
    {
        // How thick an underline or strike is, in unscaled pixels multiplied by the text's scale
        private const float DECORATION_THICKNESS = 2f;

        // Where the lines and the redaction bar sit, as a share of the line's height measured from its top. Tuned against the game's own fonts, whose lines leave room below the letters
        private const float UNDERLINE_POSITION = 0.82f;
        private const float STRIKE_POSITION = 0.5f;
        private const float REDACT_TOP = 0.12f;
        private const float REDACT_BOTTOM = 0.88f;

        // A translucent marker yellow, written already faded by its own alpha the way a parsed color is
        private static readonly Color _defaultHighlightColor = new Color(255, 220, 90) * 0.45f;

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
                        DrawSegments(spriteBatch, element, line.Segments, new Vector2(lineX, currentY), line.Size.Y, fadedColor, shadowColor, scale, keepsOwnColor, effectTime);
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
        /// A hovered link's own hover effects apply last, timed from the cursor's arrival and eased in, with any color they set starting from the link's hover color.
        /// A character a typewriter hasn't reached yet is left out. One it is fading in is drawn at the strength it has reached.
        /// </summary>
        /// <param name="keepsOwnColor">Whether the font ignores the colors it's handed, as SpriteText does, in which case every segment takes the element's own.</param>
        /// <param name="lineHeight">How tall the line is, which decorations such as an underline are placed against.</param>
        private static void DrawSegments(SpriteBatch spriteBatch, Element element, IReadOnlyList<TextSegment> segments, Vector2 linePosition, float lineHeight, Color fadedColor, Color shadowColor, float scale, bool keepsOwnColor, double effectTime)
        {
            foreach (TextSegment segment in segments)
            {
                if (segment.Text.Length is 0)
                {
                    continue;
                }

                Element? link = GetLink(element, segment);
                bool isLinkHovered = link is not null && link.IsHovered;

                Color? hoveredLinkColor = keepsOwnColor || isLinkHovered is false ? null : link!.HoverTextColor;
                Color? runColor = keepsOwnColor ? null : hoveredLinkColor ?? segment.Color;
                Color segmentColor = runColor is Color drawnRunColor ? drawnRunColor * element.DrawAlpha : fadedColor;
                Color segmentShadowColor = runColor is null ? shadowColor : element.GetShadowColor(segmentColor);

                IReadOnlyList<TextEffect>? hoverEffects = isLinkHovered ? segment.HoverEffects : null;

                double hoverTime = hoverEffects is null ? 0d : effectTime - link!.HoverAnimationStartedAt;
                float hoverStrength = hoverEffects is null ? 0f : TextEffectHelper.GetHoverStrength(hoverTime);

                DecorationContext decoration = new DecorationContext(element, segment, linePosition, lineHeight, segmentColor, scale, GetDecorationWidth(element, segment, effectTime));

                DrawDecorations(spriteBatch, decoration, segment.Effects, 1f, isBehindText: true);
                DrawDecorations(spriteBatch, decoration, hoverEffects, hoverStrength, isBehindText: true);

                // A redacted segment is only its bar, so nothing of the text underneath is drawn
                if (TextEffectHelper.HasEffect(segment.Effects, TextEffectType.Redact))
                {
                    continue;
                }

                bool hasCharacterEffects = TextEffectHelper.HasCharacterEffects(segment.Effects) || TextEffectHelper.HasCharacterEffects(hoverEffects);

                if (hasCharacterEffects is false || segment.Characters is null || segment.CharacterOffsets is null)
                {
                    element.Font!.DrawString(spriteBatch, segment.Text, new Vector2(linePosition.X + segment.OffsetX, linePosition.Y), segmentColor, segmentShadowColor, scale);

                    DrawDecorations(spriteBatch, decoration, segment.Effects, 1f, isBehindText: false);
                    DrawDecorations(spriteBatch, decoration, hoverEffects, hoverStrength, isBehindText: false);
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

                    // A typewriter hasn't reached this character yet, so there is nothing to draw
                    float revealAlpha = segment.Effects is null ? 1f : TypewriterHelper.GetRevealAlpha(element, segment.Effects, position, effectTime);
                    if (revealAlpha <= 0f)
                    {
                        continue;
                    }

                    Vector2 effectOffset = segment.Effects is null ? Vector2.Zero : TextEffectHelper.GetOffset(segment.Effects, position, effectTime, scale);

                    if (hoverEffects is not null)
                    {
                        effectOffset += TextEffectHelper.GetOffset(hoverEffects, position, hoverTime, scale) * hoverStrength;
                    }

                    Vector2 characterPosition = new Vector2(linePosition.X + segment.CharacterOffsets[index] + effectOffset.X, linePosition.Y + effectOffset.Y);

                    Color characterColor = segmentColor;
                    Color characterShadowColor = segmentShadowColor;

                    if (keepsOwnColor is false && TryGetEffectColor(segment, hoverEffects, position, effectTime, hoverTime, hoverStrength, runColor ?? element.TextColor, hoveredLinkColor is not null, out Color effectColor))
                    {
                        // Faded with the element, so one that is fading out takes its effect colors down with it
                        characterColor = effectColor * element.DrawAlpha;
                        characterShadowColor = element.GetShadowColor(characterColor);
                    }

                    // Faded together with its shadow, so a character fading in doesn't leave its shadow standing at full strength behind it
                    element.Font!.DrawString(spriteBatch, character, characterPosition, characterColor * revealAlpha, characterShadowColor * revealAlpha, scale);
                }

                DrawDecorations(spriteBatch, decoration, segment.Effects, 1f, isBehindText: false);
                DrawDecorations(spriteBatch, decoration, hoverEffects, hoverStrength, isBehindText: false);
            }
        }

        /// <summary>Where and in what color a segment's decorations are drawn, gathered so each decoration doesn't need them passed one by one.</summary>
        /// <param name="Width">How much of the segment the decorations span, which stops short of whatever a typewriter hasn't revealed yet.</param>
        /// <param name="InkColor">The color the segment's text is drawn in, already faded, which a decoration without a color of its own takes.</param>
        private readonly record struct DecorationContext(Element Element, TextSegment Segment, Vector2 LinePosition, float LineHeight, Color InkColor, float Scale, float Width);

        /// <summary>Draws the decorations among the effects that belong on one side of the text: a highlight or redaction behind it, an underline or strike over it.
        /// They stay where the text was laid out rather than following a moving character, so a waving word keeps a steady underline.
        /// </summary>
        /// <param name="strength">How strongly to draw them, being less than full while a link's hover effects are still easing in.</param>
        private static void DrawDecorations(SpriteBatch spriteBatch, DecorationContext context, IReadOnlyList<TextEffect>? effects, float strength, bool isBehindText)
        {
            if (effects is null || context.Width <= 0f || strength <= 0f)
            {
                return;
            }

            // Whole pixels, with the right edge rounded out, so neighbouring segments meet rather than leaving a hairline gap between them
            int left = (int)Math.Floor(context.LinePosition.X + context.Segment.OffsetX);
            int right = (int)Math.Ceiling(context.LinePosition.X + context.Segment.OffsetX + context.Width);
            int top = (int)Math.Round(context.LinePosition.Y);
            int thickness = Math.Max(1, (int)Math.Round(DECORATION_THICKNESS * context.Scale));

            foreach (TextEffect effect in effects)
            {
                Rectangle? area = null;
                Color color = effect.Colors.Count is 0 ? context.InkColor : effect.Colors[0] * context.Element.DrawAlpha;

                switch (effect.Type)
                {
                    case TextEffectType.Highlight when isBehindText:
                        area = new Rectangle(left, top, right - left, (int)Math.Round(context.LineHeight));
                        color = effect.Colors.Count is 0 ? _defaultHighlightColor * context.Element.DrawAlpha : color;
                        break;
                    case TextEffectType.Redact when isBehindText:
                        int redactTop = top + (int)Math.Round(context.LineHeight * REDACT_TOP);
                        area = new Rectangle(left, redactTop, right - left, top + (int)Math.Round(context.LineHeight * REDACT_BOTTOM) - redactTop);
                        break;
                    case TextEffectType.Underline when isBehindText is false:
                        area = new Rectangle(left, top + (int)Math.Round(context.LineHeight * UNDERLINE_POSITION), right - left, thickness);
                        break;
                    case TextEffectType.Strike when isBehindText is false:
                        area = new Rectangle(left, top + (int)Math.Round(context.LineHeight * STRIKE_POSITION) - thickness / 2, right - left, thickness);
                        break;
                }

                if (area is Rectangle drawnArea)
                {
                    spriteBatch.Draw(Game1.staminaRect, drawnArea, color * strength);
                }
            }
        }

        /// <summary>How much of a segment its decorations span, being all of it unless a typewriter is still revealing it, in which case they stop where the revealed text does.</summary>
        private static float GetDecorationWidth(Element element, TextSegment segment, double effectTime)
        {
            if (TextEffectHelper.HasEffect(segment.Effects, TextEffectType.Typewriter) is false || segment.Characters is null || segment.CharacterOffsets is null)
            {
                return segment.Width;
            }

            for (int index = 0; index < segment.Characters.Count; index++)
            {
                if (TypewriterHelper.GetRevealAlpha(element, segment.Effects!, segment.SourceStart + index, effectTime) <= 0f)
                {
                    return segment.CharacterOffsets[index] - segment.OffsetX;
                }
            }

            return segment.Width;
        }

        /// <summary>The color a character's effects give it, before the element's fade. False when no effect over it sets one.
        /// The segment's own effects go first, starting from the color the character would otherwise have, unless a hovered link's hover color is covering them.
        /// The link's hover effects go after, starting from whatever that left and blended in as they ease in.
        /// </summary>
        /// <param name="baseColor">The color the character would be drawn in without any effect, which is the link's hover color while one applies.</param>
        /// <param name="hasHoverColor">Whether a hovered link's hover color is in use, which covers the segment's own coloring effects.</param>
        private static bool TryGetEffectColor(TextSegment segment, IReadOnlyList<TextEffect>? hoverEffects, int position, double effectTime, double hoverTime, float hoverStrength, Color baseColor, bool hasHoverColor, out Color effectColor)
        {
            effectColor = baseColor;
            bool isColored = false;

            if (hasHoverColor is false && segment.Effects is not null && TextEffectHelper.GetColor(segment.Effects, position, effectTime, effectColor) is Color segmentEffectColor)
            {
                effectColor = segmentEffectColor;
                isColored = true;
            }

            if (hoverEffects is not null && TextEffectHelper.GetColor(hoverEffects, position, hoverTime, effectColor) is Color hoverEffectColor)
            {
                effectColor = Color.Lerp(effectColor, hoverEffectColor, hoverStrength);
                isColored = true;
            }

            return isColored;
        }

        /// <summary>The link element a segment belongs to. Null when it belongs to none.</summary>
        private static Element? GetLink(Element element, TextSegment segment)
        {
            if (segment.LinkIndex is not int linkIndex || linkIndex >= element.Children.Count)
            {
                return null;
            }

            return element.Children[linkIndex];
        }
    }
}
