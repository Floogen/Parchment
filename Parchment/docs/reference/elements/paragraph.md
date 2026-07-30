# Paragraph

Body text. Identical to [`Title`](title.md) and [`Heading`](heading.md) apart from its default font and its optional `Width`.

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

## Text fields

`FontType` defaults to **`Small`**, the game's small font.

--8<-- "text-content.md"

## Common fields

`Scale` on a `Paragraph` is the **font** scale, since a paragraph has no sprite. `MarginLeft` indents it, and because the margin narrows the width the text measures against, an indented paragraph wraps at the indented width rather than running off the page.

--8<-- "element-common.md"

!!! warning "Long text can overflow the page"
    Nothing stops a paragraph from stacking past the bottom of a page. Parchment logs a warning, but you're responsible for fitting content to pages. Bear translations in mind: text that fits exactly in English may not in a longer language.
