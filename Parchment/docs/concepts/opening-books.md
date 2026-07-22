# Opening a book

## From an item

Any object carrying the `PeacefulEnd.Parchment/CustomFields/BookId` custom field opens the named book when the player uses it. This is covered end to end in [Your first book](../getting-started/first-book.md#4-add-the-item-that-opens-it).

## From a tile

`PeacefulEnd.Parchment_OpenBook` is a map **tile action**: the book opens when the player faces the tile and clicks it. Useful for a map-based book with no item required.

Set the `Action` property on a `Buildings`-layer tile to the action followed by the book's `Id`:

```json
{
  "Action": "EditMap",
  "Target": "Maps/Town",
  "MapTiles": [
    {
      "Position": { "X": 43, "Y": 71 },
      "Layer": "Buildings",
      "SetProperties": {
        "Action": "PeacefulEnd.Parchment_OpenBook {{ModId}}_CampingGuide"
      }
    }
  ]
}
```

| Property | Value | What it does |
| --- | --- | --- |
| `Action` | `PeacefulEnd.Parchment_OpenBook <bookId>` | Opens `<bookId>` when the player checks the tile. |

`<bookId>` is the book's `Id` as registered in `Data/PeacefulEnd.Parchment/Books` (the same string `parchment_open` uses).


See the wiki for the map side of this (setting tile properties in Tiled or patching a vanilla map):

> **[Modding: Maps](https://stardewvalleywiki.com/Modding:Maps)**

## From the console

For testing, `parchment_open <bookId> [page] [chapter]` opens a book straight away with no item or tile. See [Test it](../getting-started/first-book.md#5-test-it).

## From another mod

A SMAPI mod can open a book in code through Parchment's [C# API](../reference/api.md).
