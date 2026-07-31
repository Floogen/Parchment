# Elements

An element is one piece of content on a page: a heading, a picture, a framed callout. Every element has a `Type`, which decides which other fields it understands.

```json
{
  "Type": "Heading",
  "Text": "Setting up camp",
  "Alignment": "Center"
}
```

Elements appear in five places, and where they appear changes how they're positioned:

| Where | Positioning |
| --- | --- |
| [`Page.Elements`](../page.md) | Stacked top to bottom. |
| [`Page.Background`](../page.md) | Placed by `Position`, drawn behind the page's elements. |
| [`Page.Foreground`](../page.md) | Placed by `Position`, drawn over the page's elements. |
| [`Book.Underlay`](../book.md) | Placed by `Position` relative to the book, drawn behind the book sprite. |
| [`Book.Overlay`](../book.md) | Placed by `Position` relative to the book, drawn in front of everything. |

Where an element appears doesn't change what it can do. A tooltip, an `Action` or a `HoverAction` works the same in any of the five. Note that an element in `Page.Background` or `Page.Foreground` with none of those is [transparent to the cursor](../page.md#background-and-foreground), so decorative art doesn't cover the page.

---

## Element types

<div class="grid cards" markdown>

-   **[Title](title.md)** (`Title`)

    Large heading text.

-   **[Heading](heading.md)** (`Heading`)

    Section heading text.

-   **[Paragraph](paragraph.md)** (`Paragraph`)

    Body text.

-   **[Image](image.md)** (`Image`)

    A sprite, an animation or an item's icon, optionally with text drawn on it.

-   **[Divider](divider.md)** (`Divider`)

    A horizontal rule, plain or decorative.

-   **[Panel](panel.md)** (`Panel`)

    A nine-sliced frame containing other elements.

-   **[Banner](banner.md)** (`Banner`)

    A three-sliced strip with text in the middle, a scroll or ribbon.

-   **[Button](button.md)** (`Button`)

    A nine-sliced frame with a label, for running an action.

-   **[Page number](page-number.md)** (`PageNumber`)

    The page's own number, filled in automatically.

-   **[Grid](grid.md)** (`Grid`)

    A container laying its children out across fixed-size cells.

-   **[Input](input.md)** (`Input`)

    A text box the reader types into, for filtering a page against what they've typed.

</div>

An unrecognised `Type` is skipped with a warning rather than breaking the book.

---

## Common fields

Every element understands these, whatever its type.

--8<-- "element-common.md"

!!! tip "Any element can be clickable"
    `Action` lives on every element, not just `Button`. An `Image` with an `Action` is a perfectly good bookmark or tab. `Button` is just the shorthand for the common case of a framed label.

## Text fields

Understood by [`Title`](title.md), [`Heading`](heading.md), [`Paragraph`](paragraph.md), [`Banner`](banner.md), [`Button`](button.md), [`Image`](image.md) and [`Input`](input.md).

--8<-- "text-content.md"

## Sprite fields

Understood by [`Image`](image.md), [`Panel`](panel.md), [`Grid`](grid.md), [`Banner`](banner.md), [`Button`](button.md), [`Divider`](divider.md) and [`Input`](input.md).

--8<-- "sprite.md"

---

## Font types

| Value | What it is |
| --- | --- |
| `Dialogue` | The game's main dialogue font. Large. |
| `Small` | The game's small font. The usual choice for body text and labels. |
| `Tiny` | The game's tiny font. |
| `SpriteText` | The game's bitmap title font, the one vanilla uses for menu headers. Its natural size is **large**: a dozen characters at `TextScale: 1` is around 300 pixels wide. |

`TextScale: 1` means each font's own natural size, so switching `FontType` changes the size. On elements that have both a sprite and text (`Banner`, `Button` and a text-bearing `Image`) `TextScale` sizes the text and `Scale` sizes the sprite, independently.

## Sizing modes

Used by [`Panel`](panel.md), [`Banner`](banner.md), [`Button`](button.md) and [`Divider`](divider.md) to decide how wide they are.

| Value | Behaviour |
| --- | --- |
| `Fill` | Take the full width available. |
| `ShrinkToFit` | Be exactly as wide as the contents need. The element is then placed by its `Alignment`. |
| `Fixed` | Be exactly `Width` wide. Requires `Width`. |

In every mode the result is clamped to the space available, so an element can never be wider than its container.

## Colors

Colour fields accept any of:

| Form | Example |
| --- | --- |
| A colour name | `"SkyBlue"` |
| RGB hex | `"#8B4513"` |
| RGBA hex | `"#8B4513FF"` |
| 8-bit RGB | `"34 139 34"` |
| 8-bit RGBA | `"34 139 34 255"` |

Values are space-separated, not comma-separated. An unparsable colour logs a warning and falls back to the default.

## Rectangles and points

Rectangles and points are objects:

```json
"TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
"Position": { "X": -64, "Y": 192 }
```
