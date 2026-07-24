# Book indicators

A book indicator is an icon floating above a map tile while a [game state query](conditions.md) passes. It points at something, it doesn't do anything: clicking an indicator has no effect. Pair it with [a tile action](opening-books.md#from-a-tile) when you want the tile to open a book as well.

## Setting an indicator

`PeacefulEnd.Parchment_BookIndicator` is a map **tile property**. Parchment reads the `Buildings` layer whenever the player enters a location and spawns an indicator on every tile carrying the property whose query passes.

```json
{
    "Action": "EditMap",
    "Target": "Maps/ArchaeologyHouse",
    "MapTiles": [
    {
        "Layer": "Buildings",
        "Position": {
        "X": 17,
        "Y": 11
        },
        "SetProperties": {
        "PeacefulEnd.Parchment_BookIndicator": "4.5 -4.5 \"!PeacefulEnd.Parchment_HasSeenChapterId PeacefulEnd.Campgrounds.Parchment_CampingGuide contents\""
        }
    }
    ]
}
```
The above example shows the indicator until the player has opened the Camping Guide from Campgrounds.


## Details
| Property | Value | What it does |
| --- | --- | --- |
| `PeacefulEnd.Parchment_BookIndicator` | `<offsetX> <offsetY> "<query>"` | Floats an indicator above the tile while `<query>` passes. |

The value has three parts, all of them required:

| Part | Meaning |
| --- | --- |
| `<offsetX>` | Horizontal offset in pixels. Decimals are valid. |
| `<offsetY>` | Vertical offset in pixels. Negative moves the indicator up. |
| `<query>` | A [game state query](conditions.md), quoted. |

A value Parchment can't read logs a warning naming the tile and is skipped, leaving the rest of the map's indicators alone.

See the wiki for the map side of this (setting tile properties in Tiled or patching a vanilla map):

> **[Modding: Maps](https://stardewvalleywiki.com/Modding:Maps)**

## Positioning

Indicators are drawn on top of the map, so one positioned over a tall building sits in front of it rather than behind.

## The query

The query is a [game state query](conditions.md), the same language `Condition` uses inside a book. If you want an indicator with no real condition, `TRUE` works.

Quoting is optional, since Parchment reads everything after the offset as the query either way. Keep the quotes for readability, remembering JSON needs them escaped as `\"`.

Parchment re-checks every indicator in the current location twice a second. A query that stops passing removes the indicator.

!!! tip "Indicators appear on entry"
    A query that *starts* passing is picked up the next time the player enters the location, not while they're standing there.

## Notes

**Only the `Buildings` layer is read.** An indicator on `Back` or `Front` is ignored, and no warning is logged, so one that never appears anywhere is worth checking against the layer first.

**The tile has to exist.** Content Patcher errors rather than creating an empty tile, so pair `SetProperties` with `SetIndex` when the tile isn't already drawn on `Buildings`.

**A missing query is a warning, not a default.** `"0 0"` on its own is skipped with a message in the SMAPI log. Use `TRUE` for an unconditional indicator.

**Indicators are decoration.** They aren't clickable and they don't carry a book ID. The tile needs its own `Action` property to do anything when the player checks it, and the two are set independently: an indicator can point at a tile that opens nothing.
