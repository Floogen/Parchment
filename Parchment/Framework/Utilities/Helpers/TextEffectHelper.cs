using Microsoft.Xna.Framework;
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

        // The game's own rainbow from SparklingText, which hands each character the next color along
        private static readonly Color[] _rainbowColors = new Color[] { Color.Red, Color.Orange, Color.Yellow, Color.Chartreuse, Color.Green, Color.Cyan, Color.Blue, Color.Violet };

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

        /// <summary>The color the effects give a character (null when none of the effects over it sets one).
        /// Applied from the outermost effect in, each starting from the color the one around it left. A rainbow or gradient replaces that color while a pulse fades it towards its own,
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
                }
            }

            return isColored ? currentColor : null;
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
                if (effect.Type is TextEffectType.Rainbow or TextEffectType.Gradient or TextEffectType.Pulse)
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

        /// <summary>How far along a pulse is towards its color, easing from none to all of it and back once per period. The whole stretch pulses together, as a highlight rather than a ripple.</summary>
        private static float GetPulseBlend(TextEffect effect, double time)
        {
            return (float)((1d - Math.Cos(Math.PI * 2d * time / effect.Period)) / 2d);
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
            unchecked
            {
                uint hash = (uint)position * 374761393u + (uint)slot * 668265263u + (uint)axis * 2246822519u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;

                return hash / (float)uint.MaxValue * 2f - 1f;
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
