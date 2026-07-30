# Your first book

Adding a book takes four steps: write it, load its art, register it and give the player something that opens it.

Everything here is Content Patcher, no C# needed.

## Before you start

Create a Content Patcher content pack in the usual way, with a `manifest.json` declaring Parchment as a dependency:

```json title="manifest.json"
{
  "Name": "Your Camping Guide",
  "Author": "you",
  "Version": "1.0.0",
  "Description": "A readable camping guide.",
  "UniqueID": "you.CampingGuide",
  "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher"
  },
  "Dependencies": [
    {
      "UniqueID": "PeacefulEnd.Parchment.Core",
      "IsRequired": true
    }
  ]
}
```

The dependency isn't optional bookkeeping. Parchment is what creates the books asset, and the dependency is what guarantees it exists before your patch tries to edit it.

## 1. Write the book

A book is a [`BookData`](../reference/book.md) object. Start small (one page, one heading) and grow it once you can see it in-game.

```json title="assets/camping-guide.json"
{
  "Format": "1.0.0",
  "Id": "{{ModId}}_CampingGuide",
  "Pages": [
    {
      "Id": "cover",
      "Elements": [
        { "Type": "Title", "Text": "Camping Guide", "Alignment": "Center" },
        { "Type": "Heading", "Text": "by Linus", "Alignment": "Center" }
      ]
    },
    {
      "Id": "tents",
      "Elements": [
        { "Type": "Heading", "Text": "Pitching a tent" },
        { "Type": "Divider", "Sizing": "Fill" },
        { "Type": "Paragraph", "Text": "Find level ground, away from the river." }
      ]
    }
  ]
}
```

`{{ModId}}` is a Content Patcher token that becomes your mod's unique ID. Using it for the book's `Id` means your book can't collide with anyone else's.

A book gets long quickly, so keep it out of your `content.json`. Two ways:

```json
// Pull one book's JSON into an entry
"Entries": {
  "{{ModId}}_CampingGuide": "{{FromFile: assets/camping-guide.json}}"
}
```

```json
// Or move a whole set of patches into another file
{
  "Action": "Include",
  "FromFile": "data/books.json"
}
```

`Include` is the tidier option once a book has its own item, its own art and its own data patch. They can all live together in one file.

## 2. Loading your art { #loading-your-art }

Every `TexturePath` in a book is a **game asset name**, not a file in your content pack. So any art you want to use has to be loaded into the game's content first.

```json
{
  "Action": "Load",
  "Target": "Mods/{{ModId}}/BookIcon, Mods/{{ModId}}/Bookmark, Mods/{{ModId}}/PanelFrame",
  "FromFile": "assets/{{TargetWithoutPath}}.png"
}
```

One patch, three assets: `Target` takes a comma-delimited list, and `{{TargetWithoutPath}}` expands to each target's last segment, so `Mods/{{ModId}}/Bookmark` loads `assets/Bookmark.png`. Name your files after your assets and adding art becomes a one-word change.

Then refer to them by asset name:

```json
{
  "Type": "Image",
  "TexturePath": "Mods/{{ModId}}/PanelFrame",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 24, "Height": 24 },
  "Scale": 4,
  "Alignment": "Center"
}
```

Two consequences to know:

- **Vanilla art works too.** `"TexturePath": "LooseSprites/Cursors"` needs no `Load` patch at all. It's already a game asset.
- **Your art is patchable.** Because it's a real game asset, another mod can retexture your book, and Parchment picks up the change while the book is open.

You can skip this step entirely if your book only uses text and item icons.

## 3. Register the book

Books live in the `Data/PeacefulEnd.Parchment/Books` asset, which Parchment provides. Add yours with `EditData`, keyed by the book's `Id`:

```json title="content.json"
{
  "Action": "EditData",
  "Target": "Data/PeacefulEnd.Parchment/Books",
  "Entries": {
    "{{ModId}}_CampingGuide": {
      "Format": "1.0.0",
      "Id": "{{ModId}}_CampingGuide",
      "Pages": [ ... ]
    }
  }
}
```

!!! warning "The key and the `Id` must match"
    The asset is a list, and Content Patcher uses each entry's `Id` field as its key. Writing a different key from the `Id` will confuse both Content Patcher and Parchment, so keep them identical. `{{ModId}}` in both is the easy way.

Any number of mods can add books to this asset without stepping on each other, which is why it's `EditData` rather than `Load`. Don't use `Load` here. It would wipe every other mod's books.

## 4. Add the item that opens it

A book needs something the player can use. Define an object in `Data/Objects` and point it at your book with a custom field:

```json title="content.json"
{
  "Action": "EditData",
  "Target": "Data/Objects",
  "Entries": {
    "{{ModId}}_CampingGuide": {
      "Name": "{{ModId}}_CampingGuide",
      "DisplayName": "Camping Guide",
      "Description": "Linus wrote this. It smells faintly of woodsmoke.",
      "Type": "Basic",
      "Category": 0,
      "Price": 100,

      "Texture": "Mods/{{ModId}}/BookIcon",
      "SpriteIndex": 0,

      "Edibility": -300,

      "CanBeGivenAsGift": false,
      "CanBeTrashed": true,
      "ExcludeFromRandomSale": true,
      "ExcludeFromShippingCollection": true,

      "CustomFields": {
        "PeacefulEnd.Parchment/CustomFields/BookId": "{{ModId}}_CampingGuide"
      }
    }
  }
}
```

The custom field is the whole mechanism: **any object carrying `PeacefulEnd.Parchment/CustomFields/BookId` opens the named book when the player uses it.** Nothing else about the object matters to Parchment. It can be any item you like, and reading it doesn't consume it.

The rest of the fields are what make it behave like a book rather than a vegetable. `Edibility: -300` marks it inedible. The `Exclude` flags keep it out of random shop stock and the shipping collection. `CanBeGivenAsGift: false` stops villagers reacting to it.

!!! tip "The book's name comes from the item"
    A `BookData` has no title or description of its own. The player sees the item's `DisplayName` and `Description`. That's deliberate: it means both are localisable with the usual `[LocalizedText ...]` tokens, and you don't write them twice.

The custom field is just a string on an object, so you can also point *someone else's* item at your book without redefining it:

```json
{
  "Action": "EditData",
  "Target": "Data/Objects",
  "TargetField": [ "SomeOtherMod_Journal", "CustomFields" ],
  "Entries": {
    "PeacefulEnd.Parchment/CustomFields/BookId": "{{ModId}}_CampingGuide"
  }
}
```

You'll also want a way for the player to *get* the item: a shop entry, a mail attachment, a machine output. That's ordinary item modding and Parchment doesn't care how you do it.

!!! tip "An item isn't the only way!"
    A book can also open from a tile or from another mod. See [Opening a book](../concepts/opening-books.md).

## 5. Test it

You don't need the item to test the book. With the mod loaded:

```
parchment_open you.CampingGuide_CampingGuide
```

Use your actual mod ID rather than the token. The console doesn't know about Content Patcher tokens. The book opens immediately, so you can iterate on the JSON without crafting anything.

There's also a debug view that outlines every element's bounds, which is the fastest way to understand why something is in the wrong place:

```
parchment_debug
```

## Where to go next

- [The example pack](example-pack.md): example books to read, copy and edit.
- [Units and scale](../concepts/units.md): short, and it'll save you an afternoon.
- [Elements](../reference/elements/index.md): everything you can put on a page.
- [Conditions](../concepts/conditions.md) and [trigger actions](../concepts/actions.md): making pages react and buttons work.
