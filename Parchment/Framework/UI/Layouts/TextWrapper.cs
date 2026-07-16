using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.UI.Layouts
{
    public class WrappedLine
    {
        public string Text { get; }
        public Vector2 Size { get; }

        public WrappedLine(string text, Vector2 size)
        {
            Text = text;
            Size = size;
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

        public static WrappedText Wrap(string text, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords = false)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            {
                return new WrappedText(Array.Empty<WrappedLine>(), Vector2.Zero);
            }

            List<WrappedLine> lines = new List<WrappedLine>();

            foreach (string hardLine in text.Replace("\r\n", "\n").Split('\n'))
            {
                WrapSingleLine(lines, hardLine, font, maxWidth, scale, hyphenateBrokenWords);
            }

            return new WrappedText(lines, MeasureLines(lines));
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

        private static void AddLine(List<WrappedLine> lines, string text, IFont font, float scale)
        {
            lines.Add(new WrappedLine(text, font.MeasureString(text, scale)));
        }

        private static void AddBlankLine(List<WrappedLine> lines, IFont font, float scale)
        {
            lines.Add(new WrappedLine(string.Empty, new Vector2(0f, font.MeasureString(BLANK_LINE_MEASURE_TEXT, scale).Y)));
        }

        private static void WrapSingleLine(List<WrappedLine> lines, string line, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords)
        {
            if (line.Length is 0)
            {
                AddBlankLine(lines, font, scale);
                return;
            }

            string currentLine = string.Empty;

            foreach (string word in line.Split(' '))
            {
                string candidateLine = currentLine.Length is 0 ? word : $"{currentLine} {word}";

                if (font.MeasureString(candidateLine, scale).X <= maxWidth)
                {
                    currentLine = candidateLine;
                    continue;
                }

                if (currentLine.Length > 0)
                {
                    AddLine(lines, currentLine, font, scale);
                    currentLine = string.Empty;
                }

                if (font.MeasureString(word, scale).X <= maxWidth)
                {
                    currentLine = word;
                    continue;
                }

                currentLine = BreakLongWord(lines, word, font, maxWidth, scale, hyphenateBrokenWords);
            }

            if (currentLine.Length > 0)
            {
                AddLine(lines, currentLine, font, scale);
            }
        }

        private static string BreakLongWord(List<WrappedLine> lines, string word, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords)
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

                    if (hyphenateBrokenWords && isFinalSegment is false)
                    {
                        candidateSegment += HYPHEN;
                    }

                    if (font.MeasureString(candidateSegment, scale).X > maxWidth)
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
                    return word.Substring(segmentStart);
                }

                string segment = word.Substring(segmentStart, segmentLength);
                AddLine(lines, hyphenateBrokenWords ? $"{segment}{HYPHEN}" : segment, font, scale);
                segmentStart += segmentLength;
            }

            return string.Empty;
        }
    }
}