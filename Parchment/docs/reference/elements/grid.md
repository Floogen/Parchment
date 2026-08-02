# Grid

Arranges its children into cells of a fixed size, left to right and then top to bottom. Parchment's other containers stack vertically or place by coordinate, so this is the one that lays anything out **across** the page.

```json
{
  "Type": "Grid",
  "Columns": 6,
  "CellWidth": 20,
  "CellHeight": 20,
  "ColumnSpacing": 2,
  "RowSpacing": 2,
  "Children": [
    { "Type": "Image", "ItemId": "(O)145", "Scale": 3, "Alignment": "Center", "VerticalAlignment": "Center" },
    { "Type": "Image", "ItemId": "(O)147", "Scale": 3, "Alignment": "Center", "VerticalAlignment": "Center" }
  ]
}
```

## Grid fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Columns` <span class="req">required</span> | `int` | `1` | How many cells sit side by side before the next row starts. |
| `CellWidth` <span class="req">required</span> | `int` | — | A cell's width in unscaled sprite pixels × `Scale`. |
| `CellHeight` <span class="req">required</span> | `int` | — | A cell's height in unscaled sprite pixels × `Scale`. |
| `Rows` <span class="opt">optional</span> | `int?` | *as many as needed* | The most rows drawn. Children past the last cell are dropped. See [Capping the rows](#capping-the-rows). |
| `ColumnSpacing` <span class="opt">optional</span> | `int` | `0` | Space between columns, in unscaled sprite pixels × `Scale`. Not applied outside the outermost columns. |
| `RowSpacing` <span class="opt">optional</span> | `int` | `0` | Space between rows, in unscaled sprite pixels × `Scale`. |
| `Padding` <span class="opt">optional</span> | `int` | `0` | Space between the cells and the grid's border. |
| `Children` <span class="opt">optional</span> | list of [`elements`](index.md) | empty list | The elements filling the cells, in order. Ignored when `Source` is given. |
| `Source` <span class="opt">optional</span> | [`source`](#source) | — | Fills the cells from an item query instead of from `Children`, narrowed by what the reader types. See [Source](#source). |
| `Background` <span class="opt">optional</span> | list of [`elements`](index.md) | empty list | Elements drawn behind the cells, placed by `Position` within the grid's content area. They don't affect its size. |
| `Foreground` <span class="opt">optional</span> | list of [`elements`](index.md) | empty list | Elements drawn over the cells, placed by `Position`. |

A grid's size is **declared, not measured**. Its width is `Columns × CellWidth` plus the spacing between them, and its height follows from how many rows the children fill. That's why there's no `Sizing` or `Width` here: a cell is the same size as every other cell, so one child can never resize the rest.

---

## Cells are boxes, not moulds

A child isn't stretched to fill its cell. It's measured normally and then anchored within the cell by its own [`Alignment` and `VerticalAlignment`](../../concepts/layout.md#alignment-anchors-position-offsets), with `Position` nudging it from there.

The default is `Left` and `Top`, so children hug the top-left of their cells unless you say otherwise. For a grid of icons you almost always want both centred:

```json
{ "Type": "Image", "ItemId": "(O)145", "Scale": 3, "Alignment": "Center", "VerticalAlignment": "Center" }
```

A child larger than its cell isn't clipped, it overhangs into the neighbouring one. Size the cells to the largest thing going in them.

## Hidden cells close up

A child whose [`Condition`](../../concepts/conditions.md) fails takes no cell at all. The children after it move up to fill the gap, so the grid packs rather than leaving a hole, the same way a hidden element lets a stack close up.

That's what makes a grid filter cleanly: condition each cell and the remaining ones gather at the start rather than scattering.

!!! tip "When you want the hole"
    A fixed layout where one entry isn't unlocked yet wants the gap kept. Use a placeholder cell instead of hiding: an `Image` pointing at a blank or greyed sprite, swapped by the condition rather than removed by it.

## Capping the rows

`Rows` fixes the grid's height and drops anything past the last cell, the way a page drops content that runs past its bottom. A dropped child logs a trace message rather than failing.

```json
{
  "Type": "Grid",
  "Columns": 6,
  "Rows": 5,
  "CellWidth": 20,
  "CellHeight": 20
}
```

Without `Rows`, the grid is exactly as tall as its children need and grows a row at a time. A grid inside a page's `Elements` is stacked like anything else, so an uncapped grid that outgrows the page triggers the usual [overflow warning](../../concepts/layout.md#when-content-doesnt-fit).

## Source

`Source` fills the cells from an item query rather than from authored children, and narrows them by an [`Input`](input.md)'s text. It's how a search grid works, and the reason it works without reflowing anything is that **the number of cells never changes**. Only what each cell shows does.

```json
{
  "Type": "Grid",
  "Id": "fish",
  "Columns": 6,
  "Rows": 5,
  "CellWidth": 20,
  "CellHeight": 20,
  "Source": {
    "ItemQuery": "ALL_ITEMS (O)",
    "PerItemCondition": "ITEM_CATEGORY Target -4",
    "InputId": "search",
    "OrderBy": "Name",
    "Template": {
      "Type": "Image",
      "Scale": 3,
      "Alignment": "Center",
      "VerticalAlignment": "Center",
      "Action": "PeacefulEnd.Parchment_JumpToPageId %Item%"
    }
  }
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Template` <span class="req">required</span> | [`element`](index.md) | — | What each cell is built from. One template makes every cell. |
| `ItemQuery` <span class="opt">optional</span> | `string` | `ALL_ITEMS (O)` | The item query supplying the candidates. Resolved once and cached, so this is paid on load rather than per keystroke. |
| `PerItemCondition` <span class="opt">optional</span> | `string` | — | A game state query each candidate must pass, evaluated with that item in context. Category filters belong here. |
| `InputId` <span class="opt">optional</span> | `string` | — | The input whose text narrows the candidates. Without one the grid is an unfiltered list. |
| `OrderBy` <span class="opt">optional</span> | [`item property`](../../concepts/actions.md#item-properties) \| `None` | `None` | The property the candidates are sorted by before they reach the cells. `None` leaves them in the item query's own order. See [Ordering](#ordering). |
| `OrderDescending` <span class="opt">optional</span> | `bool` | `false` | Reverses the order, so the highest price or the last name comes first. |
| `Count` <span class="opt">optional</span> | `int?` | `Columns × Rows` | How many cells the candidates fill. Needed only when the grid has no `Rows`. |

### Ordering

`OrderBy` takes any of the [item properties](../../concepts/actions.md#item-properties) the `%Item.Something%` token reaches, so `"Name"`, `"Category"` and `"Price"` are all valid. It defaults to `"None"`, which leaves the candidates in whatever order the item query handed back, so a grid sorts only when it asks to.

```json title="content.json"
"Source": {
  "ItemQuery": "ALL_ITEMS (O)",
  "OrderBy": "Price",
  "OrderDescending": true,
  "Template": { "Type": "Image", "Scale": 3, "Alignment": "Center" }
}
```

Each property declares how it compares, so `Price` sorts as a number (9 before 1000) while `Name` and the rest sort as text, ignoring case. The [item properties table](../../concepts/actions.md#item-properties) says which is which.

Sorting happens once, when the item query is resolved, rather than on each keystroke. A grid ordering 1,000 items pays for it on load and the filter then walks an already-sorted list.

!!! note "Items that can't answer go last"
    An item with no category, or a price that isn't a number, sorts to the end. `OrderDescending` doesn't move them, so reversing the order never brings a wall of blank-looking cells to the front.

### How a cell gets its item

The item is applied to any `Image` inside the template that has **neither** an `ItemId` nor a `TexturePath` of its own. That's the hole the result fills. An `Image` with its own texture is authored art, such as a slot frame behind the icon, and is left alone.

So a cell can be more than an icon. Make the template a `Panel` and everything inside it comes along:

```json
"Template": {
  "Type": "Panel",
  "Children": [ { "Type": "Image", "Scale": 3, "Alignment": "Center" } ],
  "Background": [ { "Type": "Image", "TexturePath": "{{ModId}}/slot", "Scale": 4 } ]
}
```

`DisplayName` and `Description` are filled from the item wherever the template leaves them out, so tooltips work with no extra authoring. Set them to `""` in the template to suppress that.

`%Item%` in an action resolves to the cell's qualified item ID, which is how one template's action reaches whichever result its cell landed on.

### What filtering does

Typing narrows the candidates by display name **and** by qualified item ID, ignoring case. Matches fill the cells from the first, and cells past the last match are emptied and hidden. An empty box matches everything.

!!! warning "You see the first `Count` matches, not all of them"
    A filter matching 112 items across 30 cells shows 30. That's the trade a fixed cell count buys: nothing reflows and the page count never changes.

Say so with [tokens](../../concepts/actions.md#tokens). They read the **grid's** own `Id`, the one alongside `Type` rather than anything inside `Source`, so a grid you want to report on needs one:

```json
{
  "Type": "Paragraph",
  "Text": "Showing %GridDisplayed:fish% of %GridMatched:fish% matches."
}
```

Inside the template, [`%Item.Name%`](../../concepts/actions.md#item-properties) and its siblings let a cell label itself with the item it landed on.

!!! note "`Children` is ignored"
    A grid with `Source` builds its cells from `Template` alone. Anything in `Children` is not drawn, rather than being appended after the results.

## Common fields

`Scale` on a `Grid` multiplies the cell size, the spacing and the padding together, so raising it enlarges the whole grid rather than only its frame.

--8<-- "element-common.md"

## Sprite fields

A `Grid` can carry a nine-sliced frame behind its cells exactly as a [`Panel`](panel.md) does, and it's optional in the same way. Leave `TexturePath` out and only the cells draw.

--8<-- "sprite.md"
