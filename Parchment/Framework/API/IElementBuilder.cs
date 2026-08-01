namespace Parchment.Framework.API
{
    /// <summary>Builds one element. Obtained from the add methods on <see cref="IPageBuilder"/> and <see cref="IBookBuilder"/>.</summary>
    public interface IElementBuilder
    {
        /// <summary>The element's type name.</summary>
        string ElementType { get; }

        /// <summary>The element's ID, or empty when it hasn't been given one. Unlike the other builders an element is created without an ID, since most never need one, so this reports what WithId set rather than what the element was made with.</summary>
        string ElementId { get; }

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

        /// <summary>The handle conditions and actions use to reach an Input's text, such as "PeacefulEnd.Parchment_InputMatches search Tulip".</summary>
        IElementBuilder InputId(string inputId);

        /// <summary>The prompt shown while an Input is empty.</summary>
        IElementBuilder Placeholder(string placeholder);

        /// <summary>The most characters an Input accepts.</summary>
        IElementBuilder MaxLength(int maxLength);

        /// <summary>Adds a trigger action to run when the reader presses enter in an Input. Calling this more than once builds a list run in order.</summary>
        IElementBuilder SubmitAction(string action);

        /// <summary>Adds a trigger action to run once an Input's text has stopped changing for its TextChangedDelay. Calling this more than once builds a list run in order.
        /// The wait restarts on every change, so a typed word runs them once rather than once per letter.</summary>
        IElementBuilder TextChangedAction(string action);

        /// <summary>How long an Input's text has to sit still before its text changed actions run, in milliseconds. Defaults to 250.</summary>
        IElementBuilder TextChangedDelay(float textChangedDelay);

        /// <summary>Sets the sound played when the element is clicked.</summary>
        IElementBuilder Sound(string sound);

        /// <summary>A game state query deciding whether the element appears.</summary>
        IElementBuilder Condition(string condition);

        /// <summary>How long the element stays up once shown, in seconds. Setting this makes the element a timed one: it starts hidden and only appears when a ShowElement action names it, so it needs an Id too.
        /// Showing it again while it's up restarts the count rather than stacking another on.
        /// </summary>
        IElementBuilder Lifetime(float lifetime);

        /// <summary>How long the element holds before it starts fading, in seconds, reaching nothing at the end of its Lifetime. Left alone, it holds the whole time and then goes at once.</summary>
        IElementBuilder FadeAfter(float fadeAfter);

        /// <summary>Lets the cursor pass straight through the element to whatever sits beneath it, leaving it unhoverable and unclickable.
        /// It doesn't carry down, so a decorative container can let the cursor through while the elements inside it stay reachable.
        /// Registration fails when the element also carries a click or hover action, or when its type has to be clickable to work at all, such as an Input.
        /// </summary>
        IElementBuilder IgnoreCursor(bool ignoreCursor = true);

        /// <summary>How the element sizes itself: "Fill", "ShrinkToFit" or "Fixed".</summary>
        IElementBuilder Sizing(string sizingMode);

        /// <summary>Sets the element's width, in unscaled pixels multiplied by its scale. On a Panel, Divider or Banner this is only used when Sizing is "Fixed".
        /// On a Paragraph it applies on its own, wrapping the text at that width and reserving it.</summary>
        IElementBuilder Width(int width);
        IElementBuilder Height(int height);
        IElementBuilder Padding(int padding);

        /// <summary>How many cells sit side by side in a Grid before the next row starts.</summary>
        IElementBuilder Columns(int columns);

        /// <summary>The most rows a Grid draws. Cells past the last row are dropped, the way a page drops content past its bottom.</summary>
        IElementBuilder Rows(int rows);

        /// <summary>A Grid cell's width, in unscaled pixels multiplied by the element's scale. The same for every cell, so one child can't resize the others.</summary>
        IElementBuilder CellWidth(int cellWidth);

        /// <summary>A Grid cell's height, in unscaled pixels multiplied by the element's scale.</summary>
        IElementBuilder CellHeight(int cellHeight);

        /// <summary>Fills a Grid's cells from an item query rather than from its children, such as "ALL_ITEMS (O)". Pair it with AddSourceTemplate, which is what each cell is built from.</summary>
        IElementBuilder Source(string itemQuery);

        /// <summary>The InputId whose text narrows a Grid's candidates. Without one the results are unfiltered.</summary>
        IElementBuilder SourceFilter(string inputId);

        /// <summary>A game state query each candidate must pass, evaluated with that item in context, such as "ITEM_CATEGORY Target -4".</summary>
        IElementBuilder SourceCondition(string perItemCondition);

        /// <summary>The item property a Grid's candidates are sorted by, named as the %Item.Something% token names it, such as "Name", "Category" or "Price". Pass "None" to leave them in the item query's own order.</summary>
        IElementBuilder SourceOrder(string order);

        /// <summary>Reverses a Grid's order, so the highest price or the last name comes first. Candidates that can't answer the property still sort last.</summary>
        IElementBuilder SourceOrderDescending(bool descending);

        /// <summary>How many cells a Grid's results fill. When left out, the grid's Columns and Rows decide.</summary>
        IElementBuilder SourceCount(int count);

        /// <summary>Adds the element each of a Grid's cells is built from. The item is applied to any Image inside it that has no item or texture of its own.</summary>
        IElementBuilder AddSourceTemplate(string elementType);

        /// <summary>The space between a Grid's columns and rows, in unscaled pixels multiplied by the element's scale. Not applied outside the outermost cells, which is what Padding is for.</summary>
        IElementBuilder CellSpacing(int columnSpacing, int rowSpacing);

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

        /// <summary>Adds an animation frame that keeps whatever the element already draws, varying only its timing, scale or condition.
        /// This is how an item's icon is animated, since Item draws a sprite whose place in the sheet isn't yours to know.</summary>
        /// <param name="duration">How long the frame is shown, in milliseconds. 0 leaves it to the element's FrameDuration.</param>
        /// <param name="scale">A multiplier on the element's scale while this frame draws.</param>
        /// <param name="condition">A game state query deciding whether the frame plays.</param>
        IElementBuilder AddFrameInPlace(float duration = 0f, float scale = 1f, string? condition = null);

        /// <summary>Adds a frame played while the cursor is over the element, replacing the idle frames for as long as it stays there.
        /// When the hover frames are empty or fully conditioned out, the idle animation carries on rather than the element going still.</summary>
        IElementBuilder AddHoverFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null);

        /// <summary>Adds a hover frame that keeps whatever the element already draws, varying only its timing, scale or condition.</summary>
        IElementBuilder AddHoverFrameInPlace(float duration = 0f, float scale = 1f, string? condition = null);

        /// <summary>Shifts the frame added last, whether idle or hover, without moving where the element sits. Measured in unscaled sprite pixels multiplied by the element's scale.
        /// A draw-time effect like a frame's scale, so the element keeps the space and the hitbox it was measured with. Unlike scale it carries any text along with the sprite.
        /// Registration fails when no frame has been added yet, since there would be nothing for the offset to belong to.
        /// </summary>
        /// <param name="x">How far right to shift the frame. Negative moves left.</param>
        /// <param name="y">How far down to shift the frame. Negative moves up, which is what lifts art under the cursor.</param>
        IElementBuilder FrameOffset(int x, int y);

        /// <summary>Adds a trigger action to the frame added last, run each time that frame starts. Calling this more than once builds a list run in order.
        /// A looping animation runs them again on every cycle, so keep the whole list harmless to repeat or condition the frames so the loop stops.
        /// Registration fails when no frame has been added yet, since there would be nothing for the action to belong to.
        /// </summary>
        IElementBuilder FrameAction(string action);

        /// <summary>What a PageNumber counts from: "Book" or "Chapter".</summary>
        IElementBuilder Scope(string scope);

        /// <summary>A wrapper around a PageNumber's number, where {0} is the number, such as "Page {0}" or "- {0} -".</summary>
        IElementBuilder Format(string format);

        /// <summary>Adds a child element, on a container such as a Panel.</summary>
        IElementBuilder AddChild(string elementType);

        /// <summary>Adds an element drawn behind a container's children, placed at its own Position within the container's content area rather than stacked. Available on a Panel.</summary>
        IElementBuilder AddBackground(string elementType);

        /// <summary>Adds an element drawn over a container's children, placed at its own Position within the container's content area rather than stacked. Available on a Panel.</summary>
        IElementBuilder AddForeground(string elementType);
    }
}
