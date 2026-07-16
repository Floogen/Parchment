using Microsoft.Xna.Framework;
using Parchment.Framework.UI.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Layouts
{
    public class PanelLayout
    {
        public ElementRenderContext ChildContext { get; }
        public int Inset { get; }

        public PanelLayout(ElementRenderContext childContext, int inset)
        {
            ChildContext = childContext;
            Inset = inset;
        }
    }
}
