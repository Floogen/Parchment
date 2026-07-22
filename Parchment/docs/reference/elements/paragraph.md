# Paragraph

Body text. Identical to [`Title`](title.md) and [`Heading`](heading.md) apart from its default font.

```json
{
  "Type": "Paragraph",
  "Text": "Pitch your tent on level ground, away from the river.",
  "MarginLeft": 16
}
```

Text wraps to the available width automatically. A word too long to fit on a line by itself is broken across lines rather than left hanging off the edge, so item IDs and long compound words are safe.

Use `\n` for a deliberate line break, and `\n\n` for a blank line between paragraphs.

## Text fields

`FontType` defaults to **`Small`**, the game's small font.

--8<-- "text-content.md"

## Common fields

`Scale` on a `Paragraph` is the **font** scale, since a paragraph has no sprite. `MarginLeft` indents it, and because the margin narrows the width the text measures against, an indented paragraph wraps at the indented width rather than running off the page.

--8<-- "element-common.md"

!!! warning "Long text can overflow the page"
    Nothing stops a paragraph from stacking past the bottom of a page. Parchment logs a warning, but you're responsible for fitting content to pages. Bear translations in mind: text that fits exactly in English may not in a longer language.
