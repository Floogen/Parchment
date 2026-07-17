using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Extensions
{
    public static class ColorExtensions
    {
        public static string ToSpaceSeparated(this Color c, bool includeAlpha = true)
        {
            return includeAlpha ? $"{c.R} {c.G} {c.B} {c.A}" : $"{c.R} {c.G} {c.B}";
        }
    }
}
