namespace Parchment.Framework.API
{
    /// <summary>Builds one link, the entry [link=id] markup in an element's text points at. Obtained from <see cref="IElementBuilder.AddLink(string)"/>.</summary>
    public interface ILinkBuilder
    {
        /// <summary>The id this link was added under, which is what the markup names.</summary>
        string LinkId { get; }

        /// <summary>Sets any field on the link by name, for anything the methods below don't cover. Fields that don't exist are reported when the book is registered, along with the ones that do.</summary>
        ILinkBuilder Set(string field, object? value);

        /// <summary>A game state query deciding whether the link applies wherever it's used. When it fails the linked text is drawn plain and the cursor can't reach it.</summary>
        ILinkBuilder Condition(string condition);

        /// <summary>The color the linked text is drawn in, as a name such as "Gold" or a value such as "255 215 0". Left unset, the text keeps the color around it.</summary>
        ILinkBuilder TextColor(string color);

        /// <summary>The color the linked text is drawn in while the cursor is over it, covering any [color] markup inside the link.</summary>
        ILinkBuilder HoverTextColor(string color);

        /// <summary>The link's hover tooltip title and body.</summary>
        ILinkBuilder Tooltip(string displayName, string description);

        /// <summary>Adds a trigger action to run when the link is clicked. Calling this more than once builds a list run in order.
        /// The click plays the Sound of the element the link sits in, once however many actions run.
        /// </summary>
        ILinkBuilder Action(string action);

        /// <summary>Adds a trigger action to run when the cursor moves onto the link. Calling this more than once builds a list run in order.
        /// Every entry runs each time the cursor arrives, so keep the whole list harmless to repeat.
        /// </summary>
        ILinkBuilder HoverAction(string action);

        /// <summary>Adds a tag to the link, read the same way as an element's own. Calling this more than once builds a list.</summary>
        ILinkBuilder WithTag(string tag);

        /// <summary>Adds a text effect applied to the linked text while the cursor is over it, written the way its inline tag is without the brackets, such as "wave=3|600" or "pulse=Gold".
        /// Calling this more than once builds a list, applied in order. They ease in when the cursor arrives and stop when it leaves.
        /// </summary>
        ILinkBuilder HoverEffect(string effect);
    }
}
