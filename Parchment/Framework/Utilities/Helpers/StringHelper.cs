using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Layouts;
using StardewModdingAPI;
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

        // Scramble glyph widths by font, glyph and scale, filled as they're first drawn
        private static readonly Dictionary<(IFont Font, string Glyph, float Scale), float> _glyphWidths = new Dictionary<(IFont Font, string Glyph, float Scale), float>();

        // Glyph sizes by font and glyph at a scale of 1, filled as popping characters are first drawn. A pop's scale changes every frame, so the size is kept unscaled and multiplied out
        private static readonly Dictionary<(IFont Font, string Glyph), Vector2> _glyphSizes = new Dictionary<(IFont Font, string Glyph), Vector2>();

        // Where the game's own SparklingText takes its sparkles from, being six frames side by side starting at the source rectangle
        private const string SPARKLE_TEXTURE = "TileSheets/animations";
        private const int SPARKLE_FRAME_COUNT = 6;
        private static readonly Rectangle _sparkleSource = new Rectangle(0, 704, 64, 64);

        // Parsed once rather than every frame, as the sheet is looked up each time a segment with sparkles draws
        private static IAssetName? _sparkleAssetName;

        // The sparkles over a segment, gathered while its characters draw and drawn after them all so a sparkle is never covered by the letter beside it
        private static readonly List<(Vector2 Center, int Frame, Color Color)> _pendingSparkles = new List<(Vector2 Center, int Frame, Color Color)>();

        // How strongly a glow's innermost ring is drawn, each ring further out weaker until the last barely shows. The rings overlap, so the halo builds up stronger near the letters
        private const float GLOW_STRENGTH = 0.35f;

        // The most rings a glow is drawn in, spread evenly out to its radius, which keeps a wide or scaled up glow from costing a ring for every pixel of it
        private const int GLOW_MAX_RINGS = 4;

        // Every way an outline's or a glow's copies of the text are shifted, diagonals included so its corners are filled
        private static readonly Vector2[] _haloDirections = new Vector2[] { new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(-1f, 1f), new Vector2(0f, 1f), new Vector2(1f, 1f) };

        // The rings of copies drawn behind the segment being drawn, each a color and how far out it sits, in the order they go down: glows from the outside in and then outlines
        private static readonly List<(Color Color, int Distance)> _haloLayers = new List<(Color Color, int Distance)>();

        // A haloed segment's characters, gathered while it draws so every halo goes down before any letter, keeping one letter's outline or glow off the letter beside it
        private static readonly List<(string Text, Vector2 Position, Color Color, Color ShadowColor, float Alpha, float Rotation, float Scale)> _pendingHaloedCharacters = new List<(string Text, Vector2 Position, Color Color, Color ShadowColor, float Alpha, float Rotation, float Scale)>();

        public static void DrawLines(SpriteBatch spriteBatch, Element element, WrappedText wrappedText, Rectangle bounds, AlignmentType alignment, Color textColor, float scale)
        {
            // Every text element draws through here, so this is the only place text has to be told about a fade
            Color fadedColor = textColor * element.DrawAlpha;

            // Resolved against the faded color, so a default shadow follows the text down while a given one keeps the strength it was given
            Color shadowColor = element.GetShadowColor(fadedColor);

            // Read once for the whole element, so every moving character on it is drawn at the same moment
            double effectTime = AnimationHelper.GetAnimationTime();

            // Noted so a condition that changes while the reader is looking can be told apart from one that changed off screen
            element.LastDrawnAt = effectTime;

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
        /// A bold draws the segment's text twice, the second copy shifted right, which works the same whether the segment is drawn whole or a character at a time.
        /// A fade dims the whole segment together with its decorations, which works the same whether the segment is drawn whole or a character at a time.
        /// An outline draws the text in its color shifted every way around before the text itself, which then goes without its shadow as the outline takes its place. SpriteText outlines its own letters, so an outline is left out there.
        /// A glow draws faint rings of the same copies further out behind everything else, keeping the text's shadow. SpriteText keeps its own color, so a glow is left out there too.
        /// A wobble turns each character about its own center, its shadow and any outline or glow turning with it. SpriteText can't turn its letters, so they stay upright there.
        /// A pop grows each character about its own center, its shadow and any outline or glow growing with it, without moving the letters around it.
        /// A sparkle is drawn over its character after the whole segment, following the character as it moves and fading with it. It is the game's own sparkle, drawn at the text's scale.
        /// A character a typewriter hasn't reached yet is left out. One it is fading in is drawn at the strength it has reached, which a flicker dims further.
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
                int boldness = TextEffectHelper.GetBoldness(segment.Effects, hoverEffects, scale);

                // Eased in with the rest of the hover effects, so a fade on hover starts from full strength rather than dropping the moment the cursor arrives
                float stretchAlpha = TextEffectHelper.GetStretchAlpha(segment.Effects, effectTime) * MathHelper.Lerp(1f, TextEffectHelper.GetStretchAlpha(hoverEffects, hoverTime), hoverStrength);

                DecorationContext decoration = new DecorationContext(element, segment, linePosition, lineHeight, segmentColor, scale, GetDecorationWidth(element, segment, effectTime));

                DrawDecorations(spriteBatch, decoration, segment.Effects, stretchAlpha, isBehindText: true);
                DrawDecorations(spriteBatch, decoration, hoverEffects, hoverStrength * stretchAlpha, isBehindText: true);

                // A redacted segment is only its bar, so nothing of the text underneath is drawn
                if (TextEffectHelper.HasEffect(segment.Effects, TextEffectType.Redact))
                {
                    continue;
                }

                bool hasCharacterEffects = TextEffectHelper.HasCharacterEffects(segment.Effects) || TextEffectHelper.HasCharacterEffects(hoverEffects);

                Texture2D? sparkleTexture = TextEffectHelper.HasEffect(segment.Effects, TextEffectType.Sparkle) || TextEffectHelper.HasEffect(hoverEffects, TextEffectType.Sparkle) ? GetSparkleTexture() : null;
                _pendingSparkles.Clear();

                bool isOutlined = false;
                bool isHaloed = keepsOwnColor is false && CollectHalos(segment.Effects, hoverEffects, scale, effectTime, hoverTime, hoverStrength, out isOutlined);

                if (hasCharacterEffects is false || segment.Characters is null || segment.CharacterOffsets is null)
                {
                    Vector2 segmentPosition = new Vector2(linePosition.X + segment.OffsetX, linePosition.Y);

                    if (isHaloed)
                    {
                        DrawHalos(spriteBatch, element, segment.Text, segmentPosition, stretchAlpha, scale, boldness);
                    }

                    element.Font!.DrawString(spriteBatch, segment.Text, segmentPosition, segmentColor * stretchAlpha, isOutlined ? Color.Transparent : segmentShadowColor * stretchAlpha, scale, boldness);

                    DrawDecorations(spriteBatch, decoration, segment.Effects, stretchAlpha, isBehindText: false);
                    DrawDecorations(spriteBatch, decoration, hoverEffects, hoverStrength * stretchAlpha, isBehindText: false);
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

                    float characterAlpha = revealAlpha * stretchAlpha * (segment.Effects is null ? 1f : TextEffectHelper.GetAlpha(segment.Effects, position, effectTime));

                    // Eased in with the rest of the hover effects, so a flicker on hover starts from full strength rather than dropping the moment the cursor arrives
                    if (hoverEffects is not null)
                    {
                        characterAlpha *= MathHelper.Lerp(1f, TextEffectHelper.GetAlpha(hoverEffects, position, hoverTime), hoverStrength);
                    }

                    Vector2 effectOffset = segment.Effects is null ? Vector2.Zero : TextEffectHelper.GetOffset(segment.Effects, position, effectTime, scale);

                    if (hoverEffects is not null)
                    {
                        effectOffset += TextEffectHelper.GetOffset(hoverEffects, position, hoverTime, scale) * hoverStrength;
                    }

                    float rotation = segment.Effects is null ? 0f : TextEffectHelper.GetRotation(segment.Effects, position, effectTime);

                    if (hoverEffects is not null)
                    {
                        rotation += TextEffectHelper.GetRotation(hoverEffects, position, hoverTime) * hoverStrength;
                    }

                    float layoutX = segment.CharacterOffsets[index];
                    float slotWidth = (index + 1 < segment.CharacterOffsets.Count ? segment.CharacterOffsets[index + 1] : segment.OffsetX + segment.Width) - layoutX;
                    float characterX = layoutX;
                    string drawnCharacter = character;

                    // A scrambled character shows a random glyph centred in the room its real one was laid out with, so a wider glyph spills evenly rather than into one neighbour
                    if (segment.Effects is not null && TextEffectHelper.TryGetScrambleGlyph(element, segment.Effects, position, effectTime, out string glyph))
                    {
                        characterX += (slotWidth - GetGlyphWidth(element.Font!, glyph, scale)) / 2f;
                        drawnCharacter = glyph;
                    }

                    Vector2 characterPosition = new Vector2(linePosition.X + characterX + effectOffset.X, linePosition.Y + effectOffset.Y);

                    float popScale = segment.Effects is null ? 1f : TextEffectHelper.GetScale(segment.Effects, position, effectTime);

                    if (hoverEffects is not null)
                    {
                        popScale *= MathHelper.Lerp(1f, TextEffectHelper.GetScale(hoverEffects, position, hoverTime), hoverStrength);
                    }

                    float characterScale = scale * popScale;

                    // Grown about its own center, so a popping letter swells in place rather than spreading right and down
                    if (popScale != 1f)
                    {
                        characterPosition += GetGlyphSize(element.Font!, drawnCharacter) * (scale - characterScale) / 2f;
                    }

                    Color characterColor = segmentColor;
                    Color characterShadowColor = segmentShadowColor;

                    if (keepsOwnColor is false && TryGetEffectColor(segment, hoverEffects, position, effectTime, hoverTime, hoverStrength, runColor ?? element.TextColor, hoveredLinkColor is not null, out Color effectColor))
                    {
                        // Faded with the element, so one that is fading out takes its effect colors down with it
                        characterColor = effectColor * element.DrawAlpha;
                        characterShadowColor = element.GetShadowColor(characterColor);
                    }

                    if (isHaloed)
                    {
                        _pendingHaloedCharacters.Add((drawnCharacter, characterPosition, characterColor * characterAlpha, isOutlined ? Color.Transparent : characterShadowColor * characterAlpha, characterAlpha, rotation, characterScale));
                    }
                    else
                    {
                        // Faded together with its shadow, so a character fading in (or flickering) doesn't leave its shadow standing at full strength behind it
                        element.Font!.DrawString(spriteBatch, drawnCharacter, characterPosition, characterColor * characterAlpha, characterShadowColor * characterAlpha, characterScale, boldness, rotation);
                    }

                    if (sparkleTexture is not null)
                    {
                        Vector2 slotCenter = new Vector2(linePosition.X + layoutX + slotWidth / 2f + effectOffset.X, linePosition.Y + effectOffset.Y);
                        float sparkleAlpha = element.DrawAlpha * characterAlpha;

                        QueueSparkle(segment.Effects, position, effectTime, slotCenter, slotWidth, lineHeight, sparkleAlpha);
                        QueueSparkle(hoverEffects, position, hoverTime, slotCenter, slotWidth, lineHeight, sparkleAlpha * hoverStrength);
                    }
                }

                if (_pendingHaloedCharacters.Count > 0)
                {
                    DrawHaloedCharacters(spriteBatch, element, boldness);
                }

                DrawDecorations(spriteBatch, decoration, segment.Effects, stretchAlpha, isBehindText: false);
                DrawDecorations(spriteBatch, decoration, hoverEffects, hoverStrength * stretchAlpha, isBehindText: false);

                if (sparkleTexture is not null)
                {
                    DrawSparkles(spriteBatch, sparkleTexture, scale);
                }
            }
        }

        /// <summary>Gathers the rings of copies drawn behind the segment into <see cref="_haloLayers"/>: the segment's glow and a hovered link's, then the segment's outline and a hovered link's.
        /// A hovered link's are faded by how far its hover effects have eased in. False when there are none.
        /// </summary>
        /// <param name="isOutlined">Whether any outline is among them, which has the text drawn without its shadow.</param>
        private static bool CollectHalos(IReadOnlyList<TextEffect>? effects, IReadOnlyList<TextEffect>? hoverEffects, float scale, double effectTime, double hoverTime, float hoverStrength, out bool isOutlined)
        {
            _haloLayers.Clear();
            _pendingHaloedCharacters.Clear();

            if (TextEffectHelper.TryGetGlow(effects, scale, effectTime, out Color glowColor, out int glowRadius))
            {
                AddGlowRings(glowColor, glowRadius);
            }

            if (hoverStrength > 0f && TextEffectHelper.TryGetGlow(hoverEffects, scale, hoverTime, out Color hoverGlowColor, out int hoverGlowRadius))
            {
                AddGlowRings(hoverGlowColor * hoverStrength, hoverGlowRadius);
            }

            int glowLayerCount = _haloLayers.Count;

            if (TextEffectHelper.TryGetOutline(effects, scale, out Color outlineColor, out int outlineThickness))
            {
                _haloLayers.Add((outlineColor, outlineThickness));
            }

            if (hoverStrength > 0f && TextEffectHelper.TryGetOutline(hoverEffects, scale, out Color hoverOutlineColor, out int hoverOutlineThickness))
            {
                _haloLayers.Add((hoverOutlineColor * hoverStrength, hoverOutlineThickness));
            }

            isOutlined = _haloLayers.Count > glowLayerCount;
            return _haloLayers.Count > 0;
        }

        /// <summary>Adds a glow's rings from the outside in, so each nearer and stronger ring goes over the fainter one beyond it. The rings are spread evenly out to the radius, at most <see cref="GLOW_MAX_RINGS"/> of them.</summary>
        private static void AddGlowRings(Color color, int radius)
        {
            int ringCount = Math.Min(radius, GLOW_MAX_RINGS);

            for (int ring = ringCount; ring >= 1; ring--)
            {
                int distance = Math.Max(1, (int)Math.Round(radius * ring / (float)ringCount));
                float ringStrength = GLOW_STRENGTH * (1f - (ring - 1f) / ringCount);

                _haloLayers.Add((color * ringStrength, distance));
            }
        }

        /// <summary>Draws the text once in each halo layer's color for every direction around it, without a shadow. Each copy is bold the same as the text, so an outline or glow wraps the thickened letters.</summary>
        /// <param name="alpha">How strongly the text itself is drawn, which its halo follows so it fades and flickers with it.</param>
        /// <param name="rotation">How far the text is turned, which every copy follows so a wobbling letter's halo rocks with it.</param>
        private static void DrawHalos(SpriteBatch spriteBatch, Element element, string text, Vector2 position, float alpha, float scale, int boldness, float rotation = 0f)
        {
            foreach ((Color color, int distance) in _haloLayers)
            {
                Color layerColor = color * element.DrawAlpha * alpha;

                foreach (Vector2 direction in _haloDirections)
                {
                    element.Font!.DrawString(spriteBatch, text, position + direction * distance, layerColor, Color.Transparent, scale, boldness, rotation);
                }
            }
        }

        /// <summary>Draws the gathered characters of a haloed segment, every halo first and then every letter over them, without a shadow when the segment is outlined.</summary>
        private static void DrawHaloedCharacters(SpriteBatch spriteBatch, Element element, int boldness)
        {
            foreach ((string text, Vector2 position, Color _, Color _, float alpha, float rotation, float characterScale) in _pendingHaloedCharacters)
            {
                DrawHalos(spriteBatch, element, text, position, alpha, characterScale, boldness, rotation);
            }

            foreach ((string text, Vector2 position, Color color, Color shadowColor, float _, float rotation, float characterScale) in _pendingHaloedCharacters)
            {
                element.Font!.DrawString(spriteBatch, text, position, color, shadowColor, characterScale, boldness, rotation);
            }

            _pendingHaloedCharacters.Clear();
        }

        /// <summary>Adds the sparkle showing over a character right now, if any, to the ones drawn once the segment's characters are all down.</summary>
        /// <param name="slotCenter">The middle of the room the character was laid out with, moved along with it, at the top of its line.</param>
        /// <param name="alpha">How strongly to draw the sparkle, carrying the element's fade and the character's own.</param>
        private static void QueueSparkle(IReadOnlyList<TextEffect>? effects, int position, double time, Vector2 slotCenter, float slotWidth, float lineHeight, float alpha)
        {
            if (alpha <= 0f || TextEffectHelper.TryGetSparkle(effects, position, time, SPARKLE_FRAME_COUNT, out TextEffectHelper.Sparkle sparkle) is false)
            {
                return;
            }

            // Anywhere across the character's room and down the line's height, the way the game scatters its sparkles across the whole of its text
            Vector2 center = new Vector2(slotCenter.X + sparkle.Spread.X * slotWidth / 2f, slotCenter.Y + sparkle.Spread.Y * lineHeight);

            // Whole pixels, so a sparkle drawn between them doesn't blur
            _pendingSparkles.Add((new Vector2(MathF.Round(center.X), MathF.Round(center.Y)), sparkle.Frame, sparkle.Color * alpha));
        }

        /// <summary>Draws the gathered sparkles, each centred on where it landed. The frames sit side by side, so each is the one before it moved a frame's width along.</summary>
        /// <param name="scale">The text's scale. The game draws its sparkles at their own size, so text at a scale of 1 matches it.</param>
        private static void DrawSparkles(SpriteBatch spriteBatch, Texture2D sparkleTexture, float scale)
        {
            Vector2 origin = new Vector2(_sparkleSource.Width / 2, _sparkleSource.Height / 2);

            foreach ((Vector2 center, int frame, Color color) in _pendingSparkles)
            {
                Rectangle source = new Rectangle(_sparkleSource.X + frame * _sparkleSource.Width, _sparkleSource.Y, _sparkleSource.Width, _sparkleSource.Height);

                spriteBatch.Draw(sparkleTexture, center, source, color, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            _pendingSparkles.Clear();
        }

        /// <summary>The game's animations sheet the sparkle frames are on, so a mod retexturing the game's sparkles retextures these too.</summary>
        private static Texture2D GetSparkleTexture()
        {
            _sparkleAssetName ??= Parchment.modHelper.GameContent.ParseAssetName(SPARKLE_TEXTURE);

            return Parchment.modHelper.GameContent.Load<Texture2D>(_sparkleAssetName);
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

        /// <summary>How wide a scramble glyph draws, measured once per font and scale and kept, as the same few glyphs are measured every frame while text is scrambled.</summary>
        private static Vector2 GetGlyphSize(IFont font, string glyph)
        {
            (IFont Font, string Glyph) key = (font, glyph);

            if (_glyphSizes.TryGetValue(key, out Vector2 size) is false)
            {
                size = font.MeasureString(glyph, 1f);
                _glyphSizes[key] = size;
            }

            return size;
        }

        private static float GetGlyphWidth(IFont font, string glyph, float scale)
        {
            (IFont Font, string Glyph, float Scale) key = (font, glyph, scale);

            if (_glyphWidths.TryGetValue(key, out float width) is false)
            {
                width = font.MeasureString(glyph, scale).X;
                _glyphWidths[key] = width;
            }

            return width;
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
