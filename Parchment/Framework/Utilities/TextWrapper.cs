using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities
{
    public class WrappedText
    {
        internal string Text { get; }
        internal Vector2 Size { get; }

        internal WrappedText(string text, Vector2 size)
        {
            this.Text = text;
            this.Size = size;
        }
    }

    public static class TextWrapper
    {
        private const char HYPHEN = '-';

        public static WrappedText Wrap(string text, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords = false)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0f)
            {
                return new WrappedText(string.Empty, Vector2.Zero);
            }

            List<string> lines = new List<string>();

            foreach (string hardLine in text.Replace("\r\n", "\n").Split('\n'))
            {
                WrapSingleLine(lines, hardLine, font, maxWidth, scale, hyphenateBrokenWords);
            }

            string wrappedText = string.Join("\n", lines);

            return new WrappedText(wrappedText, font.MeasureString(wrappedText, scale));
        }

        private static void WrapSingleLine(List<string> lines, string line, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords)
        {
            if (line.Length is 0)
            {
                lines.Add(string.Empty);
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
                    lines.Add(currentLine);
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
                lines.Add(currentLine);
            }
        }

        private static string BreakLongWord(List<string> lines, string word, IFont font, float maxWidth, float scale, bool hyphenateBrokenWords)
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
                lines.Add(hyphenateBrokenWords ? $"{segment}{HYPHEN}" : segment);
                segmentStart += segmentLength;
            }

            return string.Empty;
        }
    }
}
