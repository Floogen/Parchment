namespace Parchment.Framework.API
{
    /// <summary>Builds one page of a book. Obtained from <see cref="IBookBuilder.AddPage(string)"/>.</summary>
    public interface IPageBuilder
    {
        /// <summary>The page's ID.</summary>
        string PageId { get; }

        /// <summary>Sets any field on the page by name, for anything the methods below don't cover.</summary>
        IPageBuilder Set(string field, object? value);

        /// <summary>Adds an element to the page's stacked content, by element type name such as "Heading" or "Image".</summary>
        IElementBuilder Add(string elementType);

        /// <summary>Adds an element drawn behind the page's content, positioned absolutely rather than stacked.</summary>
        IElementBuilder AddBackground(string elementType);

        /// <summary>Adds an element drawn over the page's content, positioned absolutely rather than stacked.</summary>
        IElementBuilder AddForeground(string elementType);

        IElementBuilder AddTitle(string text);
        IElementBuilder AddHeading(string text);
        IElementBuilder AddParagraph(string text);
        IElementBuilder AddBanner(string text);
        IElementBuilder AddDivider();
        IElementBuilder AddPanel();

        /// <summary>Adds the page's own number, filled in from its position in the book.</summary>
        IElementBuilder AddPageNumber();

        /// <summary>Adds an image drawn from a texture asset.</summary>
        IElementBuilder AddImage(string texturePath);

        /// <summary>Adds an image drawn from an item's icon, using a qualified item ID such as "(O)24". The item's name and description
        /// fill in the tooltip automatically.</summary>
        IElementBuilder AddItemImage(string itemId);

        /// <summary>Adds a button running a trigger action when clicked.</summary>
        IElementBuilder AddButton(string text, string action);

        /// <summary>Runs a trigger action each time this page becomes visible.</summary>
        IPageBuilder OnView(string action);

        /// <summary>Runs a trigger action each time this page becomes visible and the game state query passes.</summary>
        IPageBuilder OnView(string action, string condition);

        /// <summary>Runs a trigger action when the given keybind is pressed while this page is visible, taking the button over from the menu.
        /// The keybind uses SMAPI's syntax, such as "Escape", "LeftControl + S" or "Escape, Back".</summary>
        IPageBuilder OnKeyPress(string keybind, string action);

        /// <summary>The same, gated by a game state query checked at the moment the button is pressed.</summary>
        IPageBuilder OnKeyPress(string keybind, string action, string condition);

        /// <summary>Removes the last element added to the page's stacked content, for undoing a speculative add that turned out not to fit.
        /// Does nothing when the page has no stacked content. Background and foreground elements are left alone.</summary>
        IPageBuilder RemoveLast();

        /// <summary>The width in pixels this page has for stacked content, being what text wraps to.</summary>
        /// <remarks>This is a size rather than a position, since a page has no place on screen until its book is open. Use <c>TryGetLeftPageBounds</c> on the API for that.</remarks>
        float GetAvailableWidth();

        /// <summary>The height in pixels this page has for stacked content, from the book's appearance and layout.</summary>
        /// <remarks>Returns 0 before Parchment has finished starting up, since the page size isn't known until then.</remarks>
        float GetAvailableHeight();

        /// <summary>How tall the stacked elements added so far come to in pixels, measured with the fonts, wrapping and spacing they will actually be drawn with.
        /// Content past the bottom of the page still counts, so this keeps growing rather than stopping at the page edge.</summary>
        /// <remarks>Each call rebuilds and measures the page, so prefer calling it once per element added rather than in a tight loop.</remarks>
        float GetContentHeight();

        /// <summary>How much room is left on the page in pixels, which goes negative once the content overflows.</summary>
        float GetRemainingHeight();

        /// <summary>Whether the elements added so far run past the bottom of the page.</summary>
        /// <remarks>
        /// Pair this with <see cref="RemoveLast"/> to fill pages greedily: add an element, and if the page now overflows,
        /// take it back off and start a new page with it instead.
        /// </remarks>
        bool WouldOverflow();
    }
}
