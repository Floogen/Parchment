using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Layouts
{
    public class ImageLayout
    {
        public Rectangle SourceRectangle { get; }
        public float DrawScale { get; }
        public Vector2 DrawSize { get; }

        internal ImageLayout(Rectangle sourceRectangle, float drawScale, Vector2 drawSize)
        {
            SourceRectangle = sourceRectangle;
            DrawScale = drawScale;
            DrawSize = drawSize;
        }
    }
}
