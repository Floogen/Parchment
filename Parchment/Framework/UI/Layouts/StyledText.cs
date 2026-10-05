using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.UI.Layouts
{
    /// <summary>A stretch of <see cref="StyledText.Text"/> drawn in a color of its own, given as character offsets into it.</summary>
    public class ColorRun
    {
        public int Start { get; }
        public int Length { get; }
        public Color Color { get; }

        public int End => Start + Length;

        public ColorRun(int start, int length, Color color)
        {
            Start = start;
            Length = length;
            Color = color;
        }
    }

    /// <summary>A stretch of <see cref="StyledText.Text"/> that belongs to a link, given as character offsets into it.
    /// A link with another link inside it is cut into a run on either side, both carrying the same <see cref="Occurrence"/>.
    /// </summary>
    public class LinkRun
    {
        public int Start { get; }
        public int Length { get; }

        /// <summary>Which [link] in the authored text this is, counting only the ones that name a link the element defines. It is also the link's position in the element's <see cref="Models.Element.Children"/>.</summary>
        public int Occurrence { get; }

        public int End => Start + Length;

        public LinkRun(int start, int length, int occurrence)
        {
            Start = start;
            Length = length;
            Occurrence = occurrence;
        }
    }

    /// <summary>How a [typewriter] reveals its text, read from the tag's value.</summary>
    public class TypingOptions
    {
        /// <summary>How long each character takes to appear, in milliseconds.</summary>
        public float Speed { get; }

        /// <summary>How long to wait before the first character, in milliseconds. Counted from when the typewriter would otherwise have started.</summary>
        public float Delay { get; }

        /// <summary>Whether it starts as soon as its text is on screen, rather than waiting for the typewriters before it on the spread to finish.</summary>
        public bool IsImmediate { get; }

        /// <summary>How long each character takes to fade in once it appears, in milliseconds. Zero pops it in at once, the way the game's dialogue does.</summary>
        public float FadeDuration { get; }

        /// <summary>The sound cue played as characters appear (null for none).</summary>
        public string? Sound { get; }

        /// <summary>A game state query checked once when the typewriter would start. When it fails the text shows in full straight away, as though it had typed out. Null when it always types.</summary>
        public string? Condition { get; private init; }

        public TypingOptions(float speed, float delay, bool isImmediate, float fadeDuration, string? sound)
        {
            Speed = speed;
            Delay = delay;
            IsImmediate = isImmediate;
            FadeDuration = fadeDuration;
            Sound = sound;
        }

        /// <summary>A copy gated on the given condition, which comes from the tag's own "condition=" part rather than from the rest of its value.</summary>
        public TypingOptions WithCondition(string? condition)
        {
            return new TypingOptions(Speed, Delay, IsImmediate, FadeDuration, Sound) { Condition = condition };
        }
    }

    /// <summary>One effect opened by markup, such as a [wave], moving each character it covers at draw time without touching the layout.</summary>
    public class TextEffect
    {
        public TextEffectType Type { get; }

        /// <summary>How far the effect moves a character, in unscaled pixels multiplied by the text's scale.</summary>
        public float Amplitude { get; }

        /// <summary>How long one cycle of the effect takes, in milliseconds.</summary>
        public float Period { get; }

        /// <summary>Where the effect's tag opened in <see cref="StyledText.Text"/>. Each character's place in the effect is counted from here, so the effect carries on unbroken across a color change or a line break.</summary>
        public int Start { get; }

        /// <summary>How many characters of <see cref="StyledText.Text"/> the effect covers. Set once its closing tag is reached (or the text ends), which is what lets a gradient spread its colors across the whole of it.</summary>
        public int Length { get; set; }

        /// <summary>The colors an effect blends between, being a gradient's stops in order or the one color a pulse fades towards. Empty for an effect that only moves its characters.</summary>
        public IReadOnlyList<Color> Colors { get; }

        /// <summary>How a typewriter reveals its text. Null for every other effect.</summary>
        public TypingOptions? Typing { get; init; }

        /// <summary>Which typewriter in the element's text this is, counting from zero in the order they open. It is how the typewriter finds its progress again on the element, since the effect itself is rebuilt each time the text is laid out.</summary>
        public int TypingOrdinal { get; init; } = -1;

        public TextEffect(TextEffectType type, float amplitude, float period, int start) : this(type, amplitude, period, start, Array.Empty<Color>())
        {
        }

        public TextEffect(TextEffectType type, float amplitude, float period, int start, IReadOnlyList<Color> colors)
        {
            Type = type;
            Amplitude = amplitude;
            Period = period;
            Start = start;
            Colors = colors;
        }
    }

    /// <summary>A stretch of <see cref="StyledText.Text"/> under one set of effects, given as character offsets into it. Every effect still open over the stretch is listed, outermost first.</summary>
    public class EffectRun
    {
        public int Start { get; }
        public int Length { get; }
        public IReadOnlyList<TextEffect> Effects { get; }

        public int End => Start + Length;

        public EffectRun(int start, int length, IReadOnlyList<TextEffect> effects)
        {
            Start = start;
            Length = length;
            Effects = effects;
        }
    }

    /// <summary>An element's text with its tokens resolved and its markup taken out, holding where each color, link and effect applies alongside the plain text that is measured and drawn.</summary>
    public class StyledText
    {
        // Declared ahead of Empty, as static fields are set in the order they're written and Empty is built from this
        private static readonly IReadOnlyDictionary<int, IReadOnlyList<TextEffect>> _noLinkHoverEffects = new Dictionary<int, IReadOnlyList<TextEffect>>();

        public static readonly StyledText Empty = new StyledText(string.Empty, Array.Empty<ColorRun>(), Array.Empty<LinkRun>(), Array.Empty<EffectRun>());

        /// <summary>The text to draw, with its line breaks already normalized to a bare \n.</summary>
        public string Text { get; }

        /// <summary>The colored runs in order and never overlapping. Text outside every run is drawn in the element's own color.</summary>
        public IReadOnlyList<ColorRun> ColorRuns { get; }

        /// <summary>The linked runs in order and never overlapping. A link nested inside another takes over the text it covers.</summary>
        public IReadOnlyList<LinkRun> LinkRuns { get; }

        /// <summary>The runs under at least one effect, in order and never overlapping.</summary>
        public IReadOnlyList<EffectRun> EffectRuns { get; }

        /// <summary>The effects each link applies to its text while hovered, by the link's <see cref="LinkRun.Occurrence"/>. Each one starts where its link does and covers all of it,
        /// so a gradient spreads across the whole link even when a link nested inside cuts it in two. A link without hover effects has no entry.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyList<TextEffect>> LinkHoverEffects { get; }

        public StyledText(string text, IReadOnlyList<ColorRun> colorRuns, IReadOnlyList<LinkRun> linkRuns, IReadOnlyList<EffectRun> effectRuns) : this(text, colorRuns, linkRuns, effectRuns, _noLinkHoverEffects)
        {
        }

        public StyledText(string text, IReadOnlyList<ColorRun> colorRuns, IReadOnlyList<LinkRun> linkRuns, IReadOnlyList<EffectRun> effectRuns, IReadOnlyDictionary<int, IReadOnlyList<TextEffect>> linkHoverEffects)
        {
            Text = text;
            ColorRuns = colorRuns;
            LinkRuns = linkRuns;
            EffectRuns = effectRuns;
            LinkHoverEffects = linkHoverEffects;
        }
    }
}
