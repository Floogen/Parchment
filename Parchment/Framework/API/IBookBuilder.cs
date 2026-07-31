namespace Parchment.Framework.API
{
    /// <summary>Builds a book in code. Obtained from <see cref="IParchmentApi.CreateBook"/>, then registered or opened through its own methods.</summary>
    public interface IBookBuilder
    {
        /// <summary>The book's ID.</summary>
        string BookId { get; }

        /// <summary>Sets any field on the book by name, for anything the methods below don't cover. The name may be a dotted path into a
        /// nested group, such as "Appearance.Scale", "Layout.MarginTop" or "Animation.PageTurnDuration".</summary>
        IBookBuilder Set(string field, object? value);

        /// <summary>Sets the sprite used for the book item.</summary>
        IBookBuilder Sprite(string spritePath);

        /// <summary>Adds a page and returns its builder. Pages appear in the order they're added.</summary>
        IPageBuilder AddPage(string pageId);

        /// <summary>Adds a page belonging to a chapter. Pages sharing a chapter must be added contiguously.</summary>
        IPageBuilder AddPage(string pageId, string chapterId);

        /// <summary>Adds an element drawn behind the book sprite, positioned relative to the book's top-left.</summary>
        IElementBuilder AddUnderlay(string elementType);

        /// <summary>Adds an element drawn in front of the book sprite and its pages.</summary>
        IElementBuilder AddOverlay(string elementType);

        /// <summary>Adds a variable this book declares and returns its builder. Declaring is required before an action or query can name the variable,
        /// which is what stops a typo persisting into a save. A variable added here is addressed by this book's ID, so two books can declare the same name without sharing a value.
        /// </summary>
        IVariableBuilder AddVariable(string variableId);

        /// <summary>Runs a trigger action when the given keybind is pressed on any page of this book, taking the button over from the menu.
        /// A page binding the same button wins, and this is left alone while that page is on screen.</summary>
        IBookBuilder OnKeyPress(string keybind, string action);

        /// <summary>The same, gated by a game state query checked at the moment the button is pressed.</summary>
        IBookBuilder OnKeyPress(string keybind, string action, string condition);

        /// <summary>Validates the book and registers it. Registered books are added to Data/PeacefulEnd.Parchment/Books before content
        /// packs are applied, so Content Patcher can still edit them. Registering the same book ID again replaces your earlier registration.</summary>
        /// <param name="error">Why the book was rejected, when this returns false.</param>
        bool TryRegister(out string error);

        /// <summary>Validates the book and opens it immediately without registering it. Nothing is added to the books asset, so this is the
        /// route for a book whose contents are assembled fresh each time it's read.</summary>
        /// <param name="error">Why the book couldn't be opened, when this returns false.</param>
        bool TryOpen(out string error);
    }
}
