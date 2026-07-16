using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Layouts
{
    public class BannerLayout
    {
        public WrappedText WrappedText { get; }
        internal float TextScale { get; }
        public Rectangle LeftSource { get; }
        public Rectangle MiddleSource { get; }
        public Rectangle RightSource { get; }
        public int CapWidth { get; }
        public int Padding { get; }

        public BannerLayout(WrappedText wrappedText, float textScale, Rectangle leftSource, Rectangle middleSource, Rectangle rightSource, int capWidth, int padding)
        {
            WrappedText = wrappedText;
            TextScale = textScale;
            LeftSource = leftSource;
            MiddleSource = middleSource;
            RightSource = rightSource;
            CapWidth = capWidth;
            Padding = padding;
        }
    }
}
