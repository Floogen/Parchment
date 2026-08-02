using Parchment.Framework.UI.Rendering;

namespace Parchment.Framework.UI.Layouts
{
    public class GridLayout
    {
        /// <summary>The context a single cell's element is measured against, being one cell's worth of space rather than the whole grid's.</summary>
        public ElementRenderContext CellContext { get; }

        /// <summary>The context the Background and Foreground are placed in, sized to the grid's settled content area. Assigned once the row count is known.</summary>
        public ElementRenderContext PlacedContext { get; set; }

        public int Inset { get; }

        public GridLayout(ElementRenderContext cellContext, int inset)
        {
            CellContext = cellContext;
            PlacedContext = cellContext;
            Inset = inset;
        }
    }
}
