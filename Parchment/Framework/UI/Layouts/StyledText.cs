using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.UI.Layouts
{
    /// <summary>A stretch of <see cref="StyledText.Text"/> drawn in a color of its own, given as character offsets into it.</summary>
    public class TextRun
    {
        public int Start { get; }
        public int Length { get; }
        public Color Color { get; }

        public int End => Start + Length;

        public TextRun(int start, int length, Color color)
        {
            Start = start;
            Length = length;
            Color = color;
        }
    }

    /// <summary>An element's text with its tokens resolved and its color markup taken out, holding where each color applies alongside the plain text that is measured and drawn.</summary>
    public class StyledText
    {
        public static readonly StyledText Empty = new StyledText(string.Empty, Array.Empty<TextRun>());

        /// <summary>The text to draw, with its line breaks already normalized to a bare \n.</summary>
        public string Text { get; }

        /// <summary>The colored runs in order and never overlapping. Text outside every run is drawn in the element's own color.</summary>
        public IReadOnlyList<TextRun> Runs { get; }

        public StyledText(string text, IReadOnlyList<TextRun> runs)
        {
            Text = text;
            Runs = runs;
        }
    }
}
