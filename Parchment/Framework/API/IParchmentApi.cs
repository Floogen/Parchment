using System;

namespace Parchment.Framework.API
{
    public interface IParchmentApi
    {
        /// <summary>Opens a book in the book menu.</summary>
        /// <param name="bookId">The BookData.Id value of the book.</param>
        /// <param name="chapterId">The optional chapter to open on, null for the first chapter.</param>
        /// <returns>True if the book was found and opened.</returns>
        bool TryOpenBook(string bookId, string? chapterId = null);

        /// <summary>Opens a book in the book menu at a page number.</summary>
        /// <param name="bookId">The BookData.Id value of the book.</param>
        /// <param name="chapterId">The chapter the page number is relative to, or null to treat it as an index into the whole book.</param>
        /// <param name="page">The 0-based page number.</param>
        /// <returns>True if the book was found and opened.</returns>
        bool TryOpenBookAtPage(string bookId, string? chapterId, int page);

        /// <summary>Opens a book in the book menu at a page ID.</summary>
        /// <param name="bookId">The BookData.Id value of the book.</param>
        /// <param name="chapterId">The optional chapter to search within, or null to search the whole book.</param>
        /// <param name="pageId">The PageData.Id value of the page.</param>
        /// <returns>True if the book was found and opened.</returns>
        bool TryOpenBookAtPageId(string bookId, string? chapterId, string pageId);

        /// <summary>Opens a book in the book menu at a page number.</summary>
        [Obsolete("Use TryOpenBookAtPage instead. This overload is kept so mods built against Parchment 1.1.0 keep working.")]
        bool TryOpenBook(string bookId, string chapterId, int page);

        /// <summary>Opens a book in the book menu, optionally at a chapter and/or page ID.</summary>
        [Obsolete("Use TryOpenBook(string, string) or TryOpenBookAtPageId instead. This overload is kept so mods built against Parchment 1.1.0 keep working.")]
        bool TryOpenBook(string bookId, string? chapterId, string? pageId);
    }
}
