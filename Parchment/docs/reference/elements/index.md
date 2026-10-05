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

Any element's `Text` can carry [tokens](../../concepts/actions.md#tokens), placeholders replaced with something the book knows as the element is laid out. That covers Parchment's own `%Token%` forms and the game's `[Token]` [tokenizable strings](../../concepts/actions.md#game-tokens). Part of it can also be colored with [inline color](#inline-color) markup or turned into a [link](#links) on a `Title`, `Heading`, `Paragraph` or `PageNumber`.

--8<-- "text-content.md"

## Sprite fields

Understood by [`Image`](image.md), [`Panel`](panel.md), [`Grid`](grid.md), [`Banner`](banner.md), [`Button`](button.md), [`Divider`](divider.md) and [`Input`](input.md).

--8<-- "sprite.md"

## Animation fields

Understood by every element type. On an [`Image`](image.md) a frame steps through a sprite sheet. Everywhere else it moves the element, times a [trigger action](../../concepts/actions.md), or both.

--8<-- "animation.md"

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

Color fields accept any of:

| Form | Example |
| --- | --- |
| A color name | `"SkyBlue"` |
| RGB hex | `"#8B4513"` |
| RGBA hex | `"#8B4513FF"` |
| 8-bit RGB | `"34 139 34"` |
| 8-bit RGBA | `"34 139 34 255"` |

Values are space-separated, not comma-separated. An unparsable color logs a warning and falls back to the default.

Alpha is optional and full strength when left off. Write the color you want at full strength and let the alpha fade it, the same way the game does elsewhere: `"255 0 0 128"` is a half-faded red rather than a brighter one. The channels are scaled by the alpha before anything is drawn, so a translucent color fades towards whatever is behind it instead of washing out towards white.

## Inline color

A text element can color part of its `Text` by wrapping it in `[color=...]` and `[/color]`:

```json
{
  "Type": "Paragraph",
  "Text": "You reeled in a [color=Gold]Legend[/color] at [color=#4A90D9]Mountain Lake[/color]."
}
```

The value takes any of the [color forms](#colors) above. Everything outside a tag keeps the element's `TextColor`.

- **Nesting.** Tags nest, so `[color=Red]a [color=Blue]b[/color] c[/color]` draws `c` red again.
- **Tokens.** Tokens work inside a run, game tokens included: `[color=Gold][ItemName (O)128][/color]`. The markup is read from the text as written before any token resolves, so a value a token brings in (such as something the player typed into an `Input`) is always drawn as it is and can't color anything.
- **Shadow.** A colored run keeps the element's `ShadowColor`. Left unset, the shadow follows the run's alpha the same way it follows `TextColor`'s.
- **Case.** Tags are case-insensitive, so `[Color=Red]` works too.
- **Mistakes.** A `[color]` that is never closed runs to the end of the text. A stray `[/color]` is dropped. A color that won't parse keeps the color around it. Each logs a warning.

Markup is read on every text element except `Input`, whatever `ParseTokenizableStrings` is set to.

!!! note "Where markup isn't drawn"
    `SpriteText` keeps its own color, so markup on an element drawn in it is ignored with a warning. A tooltip's `DisplayName` and `Description` draw in a single color, so any markup in them is taken out rather than shown.

## Links

A `Title`, `Heading`, `Paragraph` or `PageNumber` can turn part of its text into a link by wrapping it in `[link=id]` and `[/link]`. The `id` names an entry in the element's `Links`:

```json
{
  "Type": "Paragraph",
  "Text": "You reeled in a [link=legend]Legend[/link] at [color=Blue]Mountain Lake[/color].",
  "Links": {
    "legend": {
      "TextColor": "Gold",
      "HoverTextColor": "Orange",
      "DisplayName": "Legend",
      "Description": "Only bites in spring rain.",
      "Action": "PeacefulEnd.Parchment_JumpToPageId legendary-fish"
    }
  }
}
```

A link behaves like an element of its own: it has its own tooltip, runs its own actions and carries its own tags, all reached through the stretch of text it covers.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `TextColor` <span class="opt">optional</span> | [`color`](#colors) | *the color around it* | The linked text's color. |
| `HoverTextColor` <span class="opt">optional</span> | [`color`](#colors) | — | The linked text's color while the cursor is over it. Covers any `[color]` markup inside the link. |
| `DisplayName` <span class="opt">optional</span> | `string` | — | The bold title of the link's hover tooltip. Can carry [tokens](../../concepts/actions.md#tokens). |
| `Description` <span class="opt">optional</span> | `string` | — | The body of the link's hover tooltip. Can carry [tokens](../../concepts/actions.md#tokens). |
| `Action` <span class="opt">optional</span> | `string` | — | A [trigger action](../../concepts/actions.md) to run when the link is clicked. Shorthand for a single-entry `Actions` that runs first when both are given. |
| `Actions` <span class="opt">optional</span> | `string[]` | — | Trigger actions to run in order when the link is clicked. |
| `HoverAction` <span class="opt">optional</span> | `string` | — | A trigger action to run when the cursor moves onto the link. Shorthand for a single-entry `HoverActions` that runs first when both are given. |
| `HoverActions` <span class="opt">optional</span> | `string[]` | — | Trigger actions to run in order when the cursor moves onto the link. |
| `Tags` <span class="opt">optional</span> | `string[]` | — | [Tags](../tags.md) carried by the link, read the same way as an element's own. |

- **Each occurrence is its own link.** The same id used twice gives two links that are hovered, highlighted and reached by a controller separately.
- **Wrapping.** A link that runs onto a second line is reached through its text on both lines, never through the words between them.
- **Nesting.** `[color]` works inside and around a link. A link inside another link takes over the text it covers. Tags of the two kinds may overlap rather than nest, in which case each closing tag closes the innermost tag of its own kind.
- **Tokens and actions.** A link's tooltip and actions resolve tokens against the link. It follows the element's `ParseTokenizableStrings` and plays the element's `Sound` when clicked.
- **The rest of the text.** Text outside every link still shows the element's own tooltip and runs the element's own actions. So does a link that only sets `TextColor`, since it gives the cursor nothing to do.
- **Controller.** Each link is a stop of its own, placed on the first line it covers.
- **Mistakes.** A `[link]` naming an id that isn't in the element's `Links` is drawn as plain text with a warning. So is a `[link]` on any other element. Unclosed and stray tags behave as they do for [inline color](#inline-color).

!!! note "Links in `SpriteText`"
    A link drawn in `SpriteText` keeps its tooltip, actions and tags. Its `TextColor` and `HoverTextColor` are ignored, as `SpriteText` keeps its own color.

## Rectangles and points

Rectangles and points are objects:

```json
"TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
"Position": { "X": -64, "Y": 192 }
```
