using Microsoft.Xna.Framework;
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

    /// <summary>An element's text with its tokens resolved and its markup taken out, holding where each color and link applies alongside the plain text that is measured and drawn.</summary>
    public class StyledText
    {
        public static readonly StyledText Empty = new StyledText(string.Empty, Array.Empty<ColorRun>(), Array.Empty<LinkRun>());

        /// <summary>The text to draw, with its line breaks already normalized to a bare \n.</summary>
        public string Text { get; }

        /// <summary>The colored runs in order and never overlapping. Text outside every run is drawn in the element's own color.</summary>
        public IReadOnlyList<ColorRun> ColorRuns { get; }

        /// <summary>The linked runs in order and never overlapping. A link nested inside another takes over the text it covers.</summary>
        public IReadOnlyList<LinkRun> LinkRuns { get; }

        public StyledText(string text, IReadOnlyList<ColorRun> colorRuns, IReadOnlyList<LinkRun> linkRuns)
        {
            Text = text;
            ColorRuns = colorRuns;
            LinkRuns = linkRuns;
        }
    }
}
