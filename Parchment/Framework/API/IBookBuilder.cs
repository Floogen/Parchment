using System;

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

        /// <summary>Adds a keybind pressed on any page of this book and returns its builder, taking the button over from the menu.
        /// A page binding the same button wins, and this is left alone while that page is on screen.
        /// At least one Action is required, and a keybind without one is reported when the book is registered.
        /// </summary>
        IKeybindBuilder OnKeyPress(string keybind);

        /// <summary>What to run when something asks this book to rebuild itself, being the PeacefulEnd.Parchment_RefreshBook action or a call to TryRefresh.
        /// Assemble a fresh builder from your current state inside the callback and call TryRefresh on that one, as this builder holds the values it was already given.
        /// Only takes effect on a book opened with TryOpen, since a registered book comes from the books asset rather than from a builder.
        /// </summary>
        IBookBuilder OnRefresh(Action onRefresh);

        /// <summary>Validates the book and registers it. Registered books are added to Data/PeacefulEnd.Parchment/Books before content
        /// packs are applied, so Content Patcher can still edit them. Registering the same book ID again replaces your earlier registration.</summary>
        /// <param name="error">Why the book was rejected, when this returns false.</param>
        bool TryRegister(out string error);

        /// <summary>Validates the book and opens it immediately without registering it. Nothing is added to the books asset, so this is the
        /// route for a book whose contents are assembled fresh each time it's read.</summary>
        /// <param name="error">Why the book couldn't be opened, when this returns false.</param>
        bool TryOpen(out string error);

        /// <summary>Rebuilds this book and swaps it into the menu the reader already has open, keeping them on the page they were reading.
        /// A builder holds the values it was given rather than recomputing them, so assemble a fresh builder from your own current state and refresh with that one.
        /// Flags, input text and seen pages are left alone, as this replaces the book inside the open menu rather than putting up a new one.</summary>
        /// <param name="error">Why the book couldn't be refreshed, when this returns false. Nothing being open, something else being open and the book being mid-animation all report here rather than throwing.</param>
        bool TryRefresh(out string error);
    }
}
