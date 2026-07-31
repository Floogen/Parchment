using Microsoft.Xna.Framework;
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

        /// <summary>Starts building a book in code. Configure it through the returned builder, then call TryRegister or TryOpen on it.</summary>
        /// <param name="bookId">The ID the book will be known by. Prefix it with your mod's unique ID.</param>
        IBookBuilder CreateBook(string bookId);

        /// <summary>Removes a book your mod previously registered. Books from content packs and from other mods can't be removed.</summary>
        /// <param name="bookId">The BookData.Id value of the book.</param>
        /// <param name="error">Why the book couldn't be removed, when this returns false.</param>
        bool TryUnregisterBook(string bookId, out string error);

        /// <summary>Gets whether a book with the given ID is loaded, whether it came from a content pack or the C# API.</summary>
        bool HasBook(string bookId);

        /// <summary>Reads a variable a book declares, so a mod can mirror a reader's choice into its own config.</summary>
        /// <param name="bookId">The BookData.Id value of the book declaring the variable.</param>
        /// <param name="variableId">The VariableData.Id value of the variable.</param>
        /// <param name="value">The variable's current value, or its default when nothing has set it yet.</param>
        /// <returns>False when the book isn't loaded or declares no variable by that name.</returns>
        bool TryGetVariable(string bookId, string variableId, out string value);

        /// <summary>Sets a variable a book declares. The value has to suit the variable's declared type and allowed values.</summary>
        /// <param name="bookId">The BookData.Id value of the book declaring the variable.</param>
        /// <param name="variableId">The VariableData.Id value of the variable.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="error">Why the value couldn't be stored, when this returns false.</param>
        bool TrySetVariable(string bookId, string variableId, string value, out string error);

        /// <summary>Gets the whole book frame's bounds on screen, for drawing alongside an open book.</summary>
        /// <param name="bounds">The book's bounds, when this returns true.</param>
        /// <returns>False when no Parchment book is open.</returns>
        /// <remarks>Taken from the book's resting position, so it stays put while the open and close animations play.</remarks>
        bool TryGetBookBounds(out Rectangle bounds);

        /// <summary>Gets the left page's content area on screen, being the region a page's stacked elements are laid out in.</summary>
        /// <param name="bounds">The page's bounds, when this returns true.</param>
        /// <returns>False when no Parchment book is open.</returns>
        bool TryGetLeftPageBounds(out Rectangle bounds);

        /// <summary>Gets the right page's content area on screen, being the region a page's stacked elements are laid out in.</summary>
        /// <param name="bounds">The page's bounds, when this returns true.</param>
        /// <returns>False when no Parchment book is open.</returns>
        bool TryGetRightPageBounds(out Rectangle bounds);

        /// <summary>Opens a book in the book menu at a page number.</summary>
        [Obsolete("Use TryOpenBookAtPage instead. This overload is kept so mods built against Parchment 1.1.0 keep working.")]
        bool TryOpenBook(string bookId, string chapterId, int page);

        /// <summary>Opens a book in the book menu, optionally at a chapter and/or page ID.</summary>
        [Obsolete("Use TryOpenBook(string, string) or TryOpenBookAtPageId instead. This overload is kept so mods built against Parchment 1.1.0 keep working.")]
        bool TryOpenBook(string bookId, string? chapterId, string? pageId);
    }
}
