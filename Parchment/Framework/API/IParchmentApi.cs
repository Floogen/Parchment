using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.API
{
    public interface IParchmentApi
    {
        /// <summary>Opens a book in the book menu.</summary>
        /// <param name="bookId">The BookData.Id value of the book.</param>
        /// <param name="chapterId">The chapter to open on.</param>
        /// <param name="page">The page within the chapter (0-based).</param>
        /// <returns>True if the book was found and opened.</returns>
        bool TryOpenBook(string bookId, string chapterId, int page);

        /// <summary>Opens a book in the book menu.</summary>
        /// <param name="bookId">The BookData.Id value of the book.</param>
        /// <param name="chapterId">The optional chapter to open on, null for the first chapter.</param>
        /// <param name="pageId">The optional page within the chapter, null for the first page.</param>
        /// <returns>True if the book was found and opened.</returns>
        bool TryOpenBook(string bookId, string? chapterId = null, string? pageId = null);
    }
}
