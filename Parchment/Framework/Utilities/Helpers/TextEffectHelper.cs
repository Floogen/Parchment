using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Layouts;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Works out where and in what color each character under a text effect is drawn at a given moment. Every effect is a pure function of the time and the character's place in it,
    /// so nothing is stored per frame and every effect on screen shares one clock.
    /// </summary>
    public static class TextEffectHelper
    {
        // How far each character trails the one before it, the same as the game's own SparklingText, which is what makes a wave travel along the text rather than bob it all at once
        public const float WAVE_CHARACTER_DELAY = 100f;

        // How long a link's hover effects take to ease in from rest once the cursor arrives, which keeps a wave from appearing mid-cycle
        public const float HOVER_EFFECT_EASE_IN_DURATION = 150f;

        // The hash axis scramble picks its glyphs on, apart from the two a shake moves along, so a shaking scramble doesn't change glyph in step with its jumps
        private const int SCRAMBLE_HASH_AXIS = 2;

        // The hash axis flicker picks its strengths on, apart from shake's and scramble's, so a flickering shake doesn't dim in step with its jumps
        private const int FLICKER_HASH_AXIS = 3;

        // The hash axes a sparkle reads, apart from every other effect's so a sparkle never lands in step with a shake, scramble or flicker on the same character
        private const int SPARKLE_PHASE_HASH_AXIS = 4;
        private const int SPARKLE_CHANCE_HASH_AXIS = 5;
        private const int SPARKLE_SPREAD_X_HASH_AXIS = 6;
        private const int SPARKLE_SPREAD_Y_HASH_AXIS = 7;

        // How far a pulsing glow dims at the low point of each period, as a share of its full strength
        public const float GLOW_PULSE_DEPTH = 0.6f;

        // How much of each period a shimmer's band spends crossing the text, with the text resting for the remainder so it reads as a glint rather than a constant scroll
        public const float SHIMMER_SWEEP_FRACTION = 0.5f;

        // The game's own rainbow from SparklingText, which hands each character the next color along
        private static readonly Color[] _rainbowColors = new Color[] { Color.Red, Color.Orange, Color.Yellow, Color.Chartreuse, Color.Green, Color.Cyan, Color.Blue, Color.Violet };

        /// <summary>One sparkle showing over a character right now.</summary>
        /// <param name="Frame">Which frame of the sparkle's animation to draw, counting from zero.</param>
        /// <param name="Spread">Where over the character it sits, with X from -1 (its left edge) to 1 (its right) and Y from 0 (the top of its line) to 1 (the bottom).</param>
        /// <param name="Color">The sparkle's tint, before any fade.</param>
        public readonly record struct Sparkle(int Frame, Vector2 Spread, Color Color);

        /// <summary>How far a character is moved from where it was laid out, adding up every effect over it.</summary>
        /// <param name="position">The character's position in <see cref="StyledText.Text"/>.</param>
        /// <param name="time">The animation clock, in milliseconds.</param>
        /// <param name="scale">The text's scale, which an effect's amplitude is multiplied by.</param>
        public static Vector2 GetOffset(IReadOnlyList<TextEffect> effects, int position, double time, float scale)
        {
            Vector2 offset = Vector2.Zero;

            foreach (TextEffect effect in effects)
            {
                int index = position - effect.Start;

                switch (effect.Type)
                {
                    case TextEffectType.Wave:
                        offset.Y += GetWaveOffset(effect, index, time) * scale;
                        break;
                    case TextEffectType.Shake:
                        // Kept to whole pixels, as a jitter that lands between them blurs rather than shakes
                        offset.X += MathF.Round(GetNoise(position, GetShakeSlot(effect, time), 0) * effect.Amplitude * scale);
                        offset.Y += MathF.Round(GetNoise(position, GetShakeSlot(effect, time), 1) * effect.Amplitude * scale);
                        break;
                    case TextEffectType.Bounce:
                        offset.Y -= GetBounceHeight(effect, index, time) * scale;
                        break;
                }
            }

            return offset;
        }

        /// <summary>How far a character is turned about its center, in radians, adding up every wobble over it.
        /// Each character rocks a beat behind the one before it, the same as a wave, so the rocking travels along the text rather than tipping it all at once.
        /// </summary>
        /// <param name="position">The character's position in <see cref="StyledText.Text"/>.</param>
        /// <param name="time">The animation clock, in milliseconds.</param>
        public static float GetRotation(IReadOnlyList<TextEffect> effects, int position, double time)
        {
            float rotation = 0f;

            foreach (TextEffect effect in effects)
            {
                if (effect.Type is TextEffectType.Wobble)
                {
                    double phase = Math.PI * 2d / effect.Period * -(time + (position - effect.Start) * WAVE_CHARACTER_DELAY);

                    rotation += MathHelper.ToRadians(effect.Amplitude) * (float)Math.Sin(phase);
                }
            }

            return rotation;
        }

        /// <summary>How strongly a character is drawn, from 0 for not at all to 1 for in full, multiplying together every flicker over it so a flicker inside another dims further.
        /// A fade dims a whole stretch at once rather than each character, so it is left to <see cref="GetStretchAlpha"/>.
        /// </summary>
        /// <param name="position">The character's position in <see cref="StyledText.Text"/>.</param>
        /// <param name="time">The animation clock, in milliseconds.</param>
        public static float GetAlpha(IReadOnlyList<TextEffect> effects, int position, double time)
        {
            float alpha = 1f;

            foreach (TextEffect effect in effects)
            {
                if (effect.Type is TextEffectType.Flicker)
                {
                    alpha *= GetFlickerAlpha(effect, position, time);
                }
            }

            return alpha;
        }

        /// <summary>How strongly a whole stretch is drawn, from 0 for not at all to 1 for in full, multiplying together every fade over it so a fade inside another dims further.
        /// Every character under a fade shares it, so it applies to a segment drawn whole and to the decorations on it as well as to each character.
        /// </summary>
        /// <param name="time">The animation clock, in milliseconds.</param>
        public static float GetStretchAlpha(IReadOnlyList<TextEffect>? effects, double time)
        {
            float alpha = 1f;

            if (effects is null)
            {
                return alpha;
            }

            foreach (TextEffect effect in effects)
            {
                if (effect.Type is TextEffectType.Fade)
                {
                    // Starts from full strength, so the text is readable the moment it appears and only then begins to dim
                    alpha *= 1f - (1f - effect.Amplitude) * GetPulseBlend(effect, time);
                }
            }

            return alpha;
        }

        /// <summary>The color the effects give a character (null when none of the effects over it sets one).
        /// Applied from the outermost effect in, each starting from the color the one around it left. A rainbow or gradient replaces that color while a pulse or a shimmer fades it towards its own,
        /// so a pulse inside a rainbow pulses the rainbow and a rainbow inside another rainbow keeps its own pace.
        /// </summary>
        /// <param name="position">The character's position in <see cref="StyledText.Text"/>.</param>
        /// <param name="time">The animation clock, in milliseconds.</param>
        /// <param name="baseColor">The color the character would be drawn in without any effect, before the element's fade. Its alpha carries through every effect, so a translucent run stays translucent.</param>
        public static Color? GetColor(IReadOnlyList<TextEffect> effects, int position, double time, Color baseColor)
        {
            Color currentColor = baseColor;
            bool isColored = false;

            foreach (TextEffect effect in effects)
            {
                int index = position - effect.Start;

                switch (effect.Type)
                {
                    case TextEffectType.Rainbow:
                        currentColor = GetRainbowColor(effect, index, time) * (currentColor.A / 255f);
                        isColored = true;
                        break;
                    case TextEffectType.Gradient when effect.Colors.Count >= 2:
                        currentColor = GetGradientColor(effect, index) * (currentColor.A / 255f);
                        isColored = true;
                        break;
                    case TextEffectType.Pulse when effect.Colors.Count >= 1:
                        currentColor = Color.Lerp(currentColor, effect.Colors[0], GetPulseBlend(effect, time));
                        isColored = true;
                        break;
                    case TextEffectType.Shimmer when effect.Colors.Count >= 1:
                        // The band's color takes on the alpha around it, so a glint across translucent text doesn't flash it opaque
                        currentColor = Color.Lerp(currentColor, effect.Colors[0] * (currentColor.A / 255f), GetShimmerBlend(effect, index, time));
                        isColored = true;
                        break;
                }
            }

            return isColored ? currentColor : null;
        }

        /// <summary>Whether an effect decorates the text with a line or box rather than moving or recoloring its characters, which lets a segment carrying only decorations still be drawn whole.</summary>
        public static bool IsDecoration(TextEffectType effectType)
        {
            return effectType is TextEffectType.Underline or TextEffectType.Strike or TextEffectType.Highlight or TextEffectType.Redact;
        }

        /// <summary>Whether any of the effects works on each character on its own, which is what has a segment drawn a character at a time. Decorations, bold, fade, outline and glow don't count, as each can draw a segment whole.</summary>
        public static bool HasCharacterEffects(IReadOnlyList<TextEffect>? effects)
        {
            if (effects is null)
            {
                return false;
            }

            foreach (TextEffect effect in effects)
            {
                if (IsDecoration(effect.Type) is false && effect.Type is not (TextEffectType.Bold or TextEffectType.Fade or TextEffectType.Outline or TextEffectType.Glow))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>How far a bold shifts its second copy of the text, in screen pixels, being the strongest bold among the effects and a hovered link's hover effects. Zero when there is none.
        /// Bolds that nest don't add up, as a shift wider than a stroke would split the letters into two rather than thicken them.
        /// </summary>
        /// <param name="hoverEffects">A hovered link's hover effects (null when there are none). A hover bold has no size to grow through, so it applies in full from the moment the cursor arrives.</param>
        /// <param name="scale">The text's scale, which a bold's strength is multiplied by.</param>
        public static int GetBoldness(IReadOnlyList<TextEffect>? effects, IReadOnlyList<TextEffect>? hoverEffects, float scale)
        {
            return Math.Max(GetBoldness(effects, scale), GetBoldness(hoverEffects, scale));
        }

        private static int GetBoldness(IReadOnlyList<TextEffect>? effects, float scale)
        {
            int boldness = 0;

            if (effects is null)
            {
                return boldness;
            }

            foreach (TextEffect effect in effects)
            {
                if (effect.Type is TextEffectType.Bold)
                {
                    // Whole pixels and never less than one, so a small scale still shows the bold rather than rounding it away
                    boldness = Math.Max(boldness, Math.Max(1, (int)Math.Round(effect.Amplitude * scale)));
                }
            }

            return boldness;
        }

        /// <summary>The innermost outline among the effects, being its color and how far its copies of the text are shifted in screen pixels. False when there is none.
        /// Outlines nested inside one another don't add up, as each would only be hidden under the one inside it.
        /// </summary>
        /// <param name="scale">The text's scale, which an outline's thickness is multiplied by.</param>
        public static bool TryGetOutline(IReadOnlyList<TextEffect>? effects, float scale, out Color color, out int thickness)
        {
            if (effects is not null)
            {
                for (int index = effects.Count - 1; index >= 0; index--)
                {
                    TextEffect effect = effects[index];

                    if (effect.Type is TextEffectType.Outline && effect.Colors.Count > 0)
                    {
                        color = effect.Colors[0];

                        // Whole pixels and never less than one, so a small scale still shows the outline rather than rounding it away
                        thickness = Math.Max(1, (int)Math.Round(effect.Amplitude * scale));
                        return true;
                    }
                }
            }

            color = Color.Transparent;
            thickness = 0;
            return false;
        }

        /// <summary>The innermost glow among the effects, being its color at this moment and how far its halo reaches in screen pixels. False when there is none.
        /// A glow with a period breathes, dimming towards <see cref="GLOW_PULSE_DEPTH"/> below its full strength and back once per period. Its color already carries that.
        /// </summary>
        /// <param name="scale">The text's scale, which a glow's radius is multiplied by.</param>
        /// <param name="time">The animation clock, in milliseconds.</param>
        public static bool TryGetGlow(IReadOnlyList<TextEffect>? effects, float scale, double time, out Color color, out int radius)
        {
            if (effects is not null)
            {
                for (int index = effects.Count - 1; index >= 0; index--)
                {
                    TextEffect effect = effects[index];

                    if (effect.Type is not TextEffectType.Glow || effect.Colors.Count is 0)
                    {
                        continue;
                    }

                    float strength = effect.Period > 0f ? 1f - GLOW_PULSE_DEPTH * GetPulseBlend(effect, time) : 1f;

                    color = effect.Colors[0] * strength;
                    radius = Math.Max(1, (int)Math.Round(effect.Amplitude * scale));
                    return true;
                }
            }

            color = Color.Transparent;
            radius = 0;
            return false;
        }

        /// <summary>Whether any of the effects is of the given type.</summary>
        public static bool HasEffect(IReadOnlyList<TextEffect>? effects, TextEffectType effectType)
        {
            if (effects is null)
            {
                return false;
            }

            foreach (TextEffect effect in effects)
            {
                if (effect.Type == effectType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The sparkle showing over a character right now, from the innermost sparkle effect over it that has one showing. False when none does.
        /// Each character gets its own chance of sparkling in every period, with its periods offset from its neighbours' so sparkles come and go at scattered moments rather than all at once.
        /// A sparkle plays through its frames once over its period, the way the game's own does, landing somewhere new over the character each time.
        /// </summary>
        /// <param name="position">The character's position in <see cref="StyledText.Text"/>.</param>
        /// <param name="frameCount">How many frames the sparkle's animation has.</param>
        public static bool TryGetSparkle(IReadOnlyList<TextEffect>? effects, int position, double time, int frameCount, out Sparkle sparkle)
        {
            if (effects is not null && frameCount > 0)
            {
                for (int index = effects.Count - 1; index >= 0; index--)
                {
                    TextEffect effect = effects[index];

                    if (effect.Type is not TextEffectType.Sparkle || effect.Colors.Count is 0)
                    {
                        continue;
                    }

                    double phase = GetHash(position, 0, SPARKLE_PHASE_HASH_AXIS) / (double)uint.MaxValue * effect.Period;
                    double step = (time + phase) / effect.Period;
                    long slot = (long)Math.Floor(step);

                    if (GetHash(position, slot, SPARKLE_CHANCE_HASH_AXIS) / (float)uint.MaxValue >= effect.Amplitude)
                    {
                        continue;
                    }

                    int frame = Math.Min(frameCount - 1, (int)((step - slot) * frameCount));

                    Vector2 spread = new Vector2(GetNoise(position, slot, SPARKLE_SPREAD_X_HASH_AXIS), GetHash(position, slot, SPARKLE_SPREAD_Y_HASH_AXIS) / (float)uint.MaxValue);

                    sparkle = new Sparkle(frame, spread, effect.Colors[0]);
                    return true;
                }
            }

            sparkle = default;
            return false;
        }

        /// <summary>The random glyph a scrambled character shows right now, from the innermost scramble over it that is still hiding it. False when none is, so the real character is drawn.</summary>
        /// <param name="position">The character's position in <see cref="StyledText.Text"/>.</param>
        public static bool TryGetScrambleGlyph(Element element, IReadOnlyList<TextEffect> effects, int position, double time, out string glyph)
        {
            for (int index = effects.Count - 1; index >= 0; index--)
            {
                TextEffect effect = effects[index];

                if (effect.Scramble is not ScrambleOptions scramble || effect.ScrambleGlyphs.Count is 0 || IsScrambled(element, effect, scramble, position, time) is false)
                {
                    continue;
                }

                long slot = (long)Math.Floor(time / scramble.Period);

                glyph = effect.ScrambleGlyphs[(int)(GetHash(position, slot, SCRAMBLE_HASH_AXIS) % (uint)effect.ScrambleGlyphs.Count)];
                return true;
            }

            glyph = string.Empty;
            return false;
        }

        /// <summary>Whether a scramble is hiding its text as a whole right now, being always for one without a condition and while its condition passes otherwise.
        /// Read at layout to keep a link the cursor can't read out of its reach, so it doesn't count a scramble that is partway through settling.
        /// </summary>
        public static bool IsHiding(Element element, TextEffect effect)
        {
            if (effect.Scramble is not ScrambleOptions scramble)
            {
                return effect.Type is TextEffectType.Redact;
            }

            return scramble.ConditionIndex is not int conditionIndex || conditionIndex >= element.InlineConditionResults.Count || element.InlineConditionResults[conditionIndex];
        }

        /// <summary>Whether one character of a scramble is still hidden. Once its condition stops passing the characters lock into place one after another, each a settle duration behind the one before it.
        /// A change the reader wasn't there to see has no time recorded, so the text is simply real.
        /// </summary>
        private static bool IsScrambled(Element element, TextEffect effect, ScrambleOptions scramble, int position, double time)
        {
            if (IsHiding(element, effect))
            {
                return true;
            }

            int conditionIndex = scramble.ConditionIndex!.Value;
            if (conditionIndex >= element.InlineConditionChangedAt.Count || element.InlineConditionChangedAt[conditionIndex] is not double changedAt)
            {
                return false;
            }

            return time < changedAt + (position - effect.Start + 1) * scramble.SettleDuration;
        }

        /// <summary>How strongly a link's hover effects apply, rising from none when the cursor arrives to all of them once they've eased in.</summary>
        /// <param name="hoverTime">How long the cursor has been over the link, in milliseconds.</param>
        public static float GetHoverStrength(double hoverTime)
        {
            float progress = Math.Clamp((float)(hoverTime / HOVER_EFFECT_EASE_IN_DURATION), 0f, 1f);

            // Eased rather than linear, so the motion settles into its full size instead of arriving at it with a jolt
            return 1f - (1f - progress) * (1f - progress);
        }

        /// <summary>Whether any of the effects sets a color, which a font that keeps its own color can't draw.</summary>
        public static bool HasColor(IReadOnlyList<TextEffect> effects)
        {
            foreach (TextEffect effect in effects)
            {
                if (effect.Type is TextEffectType.Rainbow or TextEffectType.Gradient or TextEffectType.Pulse or TextEffectType.Shimmer or TextEffectType.Outline or TextEffectType.Glow)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A gradient's color for one of its characters, spread evenly from its first stop at the first character to its last stop at the last.
        /// The whole stretch is measured, not each line, so a gradient that wraps carries on where the line before it left off.
        /// </summary>
        private static Color GetGradientColor(TextEffect effect, int index)
        {
            float fraction = effect.Length <= 1 ? 0f : Math.Clamp(index / (effect.Length - 1f), 0f, 1f);
            float scaledFraction = fraction * (effect.Colors.Count - 1);
            int stopIndex = Math.Min((int)scaledFraction, effect.Colors.Count - 2);

            return Color.Lerp(effect.Colors[stopIndex], effect.Colors[stopIndex + 1], scaledFraction - stopIndex);
        }

        /// <summary>How far along a pulse is towards its color (or a fade towards its lowest strength), easing from none to all of it and back once per period. The whole stretch pulses together, as a highlight rather than a ripple.</summary>
        private static float GetPulseBlend(TextEffect effect, double time)
        {
            return (float)((1d - Math.Cos(Math.PI * 2d * time / effect.Period)) / 2d);
        }

        /// <summary>How strongly a flickering character is drawn right now, drifting between the flicker's lowest strength and full.
        /// Each character picks a new strength every period and blends towards it rather than snapping, so it wavers like a candle instead of blinking.
        /// Every character is picked on its own, so neighbours never dim in step.
        /// </summary>
        private static float GetFlickerAlpha(TextEffect effect, int position, double time)
        {
            double step = time / effect.Period;
            long slot = (long)Math.Floor(step);
            float progress = (float)(step - slot);

            float fromStrength = GetHash(position, slot, FLICKER_HASH_AXIS) / (float)uint.MaxValue;
            float toStrength = GetHash(position, slot + 1, FLICKER_HASH_AXIS) / (float)uint.MaxValue;

            // Smoothed so each blend starts and ends at rest, which keeps the change between picks from showing as a kink
            float blend = progress * progress * (3f - 2f * progress);
            float strength = fromStrength + (toStrength - fromStrength) * blend;

            return effect.Amplitude + (1f - effect.Amplitude) * strength;
        }

        /// <summary>How strongly a shimmer's band covers one of its characters, being all of it at the band's center and easing to none at its edges.
        /// The band starts just before the first character and finishes just past the last within <see cref="SHIMMER_SWEEP_FRACTION"/> of each period, so it enters and leaves rather than appearing on the text.
        /// The whole stretch is measured, not each line, so a band that reaches the end of one line carries on at the start of the next.
        /// </summary>
        private static float GetShimmerBlend(TextEffect effect, int index, double time)
        {
            float width = effect.Amplitude;
            float sweepProgress = (float)((time - Math.Floor(time / effect.Period) * effect.Period) / (effect.Period * SHIMMER_SWEEP_FRACTION));

            if (sweepProgress >= 1f)
            {
                return 0f;
            }

            float center = -width + sweepProgress * (effect.Length - 1 + width * 2f);
            float coverage = 1f - Math.Abs(index - center) / width;

            if (coverage <= 0f)
            {
                return 0f;
            }

            // Smoothed so the band's edges fade in and out rather than stepping from one character to the next
            return coverage * coverage * (3f - 2f * coverage);
        }

        /// <summary>How high a bouncing character is lifted, rising from where it was laid out and landing back on it once per period rather than dipping below.
        /// Each character trails the one before it the way a wave's does, so the hops travel along the text in the same direction.
        /// </summary>
        private static float GetBounceHeight(TextEffect effect, int index, double time)
        {
            double phase = Math.PI / effect.Period * (time + index * WAVE_CHARACTER_DELAY);

            return (float)(effect.Amplitude * Math.Abs(Math.Sin(phase)));
        }

        /// <summary>The game's rainbow, one color further along for each character, blended between neighbours as it cycles once per period.
        /// A period of zero holds it still, which is exactly how the game draws its own rainbow text. The colors travel the same way a wave does.
        /// </summary>
        private static Color GetRainbowColor(TextEffect effect, int index, double time)
        {
            double step = index;

            if (effect.Period > 0f)
            {
                step += time / effect.Period * _rainbowColors.Length;
            }

            int colorIndex = (int)Math.Floor(step);
            float blend = (float)(step - colorIndex);

            return Color.Lerp(GetRainbowColorAt(colorIndex), GetRainbowColorAt(colorIndex + 1), blend);
        }

        private static Color GetRainbowColorAt(int colorIndex)
        {
            int wrappedIndex = colorIndex % _rainbowColors.Length;

            return _rainbowColors[wrappedIndex < 0 ? wrappedIndex + _rainbowColors.Length : wrappedIndex];
        }

        /// <summary>Which of the shake's steps the clock is on, each lasting one period. A character holds still within a step and jumps between them.</summary>
        private static long GetShakeSlot(TextEffect effect, double time)
        {
            return (long)Math.Floor(time / effect.Period);
        }

        /// <summary>A value between -1 and 1 that is always the same for the same character, step and axis.
        /// That lets a shake be worked out fresh on every frame rather than remembering where each character was put.
        /// </summary>
        private static float GetNoise(int position, long slot, int axis)
        {
            return GetHash(position, slot, axis) / (float)uint.MaxValue * 2f - 1f;
        }

        /// <summary>A number that is always the same for the same character, step and axis, which every random looking effect is drawn from. The axis keeps two effects on the same character from moving in step.</summary>
        private static uint GetHash(int position, long slot, int axis)
        {
            unchecked
            {
                uint hash = (uint)position * 374761393u + (uint)slot * 668265263u + (uint)axis * 2246822519u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;

                return hash;
            }
        }

        /// <summary>The game's wave, with each character a beat behind the one before it.
        /// The game counts its phase down from the text's remaining time, which this mirrors by running the clock backwards, so the wave travels the same way it does in game.
        /// </summary>
        private static float GetWaveOffset(TextEffect effect, int index, double time)
        {
            double phase = Math.PI * 2d / effect.Period * -(time + index * WAVE_CHARACTER_DELAY);

            return (float)(effect.Amplitude * Math.Sin(phase));
        }
    }
}
