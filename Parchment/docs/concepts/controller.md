# Controller support

Every book Parchment opens can be read on a controller. Nothing has to be authored for it, though a few of the choices you make while authoring decide how well it reads.

## The buttons

| Button | What it does |
| --- | --- |
| Left stick / D-pad | Steps the cursor between the things on the spread worth stopping at. |
| ++"A"++ | Acts on whatever the cursor is standing on, the same as a left click. |
| ++"B"++ | Closes the book. Held for three seconds when a page has taken the button over. |
| ++"LT"++ / ++"RT"++ | Turns back a page and forward a page. |

The triggers are offered after the page's own [keybinds](../reference/page.md), so a book that binds `LeftTrigger` or `RightTrigger` for itself keeps them and no page is turned underneath it.

## Stepping between elements

With **Use Controller-Style Menus** switched on (Options, under Controller), the game stops moving the cursor from the stick and hands the direction to the menu instead. Parchment answers that by gathering everything on the spread the cursor could usefully stand on, then moving to whichever of them lies that way.

An element is worth stopping at when hovering or clicking it would do something, which is any of:

- a `DisplayName` or a `Description`, being a tooltip to read
- `OnClick` or `OnHover` actions
- a `HoverTextureSourceRectangle` or `HoverFrames`
- any `Tags`
- `IsAlwaysInteractive` set to `true`

Everything else is stepped over. `IgnoreCursor` takes an element out of the walk entirely, the same as it does for the mouse.

!!! tip
    A page of nothing but paragraphs has nothing to stop at, which is correct: there is nothing on it to press. The reader turns through it with the triggers.

The page turn corners are stepped onto like anything else, while there is a page that way, so a reader can reach them without knowing the triggers exist.

With **Use Controller-Style Menus** switched off, the stick moves the cursor freely and none of the above applies.

## Where the reader starts

A book opens with the cursor on the first thing worth stopping at, in the order the page was authored: the left page before the right, and an element before whatever it holds. A book's `Underlay` and `Overlay` come after both pages, so a bookmark bar down the side is reached by stepping to it rather than by starting there.

The cursor stays where it is through anything that leaves the element in place, follows the element when a relayout moves it, and finds it again by `Id` after a [refresh](../reference/api.md) rebuilds the book. An element that a condition takes away hands the cursor to whatever is nearest to where it stood.

!!! note
    Give an `Id` to anything a reader will be standing on when the book rebuilds itself. Without one there is nothing to find it again by, and the cursor falls back to the nearest target instead.

## Text inputs

Pressing ++"A"++ on an [Input](../reference/elements/input.md) element opens the game's on-screen keyboard over the book, the same one a vanilla naming box uses. Snappy menus is what decides this: with it on the keyboard comes up, with it off the box takes the hardware keyboard as before.

The text is written through to the input as it is entered, so a list filtered on that input is already up to date once the keyboard closes. Closing the keyboard stands in for ++"Enter"++ and runs the input's `SubmitActions`, since a controller reader has no other way to submit.

While an input is focused the stick stays off the cursor, so the reader doesn't step away mid-word.

!!! note
    `MaxLength` is enforced by Parchment rather than by the keyboard, so text past the limit is cut back as it is entered.
