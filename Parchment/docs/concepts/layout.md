# Layout

A page is a stack. Elements are laid out top to bottom in the order you list them, each as wide as it needs to be and each pushing the next one down. There's no grid and no columns. A book is a column of things, and the only questions are how wide each thing is, where it sits and how much space is around it.

## Padding, margins and spacing

Three words, three different gaps. Conflating them is the most common way to end up with a mystery hole in a page.

**`Padding`** is inside a container's border. It belongs to [`Panel`](../reference/elements/panel.md), [`Banner`](../reference/elements/banner.md) and [`Button`](../reference/elements/button.md), the elements that draw a frame around something. `Padding: 0` means the contents sit flush against the inside of the border, not against the outside of the element. You never have to account for the border's own thickness. That's the container's business.

**`MarginLeft`** and **`MarginRight`** are outside an element's own box, and every element has them. They're how you indent a paragraph. Crucially, a margin *narrows the width the element measures against*, so an indented paragraph wraps at the indented width rather than running off the page. Sizing and text wrapping happen inside the margins, not around them.

**`SpacingAfter`** is between an element and the next one. It belongs to neither element exactly. It's the gap. That's why it isn't applied after the last visible element. A gap with nothing on one side of it isn't a gap, it's an accident and it would show up as phantom space at the bottom of a page or inside a panel's border.

There's no `SpacingBefore` or vertical margin. If you want a gap above something, put it on the `SpacingAfter` of whatever's above it, or if there's nothing above it, that's the container's `Padding`, or the book's [`MarginTop`](../reference/book.md#layout).

## Width

Most elements are as wide as their container. Text fills the column and wraps. A [`Panel`](../reference/elements/panel.md) or [`Divider`](../reference/elements/divider.md) defaults to `Fill`.

The exceptions are the elements that know their own size. An [`Image`](../reference/elements/image.md) is exactly its sprite (source rectangle times `Scale`) and never anything else. A `ShrinkToFit` [`Banner`](../reference/elements/banner.md), `Button` or `Panel` is exactly as wide as its contents need.

In every case the result is clamped to the space available, so nothing can be wider than what contains it. What happens when it doesn't fit differs by element: an image is scaled down, text wraps, a panel squeezes its children. All three log a warning.

Containers narrow the space for their contents. A panel's children measure against the panel's inner width, not the page's, so a paragraph inside a narrow panel wraps at the panel's width, and a nested panel narrows it again. Each level only ever knows about the level above it.

## Alignment

`Alignment` does two things, and they compose.

It places the **element** within its container, which only has an effect when the element is narrower than the space available. A `Fill` panel has no slack, so its `Alignment` does nothing. Give the same panel a `Width` and it starts mattering, which is why alignment often seems to "start working" the moment you set a width. This holds for [placed elements](#placed-elements) too, where the container is the page's content area or the book itself.

It also aligns each **line of text** within the element. A centred three-line paragraph gets its block placed on the page *and* each line centred within the block, so the ragged edges fall where you'd expect.

Both happen from the same field, which is almost always what you want. The exception is [`Banner`](../reference/elements/banner.md), whose text is always centred in the scroll regardless of where the banner itself sits. A right-aligned banner with left-shoved text would just look broken.

An [`Image`](../reference/elements/image.md) with text on it separates them properly: `Alignment` places the picture, `TextAlignment` places the words on it.

There's a `VerticalAlignment` too, but only for [placed elements](#placed-elements). A stacked element's vertical position is whatever the elements above it leave, so there's nothing for it to decide. Setting it on a stacked element logs an error to SMAPI and changes nothing, rather than failing quietly.

## Placed elements

Not everything stacks. Elements in a page's `Background` and `Foreground`, or the book's `Underlay` and `Overlay`, are placed by their `Position` instead, a screen-pixel coordinate relative to the page's content area or the book's top-left.

Placed elements don't participate in the layout at all. They can't push anything, nothing pushes them and they can overlap freely. Negative coordinates are fine and are how you hang a bookmark off the side of the book.

Because `Position` is a coordinate rather than a measurement, it's the one field that **doesn't** scale (see [Units and scale](units.md)). Changing a placed element's `Scale` grows it from its top-left rather than moving it.

Placed elements are still fully featured. One in a page's `Background` or `Foreground` can carry a tooltip, an `Action` or a `HoverAction`, and it's tested against the cursor in drawing order from the top down: foreground, then the stacked elements, then background. An element in those two lists with nothing to offer is [skipped entirely](../reference/page.md#background-and-foreground) so decorative art can overlap a button without stealing its clicks.

### Alignment anchors, position offsets

`Alignment` and `VerticalAlignment` both apply to a placed element, and they decide where `Position` counts from. The element is anchored within the container first, then moved by `Position`:

| `Alignment` | Where `X: 0` lands | What a positive `X` does |
| --- | --- | --- |
| `Left` | The container's left edge. | Moves right. |
| `Center` | Horizontally centred. | Moves right of centre. |
| `Right` | Flush against the right edge. | Pushes past the right edge, so you'll usually want a negative value here. |

| `VerticalAlignment` | Where `Y: 0` lands | What a positive `Y` does |
| --- | --- | --- |
| `Top` | The container's top edge. | Moves down. |
| `Center` | Vertically centred. | Moves below centre. |
| `Bottom` | Flush against the bottom edge. | Pushes past the bottom edge, so you'll usually want a negative value here. |

The two are independent, so you can centre horizontally and pin to the bottom. The defaults are `Left` and `Top`, both anchoring at zero, which is why `Position` reads as a plain coordinate until you set something else. Centring a flourish on a page therefore needs no arithmetic against the page's size:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/flourish",
  "Alignment": "Center",
  "VerticalAlignment": "Bottom",
  "Position": { "X": 0, "Y": -24 }
}
```

That sits the flourish along the bottom of the page, 24px up from the edge, wherever the page's content area happens to fall. The book's `Underlay` and `Overlay` anchor against the book sprite instead, so the same element there would sit against the bottom of the book rather than the bottom of a page.

The usual caveat applies on both axes: an element with no slack can't move. A default `Panel` or `Divider` fills the width, so aligning one does nothing until it has a `Width`. A `Paragraph` in a placed list wraps at the full container width, so its block only shifts by however much its longest line falls short. Vertically there's almost always slack, since a placed element is rarely as tall as the page.

!!! warning "`VerticalAlignment` is for placed elements only"
    On a stacked element it does nothing, because the elements above it already decide where it starts. Parchment logs an error naming the element so it doesn't look like the field silently broke. To push a whole page's content down, use the `SpacingAfter` of the element above it or the book's [`MarginTop`](../reference/book.md#layout).

!!! note "Margins don't apply to placed elements"
    `MarginLeft` and `MarginRight` are ignored here, so alignment measures against the container's full width and height. Inset a placed element with `Position` instead, which is what it's for.

## When content doesn't fit

Parchment doesn't reflow. If a page's elements stack past the bottom, the ones that start below the fold are dropped and a warning goes to the log. A paragraph that's too tall for a fixed-height panel loses its last lines rather than drawing through the border.

That's a deliberate choice: a book is a set of authored spreads, and a page break that moves on its own isn't a page break. Fitting the content to the page is your job, and the warnings are there to tell you when you haven't.

One corollary to plan for early: **text that fits exactly in one language may not in another**. German and Russian run noticeably longer than English. If your book will be translated, leave headroom rather than filling every page to the last line.

If you find yourself fighting the page, the debug view is faster than guessing. `parchment_debug` in the console outlines every element's bounds, which usually makes the answer obvious in one look.
