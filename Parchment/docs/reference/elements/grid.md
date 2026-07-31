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
| `Children` <span class="opt">optional</span> | list of [`elements`](index.md) | empty list | The elements filling the cells, in order. |
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

## Common fields

`Scale` on a `Grid` multiplies the cell size, the spacing and the padding together, so raising it enlarges the whole grid rather than only its frame.

--8<-- "element-common.md"

## Sprite fields

A `Grid` can carry a nine-sliced frame behind its cells exactly as a [`Panel`](panel.md) does, and it's optional in the same way. Leave `TexturePath` out and only the cells draw.

--8<-- "sprite.md"
