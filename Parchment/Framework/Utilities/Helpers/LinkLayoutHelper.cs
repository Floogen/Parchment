using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Layouts;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class LinkLayoutHelper
    {
        /// <summary>Places each of a text element's links over the stretch of text it covers, one rectangle per line it reaches.
        /// Measured from the element's own top left, which is the origin its children are hit tested from. Lines are aligned within the width the element measured to, the same as they are drawn.
        /// A link with no text on screen (cut off or empty once its tokens resolved) is given empty bounds, so the cursor and a controller both pass it by.
        /// </summary>
        public static void ArrangeLinks(Element element, WrappedText? wrappedText, float measuredWidth, AlignmentType alignment)
        {
            if (element.Children.Count is 0)
            {
                return;
            }

            Dictionary<int, List<Rectangle>> regionsByLink = new Dictionary<int, List<Rectangle>>();

            if (wrappedText is not null)
            {
                CollectRegions(wrappedText, (int)measuredWidth, alignment, regionsByLink);
            }

            for (int index = 0; index < element.Children.Count; index++)
            {
                Element link = element.Children[index];

                if (link.Data is not LinkElementData)
                {
                    continue;
                }

                if (regionsByLink.TryGetValue(index, out List<Rectangle>? regions) is false || regions.Count is 0)
                {
                    link.HitRegions = Array.Empty<Rectangle>();
                    link.Bounds = Rectangle.Empty;

                    continue;
                }

                Rectangle bounds = regions[0];
                foreach (Rectangle region in regions)
                {
                    bounds = Rectangle.Union(bounds, region);
                }

                link.HitRegions = regions;
                link.Bounds = bounds;
            }
        }

        private static void CollectRegions(WrappedText wrappedText, int measuredWidth, AlignmentType alignment, Dictionary<int, List<Rectangle>> regionsByLink)
        {
            float lineY = 0f;

            foreach (WrappedLine line in wrappedText.Lines)
            {
                if (line.Segments is not null)
                {
                    float lineX = AlignmentHelper.GetAlignedX(availableWidth: measuredWidth, contentWidth: line.Size.X, alignment: alignment);

                    foreach (TextSegment segment in line.Segments)
                    {
                        if (segment.LinkIndex is not int linkIndex || segment.Width <= 0f)
                        {
                            continue;
                        }

                        if (regionsByLink.TryGetValue(linkIndex, out List<Rectangle>? regions) is false)
                        {
                            regions = new List<Rectangle>();
                            regionsByLink[linkIndex] = regions;
                        }

                        Rectangle region = new Rectangle((int)(lineX + segment.OffsetX), (int)lineY, (int)Math.Ceiling(segment.Width), (int)Math.Ceiling(line.Size.Y));

                        // A link cut in two by one nested inside it lands as neighbouring pieces on the same line, which read as one stretch to the cursor
                        if (regions.Count is not 0 && regions[^1].Y == region.Y && Math.Abs(regions[^1].Right - region.X) <= 1)
                        {
                            regions[^1] = Rectangle.Union(regions[^1], region);
                            continue;
                        }

                        regions.Add(region);
                    }
                }

                lineY += line.Size.Y;
            }
        }
    }
}
