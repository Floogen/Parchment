# Troubleshooting

Most Parchment problems fail quietly. A number that's zero when it should be sixteen, a source rectangle that's the whole sheet instead of one sprite, a query that's false because it's misspelled. None of these throw, they just produce nothing, or something subtly wrong. That's what makes them annoying, and it's why this page is organised by what you *see* rather than by what's broken.

## Start here

```
parchment_debug
```

This outlines every element's bounds in green, the page content areas in red and the corner hotspots in cyan. Before guessing, look. Most of the entries below are distinguishable in one glance:

- **A green box in the right place, but empty**: the element is laid out correctly and drawing nothing. It's a texture or a source rectangle problem.
- **A green box that's the wrong size**: it's a measuring problem. Check the source rectangle and `Scale`.
- **No green box at all**: the element was dropped or hidden. It's a condition, an overflow or a validation failure.

Then check the SMAPI log. Parchment warns when it drops, shrinks or truncates something, and the warning names the element.

## Nothing renders at all

**The whole book is blank.** The page content area is degenerate. The margins have eaten the page. Check `Layout` on the book: those are in *unscaled sprite pixels* and get multiplied by `Appearance.Scale`, so a `MarginTop` of 175 on a 158-pixel-tall frame leaves nothing. If you converted from screen pixels, divide by the scale.

**The book doesn't open.** Check that the item's `CustomFields` key is exactly `PeacefulEnd.Parchment/CustomFields/BookId`, and that its value matches a book's `Id`. Then check that the book's entry key and its `Id` field are identical. Content Patcher keys list entries by their `Id`, and a mismatch means the book isn't where anyone's looking for it.

**`parchment_open` says nothing.** The book ID is wrong. It's the `Id` field, not the entry key, not the item's name, though if you've done step three right, all three are the same string.

## An element is missing

**It's conditional and never appears.** A malformed [game state query](concepts/conditions.md) is false, not an error, so a typo looks exactly like a condition that didn't pass. Check the spelling character by character: `PeacefulEnd.Parchment_CurrentPageIndex`, not `CurrentPageNumber` and not `Parchment_currentPageIndex`.

Remember the scope trap: `IsFirstPage` and `IsLastPage` ask about the **book**, while the `FirstPage` and `LastPage` actions work on the **current chapter**. `!IsLastPage` won't hide a button at a chapter's end.

**It's at the bottom of a full page.** Elements that stack past the page are dropped with a warning. Parchment doesn't reflow (see [Layout](concepts/layout.md#when-content-doesnt-fit)).

**It failed validation.** A bad element is skipped at load with a warning naming the problem. `Sizing: Fixed` without `Width`, a `Button` without an `Action`, animation frames that don't match the source rectangle's size. All of these drop the element rather than the book.

**It's an image with an `ItemId` that doesn't exist.** Check the qualified ID, including the prefix: `(O)24`, not `24`.

## Text problems

**The text is enormous.** `FontType: SpriteText` at `TextScale: 1`. SpriteText is the game's menu-header font and its natural size is large. A dozen characters is around 300 pixels wide. Use `Small` for anything that isn't a title.

**The text vanished but the element is there.** It was truncated to fit and there was nothing left. Check the log. The warning names the element and the height it had to work with. Usually the container is too short, or `TextScale` is too big for it.

**The text wraps too early.** Something narrowed the width it measures against: a `MarginLeft`, a panel's `Padding` or a container that's narrower than you think. Margins narrow the measuring width deliberately, so an indented paragraph wraps at the indented width.

**The last lines are missing from a panel.** A fixed-`Height` panel truncates its contents rather than growing. Drop the `Height` or shorten the text.

## Art problems

**The picture is huge, or scaled down when it shouldn't be.** `TextureSourceRectangle` is missing, so the whole sprite sheet became the element. This is *especially* likely on an animated image. `Frames` says what to draw, but `TextureSourceRectangle` is what sets the size, and it's required when animating.

**The art looks smeared or has uneven pixels.** Either the scale isn't a whole number (usually because Parchment shrank an oversized image to fit the column, which it warns about) or a nine-sliced frame has detail crossing a slice boundary. See [Preparing your art](concepts/art.md).

**The sprite sits oddly inside its box.** Transparent padding inside the source rectangle. The element reserves space for the rectangle, not for the pixels you painted in it. Tighten the rectangle to the art.

**Flipping moved the sprite sideways.** Same cause. `SpriteEffects` mirrors the sprite *within its bounds* and can't move the element. If the art appears to shift, it has uneven transparent padding, and it moved by twice the difference.

**The hover state overhangs the element.** `HoverTextureSourceRectangle` is a different size from `TextureSourceRectangle`. The layout is measured from the normal rectangle, so the hover sprite draws at the normal one's scale and spills. Make them the same size.

**An animation went still.** Every frame carried a `Condition` and none of them passed, so the element fell back to drawing `TextureSourceRectangle`. That's the designed behaviour, not a failure. If the still it lands on looks wrong, aim the source rectangle at a better cell.

**One frame never shows up.** Its `Condition` isn't passing. A malformed query is false rather than an error, so the frame is quietly skipped and the animation just runs shorter. Check the query by hand.

**The tint did nothing, or turned everything black.** Tints multiply. Red on grey gives red. Red on blue gives near-black. Anything on black stays black. Tinting wants neutral or greyscale art.

## Clicks and actions

**A button does nothing.** Check the log. A typo'd action fails at click time with a warning naming the string you wrote. Nothing validates action names up front.

**A navigation button does nothing on some pages.** It's doing exactly what it says. `PreviousPage` on the first page of a chapter has nowhere to go. There's no way to grey a button out. Use a `Condition` to hide it instead.

**Clicks land on the wrong thing.** Underlay elements are hit-tested last, so pages win, but the book's *margins* aren't covered by any page, so an underlay behind them can claim clicks.

**A background or foreground element shows no tooltip.** It has neither a `DisplayName` nor a `Description`, so it's treated as decoration and the cursor passes through it.

**A foreground element covers the buttons under it.** It's interactive, so it does claim the cursor over its whole rectangle. Either shrink it to the art or drop the `Action`, `HoverAction`, tooltip or `HoverTextureSourceRectangle` that made it interactive in the first place.

**A background tooltip appears over a foreground one.** It can't. Foreground wins, then the stacked elements, then background. If the wrong tooltip appears, two elements overlap and the one you didn't expect is higher in that order. `parchment_debug` will show both boxes.

## Placement

**A positioned element didn't move when I changed `Scale`.** `Position` is in screen pixels and deliberately doesn't scale. Changing `Scale` resizes the element from its top-left. See [Units and scale](concepts/units.md).

**Alignment does nothing.** The element is already as wide as its container, so there's no slack to align within. A `Fill` panel can't be centred. Give it a `Width` and it can.

**Text inside a panel is centred on the panel, not the page.** Working as intended. Alignment is resolved within the containing element, and a panel's children measure against the panel.

## Still stuck

If an element is laid out right and drawing nothing, and the log is silent, the most likely culprit is a source rectangle that's valid but wrong, pointing at empty space on the sheet. `parchment_debug` will show the box. Open the PNG and check the coordinates.
