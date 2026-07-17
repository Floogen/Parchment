using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class NineSliceHelper
    {
        public static int GetBorderThickness(Rectangle sourceRectangle, float scale)
        {
            return (int)(Math.Min(sourceRectangle.Width, sourceRectangle.Height) / 3f * scale);
        }
    }
}
