# Page number

The page's own number, filled in for you.

```json
{
  "Type": "PageNumber",
  "Alignment": "Center"
}
```

Stacked in `Elements` it sits wherever the content above it ends, which is rarely what you want for a folio. For a number pinned to the same spot on every page, put it in the page's [`Foreground`](../page.md#background-and-foreground) with a `Position`, remembering that placed elements ignore `Alignment`:

```json
{
  "Type": "PageNumber",
  "Position": { "X": 96, "Y": 296 }
}
```

The number counts from **1**, so the first page of the book shows `1`. By default it's the page's position in the book as a whole, matching how [`JumpToPage`](../../concepts/actions.md#parchments-actions) counts (only that action is 0-based).

Unlike every other text element, `PageNumber` takes no `Text`. Giving it one is ignored rather than treated as an error.

## Page number fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Scope` <span class="opt">optional</span> | `Book` \| `Chapter` | `Book` | What the number counts from. `Chapter` starts again at `1` on each [chapter's](../page.md#chapters) first page, rather than running through the book. No effect in a book without chapters, where the whole book counts as one. |
| `Format` <span class="opt">optional</span> | `string` | — | A wrapper around the number, where `{0}` is the number. `"Page {0}"` gives `Page 4`, `"- {0} -"` gives `- 4 -`. When omitted the number is drawn on its own. |

```json
{
  "Type": "PageNumber",
  "Format": "- {0} -",
  "Scope": "Chapter",
  "Alignment": "Center"
}
```

## Text fields

`FontType` defaults to **`Small`**, the game's small font.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `FontType` <span class="opt">optional</span> | [`font type`](index.md#font-types) | `Small` | Which font to draw the number with. |
| `TextColor` <span class="opt">optional</span> | [`color`](index.md#colors) | *the book's default* | The number's colour. |

## Common fields

`Scale` on a `PageNumber` is the **font** scale, since it has no sprite.

--8<-- "element-common.md"

## Gotchas

**It needs a page.** A `PageNumber` in a book's [`Underlay` or `Overlay`](../book.md) has no single page to name, since those span the whole book, so it draws nothing and logs a warning once. Put it in the page's `Elements`, `Background` or `Foreground` instead.

**Numbering follows the book, not the reader.** A page shows the same number however the reader arrived at it. Which page is number 1 depends on `Scope`, not on where they started reading.

**A repeated chapter ID restarts the count.** Chapters are contiguous runs of pages, so pages sharing a `ChapterId` in two separate runs are two chapters (Parchment already warns about this at load). With `Scope` set to `Chapter`, the second run counts from `1` again.

**A bad `Format` drops the element.** It's checked at load, so `"Page {1}"` (there's only ever a `{0}`) fails validation with the reason in the SMAPI log. For a literal brace, double it: `"{{0}}"` draws `{0}`. Content Patcher reads `{{` as the start of a token, so put a literal brace in a `Format` only via a CP token holding it.

**A skipped page still shifts the numbers.** A page dropped at load for failing validation isn't in the book, so every page after it shifts up by one. Check the SMAPI log if the numbers look off by one.
