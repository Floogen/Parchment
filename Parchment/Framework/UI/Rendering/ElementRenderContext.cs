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
    /// <param name="PageIndex">The 0-based index of the page being laid out within the book, or -1 when the elements do not belong to a single page (a book's Underlay and Overlay span the whole book).
    /// Carried through <see cref="WithWidth"/> and <see cref="WithSize"/> so a container's children see the same page.
    /// </param>
    /// <param name="ChapterPageIndex">The same page's 0-based index within its own chapter, or -1 on the same terms as <paramref name="PageIndex"/>.</param>
    public readonly record struct ElementRenderContext(float AvailableWidth, float AvailableHeight, int PageIndex = -1, int ChapterPageIndex = -1)
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
