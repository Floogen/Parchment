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
            color = parsedColor.Value;

            return true;
        }
    }
}
