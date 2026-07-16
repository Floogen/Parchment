using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering
{
    public class ElementRenderContext
    {
        public float AvailableWidth { get; init; }

        public Color? DefaultTextColor { get; init; }
    }
}
