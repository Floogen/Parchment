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
    }
}
