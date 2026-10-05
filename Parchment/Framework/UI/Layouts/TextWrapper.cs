using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.UI.Layouts
{
    /// <summary>A stretch of a wrapped line drawn in one color, belonging to at most one link and under one set of effects, cut wherever any of those changes.</summary>
    public class TextSegment
    {
        public string Text { get; }

        /// <summary>The color the segment is drawn in (null for the element's own text color).</summary>
        public Color? Color { get; }

        /// <summary>The link the segment belongs to, as its position in the element's <see cref="Element.Children"/> (null when it belongs to none).</summary>
        public int? LinkIndex { get; }

        /// <summary>How far into the line the segment starts, in the same scaled pixels as <see cref="WrappedLine.Size"/>.</summary>
        public float OffsetX { get; }

        /// <summary>How wide the segment is, in the same scaled pixels as <see cref="OffsetX"/>.</summary>
        public float Width { get; }

        /// <summary>Where the segment starts in <see cref="StyledText.Text"/>, which is how each character finds its place in an effect.</summary>
        public int SourceStart { get; }

        /// <summary>The effects moving the segment's characters, outermost first (null when it sits still).</summary>
        public IReadOnlyList<TextEffect>? Effects { get; }

        /// <summary>Each of the segment's characters as its own string, so an effect can draw them one at a time without building a string per character every frame. Null when <see cref="Effects"/> is.</summary>
        public IReadOnlyList<string>? Characters { get; }

        /// <summary>How far into the line each character starts, in the same scaled pixels as <see cref="OffsetX"/>. Null when <see cref="Effects"/> is.
        /// Measured as everything on the line before the character rather than by adding up characters on their own, so a moving stretch takes exactly the room the layout gave it.
        /// </summary>
        public IReadOnlyList<float>? CharacterOffsets { get; }

        public TextSegment(string text, Color? color, int? linkIndex, float offsetX, float width) : this(text, color, linkIndex, offsetX, width, 0, null, null, null)
        {
        }

        public TextSegment(string text, Color? color, int? linkIndex, float offsetX, float width, int sourceStart, IReadOnlyList<TextEffect>? effects, IReadOnlyList<string>? characters, IReadOnlyList<float>? characterOffsets)
        {
            Text = text;
            Color = color;
            LinkIndex = linkIndex;
            OffsetX = offsetX;
            Width = width;
            SourceStart = sourceStart;
            Effects = effects;
            Characters = characters;
            CharacterOffsets = characterOffsets;
        }
    }

    public class WrappedLine
    {
        public string Text { get; }
        public Vector2 Size { get; }

        /// <summary>The line cut wherever its color, link or effects change (null when the whole line is drawn in the element's own color, belongs to no link and sits still).</summary>
        public IReadOnlyList<TextSegment>? Segments { get; }

        /// <summary>Whether any of the line's segments is moved by an effect.</summary>
        public bool HasEffects { get; }

        public WrappedLine(string text, Vector2 size) : this(text, size, null)
        {
        }

        public WrappedLine(string text, Vector2 size, IReadOnlyList<TextSegment>? segments)
        {
            Text = text;
            Size = size;
            Segments = segments;

            if (segments is not null)
            {
                foreach (TextSegment segment in segments)
                {
                    if (segment.Effects is not null)
                    {
                        HasEffects = true;
                        break;
                    }
                }
            }
        }
    }

    public class WrappedText
    {
        public IReadOnlyList<WrappedLine> Lines { get; }
        public Vector2 Size { get; }

        public WrappedText(IReadOnlyList<WrappedLine> lines, Vector2 size)
        {
            Lines = lines;
            Size = size;
        }

        /// <summary>
        /// Returns a copy containing only the leading lines that fit within the given height.
        /// Whole lines only; a line that would be partially cut is dropped entirely.
        /// </summary>
        public WrappedText TruncateToHeight(float maximumHeight)
        {
            if (Size.Y <= maximumHeight)
            {
                return this;
            }

            List<WrappedLine> keptLines = new List<WrappedLine>();
            float currentHeight = 0f;
            float maximumLineWidth = 0f;

            foreach (WrappedLine line in Lines)
            {
                if (currentHeight + line.Size.Y > maximumHeight)
                {
                    break;
                }

                keptLines.Add(line);
                currentHeight += line.Size.Y;
                maximumLineWidth = Math.Max(maximumLineWidth, line.Size.X);
            }

            return new WrappedText(keptLines, new Vector2(maximumLineWidth, currentHeight));
        }
    }

    public static class TextWrapper
    {
        private const char HYPHEN = '-';
        private const string BLANK_LINE_MEASURE_TEXT = " ";

        /// <summary>What every step of a wrap reads, gathered so the steps don't each carry the same handful of arguments.</summary>
        private class WrapState
        {
            public List<WrappedLine> Lines { get; } = new List<WrappedLine>();
            public IReadOnlyList<ColorRun> ColorRuns { get; init; } = Array.Empty<ColorRun>();
            public IReadOnlyList<LinkRun> LinkRuns { get; init; } = Array.Empty<LinkRun>();
            public IReadOnlyList<EffectRun> EffectRuns { get; init; } = Array.Empty<EffectRun>();
            public IFont Font { get; init; } = null!;
            public float MaxWidth { get; init; }
            public float Scale { get; init; }
            public bool HyphenateBrokenWords { get; init; }
        }

        /// <summary>Resolves an element's tokens and color markup before wrapping its text in the given font.
        /// Every text element measures through here, so markup reads the same on a Paragraph as it does on a Banner, a Button or an Image's caption.
        /// </summary>
        public static WrappedText WrapElementText(string? text, Element element, IFont font, float maxWidth, float scale)
        {
            StyledText styledText = TextMarkupHelper.Resolve(text, element);

            if (font is SpriteTextAdapter && (styledText.ColorRuns.Count is not 0 || styledText.EffectRuns.Any(run => TextEffectHelper.HasColor(run.Effects))))
            {
                Parchment.monitor.LogOnce($"'{text}' has [color], [rainbow], [gradient] or [pulse] markup but draws in SpriteText, which keeps its own color, so the coloring is ignored.", LogLevel.Warn);
            }

            return Wrap(styledText, font, maxWidth, scale);
        }

        public static WrappedText Wrap(string text, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords = false)
        {
            return Wrap(new StyledText(text?.Replace("\r\n", "\n") ?? string.Empty, Array.Empty<ColorRun>(), Array.Empty<LinkRun>(), Array.Empty<EffectRun>()), font, maxWidth, scale, hyphenateBrokenWords);
        }

        public static WrappedText Wrap(StyledText styledText, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords = false)
        {
            string text = styledText.Text;

            if (string.IsNullOrEmpty(text) is false && maxWidth <= 0f)
            {
                Parchment.monitor.LogOnce($"Cannot wrap '{text}': the available width is {maxWidth}.", LogLevel.Warn);
            }

            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            {
                return new WrappedText(Array.Empty<WrappedLine>(), Vector2.Zero);
            }

            WrapState state = new WrapState() { ColorRuns = styledText.ColorRuns, LinkRuns = styledText.LinkRuns, EffectRuns = styledText.EffectRuns, Font = font, MaxWidth = maxWidth, Scale = scale, HyphenateBrokenWords = hyphenateBrokenWords };

            // Where each hard line starts in the whole text, so a line cut out of it can find the runs it falls under
            int hardLineStart = 0;

            foreach (string hardLine in text.Split('\n'))
            {
                WrapSingleLine(state, hardLine, hardLineStart);
                hardLineStart += hardLine.Length + 1;
            }

            return new WrappedText(state.Lines, MeasureLines(state.Lines));
        }

        private static Vector2 MeasureLines(IReadOnlyList<WrappedLine> lines)
        {
            float maximumLineWidth = 0f;
            float totalHeight = 0f;

            foreach (WrappedLine line in lines)
            {
                maximumLineWidth = Math.Max(maximumLineWidth, line.Size.X);
                totalHeight += line.Size.Y;
            }

            return new Vector2(maximumLineWidth, totalHeight);
        }

        /// <param name="sourceStart">Where the line starts in the whole text.</param>
        /// <param name="sourceLength">How much of the whole text the line covers, which leaves out a hyphen the wrap added to a broken word.</param>
        private static void AddLine(WrapState state, string text, int sourceStart, int sourceLength)
        {
            state.Lines.Add(new WrappedLine(text, state.Font.MeasureString(text, state.Scale), GetSegments(state, text, sourceStart, sourceLength)));
        }

        private static void AddBlankLine(WrapState state)
        {
            state.Lines.Add(new WrappedLine(string.Empty, new Vector2(0f, state.Font.MeasureString(BLANK_LINE_MEASURE_TEXT, state.Scale).Y)));
        }

        /// <summary>Cuts a line wherever its color, link or effects change. Returns null when no run of any kind reaches it.
        /// Each segment's offset is measured once here rather than on every draw. Alignment only moves the line as a whole, so the offsets hold however it is aligned.
        /// </summary>
        private static IReadOnlyList<TextSegment>? GetSegments(WrapState state, string text, int sourceStart, int sourceLength)
        {
            if ((state.ColorRuns.Count is 0 && state.LinkRuns.Count is 0 && state.EffectRuns.Count is 0) || sourceLength <= 0)
            {
                return null;
            }

            int sourceEnd = sourceStart + sourceLength;
            SortedSet<int> cuts = new SortedSet<int>() { sourceStart, sourceEnd };
            bool isReached = false;

            foreach (ColorRun run in state.ColorRuns)
            {
                isReached |= AddCuts(cuts, run.Start, run.End, sourceStart, sourceEnd);
            }

            foreach (LinkRun run in state.LinkRuns)
            {
                isReached |= AddCuts(cuts, run.Start, run.End, sourceStart, sourceEnd);
            }

            foreach (EffectRun run in state.EffectRuns)
            {
                isReached |= AddCuts(cuts, run.Start, run.End, sourceStart, sourceEnd);
            }

            if (isReached is false)
            {
                return null;
            }

            List<int> cutPoints = new List<int>(cuts);
            List<TextSegment> segments = new List<TextSegment>(cutPoints.Count - 1);

            for (int index = 0; index < cutPoints.Count - 1; index++)
            {
                int pieceStart = cutPoints[index];
                int localStart = pieceStart - sourceStart;
                bool isLastPiece = index == cutPoints.Count - 2;

                // The last piece takes everything left on the line, so a hyphen the wrap added is drawn in the color of the word it broke
                string segmentText = isLastPiece ? text.Substring(localStart) : text.Substring(localStart, cutPoints[index + 1] - pieceStart);
                float offsetX = localStart is 0 ? 0f : state.Font.MeasureString(text.Substring(0, localStart), state.Scale).X;
                float width = state.Font.MeasureString(segmentText, state.Scale).X;

                IReadOnlyList<TextEffect>? effects = FindEffects(state.EffectRuns, pieceStart);

                if (effects is null)
                {
                    segments.Add(new TextSegment(segmentText, FindColor(state.ColorRuns, pieceStart), FindLinkIndex(state.LinkRuns, pieceStart), offsetX, width));
                    continue;
                }

                string[] characters = new string[segmentText.Length];
                float[] characterOffsets = new float[segmentText.Length];

                for (int characterIndex = 0; characterIndex < segmentText.Length; characterIndex++)
                {
                    characters[characterIndex] = segmentText[characterIndex].ToString();
                    characterOffsets[characterIndex] = characterIndex is 0 ? offsetX : state.Font.MeasureString(text.Substring(0, localStart + characterIndex), state.Scale).X;
                }

                segments.Add(new TextSegment(segmentText, FindColor(state.ColorRuns, pieceStart), FindLinkIndex(state.LinkRuns, pieceStart), offsetX, width, pieceStart, effects, characters, characterOffsets));
            }

            return segments;
        }

        private static IReadOnlyList<TextEffect>? FindEffects(IReadOnlyList<EffectRun> runs, int position)
        {
            foreach (EffectRun run in runs)
            {
                if (position >= run.Start && position < run.End)
                {
                    return run.Effects;
                }
            }

            return null;
        }

        /// <summary>Adds where a run starts and ends to a line's cuts, for whatever part of it falls on the line. Returns whether any of it does.</summary>
        private static bool AddCuts(SortedSet<int> cuts, int runStart, int runEnd, int sourceStart, int sourceEnd)
        {
            if (runEnd <= sourceStart || runStart >= sourceEnd)
            {
                return false;
            }

            cuts.Add(Math.Max(runStart, sourceStart));
            cuts.Add(Math.Min(runEnd, sourceEnd));

            return true;
        }

        private static Color? FindColor(IReadOnlyList<ColorRun> runs, int position)
        {
            foreach (ColorRun run in runs)
            {
                if (position >= run.Start && position < run.End)
                {
                    return run.Color;
                }
            }

            return null;
        }

        private static int? FindLinkIndex(IReadOnlyList<LinkRun> runs, int position)
        {
            foreach (LinkRun run in runs)
            {
                if (position >= run.Start && position < run.End)
                {
                    return run.Occurrence;
                }
            }

            return null;
        }

        /// <param name="lineStart">Where this hard line starts in the whole text. Every line cut from it is a contiguous stretch of it, which is what lets each one be traced back to the runs it falls under.</param>
        private static void WrapSingleLine(WrapState state, string line, int lineStart)
        {
            if (line.Length is 0)
            {
                AddBlankLine(state);
                return;
            }

            int indentLength = 0;
            while (indentLength < line.Length && line[indentLength] == ' ')
            {
                indentLength++;
            }

            // A line that is nothing but spaces: preserve it rather than culling to blank.
            if (indentLength == line.Length)
            {
                AddLine(state, line, lineStart, line.Length);
                return;
            }

            string indent = line.Substring(0, indentLength);
            string currentLine = string.Empty;
            int currentStart = lineStart;
            bool indentPending = true;
            int nextWordStart = lineStart + indentLength;

            foreach (string word in line.Substring(indentLength).Split(' '))
            {
                // Tracked alongside the words, since the single space each one is split on is the only thing between them
                int wordStart = nextWordStart;
                nextWordStart += word.Length + 1;

                string candidateLine = currentLine.Length is 0 ? (indentPending ? indent + word : word) : $"{currentLine} {word}";

                if (state.Font.MeasureString(candidateLine, state.Scale).X <= state.MaxWidth)
                {
                    // A word still carrying the indent starts where the hard line does, as the indent is written in front of it
                    if (currentLine.Length is 0)
                    {
                        currentStart = indentPending ? lineStart : wordStart;
                    }

                    currentLine = candidateLine;
                    continue;
                }

                if (currentLine.Length > 0)
                {
                    AddLine(state, currentLine, currentStart, currentLine.Length);
                    currentLine = string.Empty;
                    indentPending = false;
                }

                string wordWithIndent = indentPending ? indent + word : word;
                int wordWithIndentStart = indentPending ? lineStart : wordStart;

                if (state.Font.MeasureString(wordWithIndent, state.Scale).X <= state.MaxWidth)
                {
                    currentLine = wordWithIndent;
                    currentStart = wordWithIndentStart;
                    indentPending = false;
                    continue;
                }

                currentLine = BreakLongWord(state, wordWithIndent, wordWithIndentStart, out currentStart);
                indentPending = false;
            }

            if (currentLine.Length > 0)
            {
                AddLine(state, currentLine, currentStart, currentLine.Length);
            }
        }

        /// <param name="wordStart">Where the word starts in the whole text.</param>
        /// <param name="remainderStart">Where the returned remainder starts in the whole text, so the line it goes on to begin can be traced back like any other.</param>
        private static string BreakLongWord(WrapState state, string word, int wordStart, out int remainderStart)
        {
            int segmentStart = 0;

            while (segmentStart < word.Length)
            {
                int segmentLength = 0;

                while (segmentStart + segmentLength < word.Length)
                {
                    int candidateLength = segmentLength + 1;
                    bool isFinalSegment = segmentStart + candidateLength >= word.Length;
                    string candidateSegment = word.Substring(segmentStart, candidateLength);

                    if (state.HyphenateBrokenWords && isFinalSegment is false)
                    {
                        candidateSegment += HYPHEN;
                    }

                    if (state.Font.MeasureString(candidateSegment, state.Scale).X > state.MaxWidth)
                    {
                        break;
                    }

                    segmentLength = candidateLength;
                }

                if (segmentLength is 0)
                {
                    segmentLength = 1;
                }

                if (segmentStart + segmentLength >= word.Length)
                {
                    remainderStart = wordStart + segmentStart;
                    return word.Substring(segmentStart);
                }

                string segment = word.Substring(segmentStart, segmentLength);
                AddLine(state, state.HyphenateBrokenWords ? $"{segment}{HYPHEN}" : segment, wordStart + segmentStart, segmentLength);
                segmentStart += segmentLength;
            }

            remainderStart = wordStart + word.Length;
            return string.Empty;
        }
    }
}
