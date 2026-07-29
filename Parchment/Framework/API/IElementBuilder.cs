namespace Parchment.Framework.API
{
    /// <summary>Builds one element. Obtained from the add methods on <see cref="IPageBuilder"/> and <see cref="IBookBuilder"/>.</summary>
    public interface IElementBuilder
    {
        /// <summary>The element's type name.</summary>
        string ElementType { get; }

        /// <summary>Sets any field on the element by name, for anything the methods below don't cover. Fields that don't exist on this
        /// element type are reported when the book is registered, along with the ones that do.</summary>
        IElementBuilder Set(string field, object? value);

        /// <summary>Sets the element's ID, which page actions and queries use to refer to it.</summary>
        IElementBuilder WithId(string id);

        IElementBuilder Text(string text);

        /// <summary>How the element sits across its container's width: "Left", "Center" or "Right".</summary>
        IElementBuilder Alignment(string alignment);

        /// <summary>How the element sits down its container's height: "Top", "Center" or "Bottom". Only used on a placed element, one added
        /// through AddBackground, AddForeground, AddUnderlay or AddOverlay, as a stacked element takes its vertical position from the elements above it.</summary>
        IElementBuilder VerticalAlignment(string alignment);

        /// <summary>How text sits inside the element, where the element type supports it.</summary>
        IElementBuilder TextAlignment(string alignment);

        /// <summary>The font to draw text with: "Dialogue", "Small", "Tiny" or "SpriteText".</summary>
        IElementBuilder Font(string fontType);

        IElementBuilder TextColor(string color);
        IElementBuilder TextScale(float scale);
        IElementBuilder Scale(float scale);
        IElementBuilder Rotation(float rotation);

        /// <summary>The pivot the sprite rotates and scales around, in unscaled sprite pixels from the source rectangle's top-left. It
        /// changes what the sprite turns and grows about, never where it rests.</summary>
        IElementBuilder Origin(float x, float y);

        /// <summary>Positions the element in screen pixels relative to its container, rather than stacking it.</summary>
        IElementBuilder Position(int x, int y);

        IElementBuilder Texture(string texturePath);
        IElementBuilder TextureSource(int x, int y, int width, int height);
        IElementBuilder HoverTextureSource(int x, int y, int width, int height);

        /// <summary>A colour multiplied over the sprite, as a name such as "Red" or a value such as "255 128 0".</summary>
        IElementBuilder Tint(string tintColor);

        /// <summary>Draws an item's icon instead of a texture, using a qualified item ID such as "(O)24".</summary>
        IElementBuilder Item(string itemId);

        /// <summary>Adds a trigger action to run when the element is clicked. Calling this more than once builds a list run in order, so
        /// there's no need to decide up front between one action and several.</summary>
        IElementBuilder Action(string action);

        /// <summary>Adds a click action and sets the sound played on click. The sound plays once however many actions run.</summary>
        IElementBuilder Action(string action, string sound);

        /// <summary>Adds a trigger action to run when the cursor moves onto the element. Calling this more than once builds a list run in
        /// order. Every entry runs on each entry of the cursor, so keep the whole list harmless to repeat.</summary>
        IElementBuilder HoverAction(string action);

        /// <summary>Sets the sound played when the element is clicked.</summary>
        IElementBuilder Sound(string sound);

        /// <summary>A game state query deciding whether the element appears.</summary>
        IElementBuilder Condition(string condition);

        /// <summary>How the element sizes itself: "Fill", "ShrinkToFit" or "Fixed".</summary>
        IElementBuilder Sizing(string sizingMode);

        /// <summary>Sets the element's width, in unscaled pixels multiplied by its scale. On a Panel, Divider or Banner this is only used when Sizing is "Fixed".
        /// On a Paragraph it applies on its own, wrapping the text at that width and reserving it.</summary>
        IElementBuilder Width(int width);
        IElementBuilder Height(int height);
        IElementBuilder Padding(int padding);

        /// <summary>Extra space left below the element.</summary>
        IElementBuilder Spacing(int spacingAfter);

        IElementBuilder Margin(int left, int right);

        /// <summary>The hover tooltip's title and body.</summary>
        IElementBuilder Tooltip(string displayName, string description);

        /// <summary>Adds an animation frame, on an Image. The frame takes its size from the element's source rectangle.</summary>
        /// <param name="x">The frame's source X, in the texture.</param>
        /// <param name="y">The frame's source Y, in the texture.</param>
        /// <param name="duration">How long the frame is shown, in milliseconds. 0 leaves it to the element's FrameDuration.</param>
        /// <param name="scale">A multiplier on the element's scale while this frame draws. It's a draw-time effect, so a frame above 1
        /// overhangs its own bounds rather than pushing anything aside.</param>
        /// <param name="condition">A game state query deciding whether the frame plays. When every frame's condition fails, the element
        /// falls back to drawing its source rectangle.</param>
        IElementBuilder AddFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null);

        /// <summary>Adds a frame played while the cursor is over the element, replacing the idle frames for as long as it stays there.
        /// When the hover frames are empty or fully conditioned out, the idle animation carries on rather than the element going still.</summary>
        IElementBuilder AddHoverFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null);

        /// <summary>What a PageNumber counts from: "Book" or "Chapter".</summary>
        IElementBuilder Scope(string scope);

        /// <summary>A wrapper around a PageNumber's number, where {0} is the number, such as "Page {0}" or "- {0} -".</summary>
        IElementBuilder Format(string format);

        /// <summary>Adds a child element, on a container such as a Panel.</summary>
        IElementBuilder AddChild(string elementType);
    }
}
