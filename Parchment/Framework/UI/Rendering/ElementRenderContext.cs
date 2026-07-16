using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering
{
    public readonly record struct ElementRenderContext(float AvailableWidth, float AvailableHeight)
    {
        public ElementRenderContext WithWidth(float availableWidth)
        {
            return this with { AvailableWidth = availableWidth };
        }

        public ElementRenderContext WithSize(float availableWidth, float availableHeight)
        {
            return this with { AvailableWidth = availableWidth, AvailableHeight = availableHeight };
        }
    }
}
