using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Books;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Works out how much room one page has for content, from the book's own appearance and layout.</summary>
    /// <remarks>
    /// This is the single source of truth for page size, shared by the menu that renders a book and the builder that
    /// measures one before it is registered. Position is deliberately left out, as it depends on the open menu while
    /// the size does not.
    /// </remarks>
    public static class PageLayoutHelper
    {
        /// <summary>Get the size of one page's content area in screen pixels, which is half the book frame less its margins.</summary>
        /// <param name="bookData">The book being laid out.</param>
        public static Point GetPageContentSize(BookData bookData)
        {
            if (bookData is null)
            {
                return Point.Zero;
            }

            BookAppearanceData appearance = bookData.Appearance;
            BookLayoutData layout = bookData.Layout;

            int bookWidth = (int)(appearance.FrameWidth * appearance.Scale);
            int bookHeight = (int)(appearance.FrameHeight * appearance.Scale);

            return GetPageContentSize(bookWidth, bookHeight, layout, appearance.Scale);
        }

        /// <summary>Get the size of one page's content area from an already measured book frame, for callers that have the on screen bounds to hand.</summary>
        /// <param name="bookWidth">The whole book frame's width in screen pixels.</param>
        /// <param name="bookHeight">The whole book frame's height in screen pixels.</param>
        /// <param name="layout">The book's margins, in unscaled sprite pixels.</param>
        /// <param name="scale">The scale the book is drawn at.</param>
        public static Point GetPageContentSize(int bookWidth, int bookHeight, BookLayoutData layout, float scale)
        {
            int marginOuter = (int)(layout.MarginOuter * scale);
            int marginSpine = (int)(layout.MarginSpine * scale);
            int marginTop = (int)(layout.MarginTop * scale);
            int marginBottom = (int)(layout.MarginBottom * scale);

            return new Point(bookWidth / 2 - marginOuter - marginSpine, bookHeight - marginTop - marginBottom);
        }
    }
}
