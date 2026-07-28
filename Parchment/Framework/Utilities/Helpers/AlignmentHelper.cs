using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.BellsAndWhistles.PlayerStatusList;

namespace Parchment.Framework.Utilities.Helpers
{
    internal static class AlignmentHelper
    {
        public static float GetAlignedX(float availableWidth, float contentWidth, AlignmentType alignment)
        {
            switch (alignment)
            {
                case AlignmentType.Center:
                    return (availableWidth - contentWidth) / 2f;
                case AlignmentType.Right:
                    return availableWidth - contentWidth;
            }

            return 0f;
        }

        public static float GetAlignedX(Rectangle bounds, float contentWidth, AlignmentType alignment)
        {
            return bounds.X + GetAlignedX(bounds.Width, contentWidth, alignment);
        }

        public static float GetAlignedY(float availableHeight, float contentHeight, VerticalAlignmentType alignment)
        {
            switch (alignment)
            {
                case VerticalAlignmentType.Center:
                    return (availableHeight - contentHeight) / 2f;
                case VerticalAlignmentType.Bottom:
                    return availableHeight - contentHeight;
            }

            return 0f;
        }

        public static float GetAlignedY(Rectangle bounds, float contentHeight, VerticalAlignmentType alignment)
        {
            return bounds.Y + GetAlignedY(bounds.Height, contentHeight, alignment);
        }
    }
}
