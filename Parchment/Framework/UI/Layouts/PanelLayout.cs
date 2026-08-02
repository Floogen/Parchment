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

        /// <summary>The context the Background and Foreground are placed in, sized to the panel's settled content area rather than the space that was available to it.
        /// Assigned at the end of the measure pass, since a shrink-to-fit or auto-height panel doesn't know its own size until its children have been stacked.
        /// </summary>
        public ElementRenderContext PlacedContext { get; set; }

        public int Inset { get; }

        public PanelLayout(ElementRenderContext childContext, int inset)
        {
            ChildContext = childContext;
            PlacedContext = childContext;
            Inset = inset;
        }
    }
}
