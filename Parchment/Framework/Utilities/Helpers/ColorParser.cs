using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class ColorParser
    {
        public static bool TryParse(string? value, out Color color)
        {
            color = Color.White;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            Color? parsedColor = Utility.StringToColor(value.Trim());
            if (parsedColor is null)
            {
                return false;
            }
            color = Premultiply(parsedColor.Value);

            return true;
        }

        /// <summary>Scales a color's channels by its own alpha, which mirrors how the game handles it.</summary>
        public static Color Premultiply(Color color)
        {
            if (color.A is byte.MaxValue)
            {
                return color;
            }

            return Color.FromNonPremultiplied(color.R, color.G, color.B, color.A);
        }
    }
}
