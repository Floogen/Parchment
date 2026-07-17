using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Layouts
{
    public class ButtonLayout
    {
        public WrappedText WrappedText { get; }
        public int Inset { get; }
        public float TextScale { get; }

        public ButtonLayout(WrappedText wrappedText, int inset, float textScale)
        {
            WrappedText = wrappedText;
            Inset = inset;
            TextScale = textScale;
        }
    }
}
