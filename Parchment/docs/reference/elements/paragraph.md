# Paragraph

Body text. Identical to [`Title`](title.md) and [`Heading`](heading.md) apart from its default font, its optional `Width` and its optional `Height`, which lets long text [scroll](#scrolling) within a fixed space.

```json
{
  "Type": "Paragraph",
  "Text": "Pitch your tent on level ground, away from the river.",
  "MarginLeft": 16
}
```

Text wraps to the available width automatically. A word too long to fit on a line by itself is broken across lines rather than left hanging off the edge, so item IDs and long compound words are safe.

Use `\n` for a deliberate line break, and `\n\n` for a blank line between paragraphs.

## Paragraph fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Width` <span class="opt">optional</span> | `int?` | *the available width* | The width the paragraph occupies, in unscaled pixels × `Scale`. Text wraps at it and the paragraph reserves it. Clamped to the width available, so it can only narrow the paragraph and never widen it. |
| `Height` <span class="opt">optional</span> | `int?` | *as tall as its text* | The height the paragraph occupies, in unscaled pixels × `Scale`. The paragraph reserves it whether or not its text fills it. Text that runs past the bottom is handled by `Overflow`. Clamped to the height left on the page. See [Scrolling](#scrolling). |
| `Overflow` <span class="opt">optional</span> | `string` | `Scroll` | What happens to text that runs past `Height`. `Scroll` lets the reader scroll it into view and `Truncate` leaves it out. Ignored without a `Height`. |
| `ScrollBar` <span class="opt">optional</span> | [`ScrollBar`](#scroll-bar) | *a thin bar in the text color* | How the scroll bar looks. |

Give `Width` when you want a narrower column than the page provides, such as text set beside an image or a short line you want broken at a particular point.

```json
{
  "Type": "Paragraph",
  "Text": "Pitch your tent on level ground, away from the river.",
  "Width": 120,
  "Alignment": "Center"
}
```

!!! note "`Width` changes what `Alignment` aligns against"
    Without a `Width` a paragraph is only as wide as its longest line, so `Alignment` centres that block within the column. With a `Width` the paragraph is exactly that wide wherever the column allows it, so `Alignment` centres each line within the `Width` box and the box itself sits at the start of the column. Give the box an `Alignment` of its own by wrapping it in a [`Panel`](panel.md), or offset it with `Position`.

## Scrolling

Give a paragraph a `Height` and it shows its text through a window that tall. When the text runs past the bottom, the reader can scroll through the rest:

- the **mouse wheel**, with the cursor over the paragraph
- the **right stick** on a controller, with the cursor over the paragraph
- the **scroll bar** down the paragraph's right side, by dragging its thumb or clicking above or below it to move a page at a time

```json
{
  "Type": "Paragraph",
  "Text": "The legend speaks of a fish that only bites in the rain, deep in the mountain lake...",
  "Height": 120
}
```

Text moves a whole line at a time, so a line is never cut in half at the edge of the window. The bar is only drawn while there is more text than fits, which is also the only time the text wraps short of it. A paragraph whose text fits keeps its full width and shows no bar.

Without a `Width`, a paragraph with text to scroll takes the full width available, so its bar sits at the edge of the column rather than just past its longest line.

!!! note "Links scroll with their text"
    A [link](index.md#links) on a line scrolled out of view can't be hovered, clicked or stepped onto with a controller until it is scrolled back into view.

!!! tip "Typewriters keep their newest line in view"
    A [typewriter](index.md#text-effects) in a scrolling paragraph scrolls it down to each new line as it types. Once the reader scrolls the paragraph themselves it stays where they put it. Clicking to finish the typing shows the rest of the text without moving the window.

The reader's place is kept when the book is [refreshed](../api.md), so a rebuild doesn't send them back to the top.

A paragraph with text to scroll counts as something worth stopping at for a [controller](../../concepts/controller.md#stepping-between-elements), even without a tooltip or an action, so a reader on a controller can reach it to scroll it.

!!! warning "`IgnoreCursor` stops the reader scrolling"
    The mouse wheel, the right stick and a click on its bar all pass straight through a paragraph with `IgnoreCursor`, the same as the cursor does. A typewriter still scrolls it.

### Leaving the rest out

Set `Overflow` to `Truncate` to show only the lines that fit, with no bar and nothing to scroll.

```json
{
  "Type": "Paragraph",
  "Text": "A short note that only ever needs its first few lines shown.",
  "Height": 60,
  "Overflow": "Truncate"
}
```

### Scroll bar

A flat bar in a single color. Every field is optional. The colors follow the paragraph's `TextColor` when left off, so the bar matches the page without being styled.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Width` <span class="opt">optional</span> | `int` | `6` | How wide the bar is drawn, in unscaled pixels × `Scale`. |
| `Gap` <span class="opt">optional</span> | `int` | `6` | The room left between the text and the bar, in unscaled pixels × `Scale`. |
| `Color` <span class="opt">optional</span> | [`color`](index.md#colors) | *the text color, faint* | The color of the thumb, the part that moves. |
| `HoverColor` <span class="opt">optional</span> | [`color`](index.md#colors) | *see below* | The thumb's color while the cursor is over the bar. Defaults to `Color` when that is given. Without one it is a stronger take on the text color. |
| `ActiveColor` <span class="opt">optional</span> | [`color`](index.md#colors) | *`HoverColor`* | The thumb's color while it is being dragged. |
| `TrackColor` <span class="opt">optional</span> | [`color`](index.md#colors) | — | The color of the channel the thumb runs along. Left off, only the thumb is drawn. |

```json
{
  "Type": "Paragraph",
  "Text": "...",
  "Height": 160,
  "ScrollBar": {
    "Width": 4,
    "Color": "139 90 43 120",
    "HoverColor": "139 90 43",
    "TrackColor": "0 0 0 30"
  }
}
```

The cursor reaches the bar through a strip at least 16 pixels wide down the paragraph's right side, so a thin bar is still easy to take hold of.

!!! note "Give `HoverColor` when you give `Color`"
    The default highlight is built from the text color, so once you choose the thumb's `Color` the bar no longer changes on hover until you choose a `HoverColor` too.

## Text fields

`FontType` defaults to **`Small`**, the game's small font.

--8<-- "text-content.md"

## Link fields

--8<-- "link-fields.md"

## Common fields

`Scale` on a `Paragraph` is the **font** scale, since a paragraph has no sprite. `MarginLeft` indents it, and because the margin narrows the width the text measures against, an indented paragraph wraps at the indented width rather than running off the page.

--8<-- "element-common.md"

!!! warning "Long text can overflow the page"
    Nothing stops a paragraph from stacking past the bottom of a page. Parchment logs a warning, but you're responsible for fitting content to pages. Bear translations in mind: text that fits exactly in English may not in a longer language. Giving the paragraph a `Height` keeps it to a fixed space however long the text gets, with the rest a [scroll](#scrolling) away.
