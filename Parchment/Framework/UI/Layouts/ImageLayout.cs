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
        public Rectangle TextArea { get; }
        public WrappedText? WrappedText { get; }
        public float TextScale { get; }
        public float Rotation { get; }

        public ImageLayout(Rectangle sourceRectangle, float drawScale, Vector2 drawSize, Rectangle textArea, WrappedText? wrappedText, float textScale, float rotation)
        {
            SourceRectangle = sourceRectangle;
            DrawScale = drawScale;
            DrawSize = drawSize;
            TextArea = textArea;
            WrappedText = wrappedText;
            TextScale = textScale;
            Rotation = rotation;
        }
    }
}
