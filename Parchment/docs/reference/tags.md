# Tags

`Tags` is a list of free-form markers you can put on any element. Parchment mostly leaves them alone. They exist so that **another mod** can look at whatever the cursor is over and recognise it.

```json
{
    "Type": "Image",
    "TexturePath": "Portraits/Abigail",
    "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 64, "Height": 64 },
    "Tags": [ "NpcId.Abigail" ]
}
```

In C#, call `WithTag` once per tag:

```cs
page.AddImage()
    .Texture("Portraits/Abigail")
    .TextureSource(0, 0, 64, 64)
    .WithTag("NpcId.Abigail");
```

## Tags and the cursor

An element is only worth tagging if the cursor can reach it, so **a tagged element becomes interactive**. That matters in a page's `Background` and `Foreground`, where decorative art normally lets the cursor through to whatever sits beneath it (see [Background and Foreground](page.md#background-and-foreground)).

`IgnoreCursor` still wins. An element with both is decorative, its tags are never read and nothing warns about it.

!!! note
    Tagging an element doesn't give it a tooltip. If you want one, set `DisplayName` and `Description` as usual.

## Tags Parchment recognises

Most tags mean nothing to Parchment. These two do:

| Prefix | Names | Written by |
| --- | --- | --- |
| `NpcId.` | An NPC by internal name, such as `NpcId.Abigail` | You |
| `ItemId.` | An item by [qualified item ID](https://stardewvalleywiki.com/Modding:Common_data_field_types#Item_ID), such as `ItemId.(O)145` | Parchment |

`ItemId.` is **derived**, not authored. Parchment adds it for you whenever the element is already showing an item, which covers an [`Image`](elements/image.md) with an `ItemId` and every cell of a [`Grid`](elements/grid.md#source) filled from an item query. There's no need to repeat it in `Tags`.

A tag using one of these prefixes with nothing behind it, such as `"NpcId."` or `"NpcId"`, fails at load rather than passing silently.

!!! warning
    A **misspelled** prefix can't be caught. `"NcpId.Abigail"` is a valid free-form tag as far as Parchment can tell, so it loads without complaint and simply does nothing. If a tag isn't having the effect you expect, check its spelling first.

## Containers

A tag carries down. When the cursor lands on an element with no tag of its own, Parchment looks at the container holding it, then that container's container, and so on. So a `Panel` tagged `NpcId.Abigail` covers the paragraph and the divider inside it without either needing its own tag.

The same applies to an item, which is what makes every part of a Grid cell answer with that cell's item.

## Lookup Anything

[Lookup Anything](https://www.nexusmods.com/stardewvalley/mods/541) lets players press a key to see everything the game knows about an item or a villager. It reads custom menus by looking for what the cursor is over, and Parchment tells it.

Nothing needs to be enabled. If a player has Lookup Anything installed, it works. If they don't, the tags sit there harmlessly.

**Items work with no tagging at all.** Anything already showing an item is already a lookup target:

```json
{
    "Type": "Image",
    "ItemId": "(O)145"
}
```

**NPCs need an `NpcId.` tag**, since a portrait is just a texture as far as Parchment is concerned:

```json
{
    "Type": "Panel",
    "Sizing": "ShrinkToFit",
    "Tags": [ "NpcId.Abigail" ],
    "Children": [
        {
            "Type": "Image",
            "TexturePath": "Portraits/Abigail",
            "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 64, "Height": 64 }
        },
        {
            "Type": "Paragraph",
            "Text": "She prefers amethyst above all else."
        }
    ]
}
```

The tag sits on the `Panel`, so the portrait and the line of text are both lookup targets.

!!! tip
    Use the NPC's **internal** name, the one the game's data files use, not their translated display name. `Abigail`, not `Abigaïl`.

An NPC who isn't loaded in the current save resolves to nothing and the lookup key does nothing. That's a save without that character rather than a mistake in your pack, so it doesn't warn.

## Asking about tags in a condition

Tags are readable from a [condition](../concepts/conditions.md#element-tags), which is what turns them from markers other mods read into something your own book can act on.

**Asking about the element you're writing on** needs the `%Tags%` token. A query receives a resolved string rather than the element the condition belongs to, so the token resolves the tags first and the query reads them:

```json
{
    "Type": "Panel",
    "Height": 40,
    "Tags": [ "Fish", "Ocean", "Crab Pot" ],
    "Condition": "PeacefulEnd.Parchment_TagsMatchInput %Tags% search"
}
```

That's a search box filtering a hand-written list, the same thing [page tags](page.md#tags) do for a table of contents, without every entry needing its own page. Swap `TagsMatchInput` for `TagsInclude` to test against fixed tags instead:

```json
"Condition": "PeacefulEnd.Parchment_TagsInclude %Tags% Ocean"
```

**Asking about a different element** names it by `Id`:

```json
"Condition": "PeacefulEnd.Parchment_ElementHasTag summaryPanel Ocean"
```

**Asking about whatever the cursor is over** needs no ID at all:

```json
"Condition": "PeacefulEnd.Parchment_IsHoveringTag Fish"
```

Since the derived `ItemId.` tag counts everywhere, that last one reaches an item without any tagging:

```json
"Condition": "PeacefulEnd.Parchment_IsHoveringTag ItemId.(O)145"
```

!!! note "Tags with spaces need quoting, tags with commas don't work in `%Tags%`"
    A game state query splits its arguments on spaces, so a tag containing one has to be quoted: `PeacefulEnd.Parchment_ElementHasTag entry "Crab Pot"`. Since that's inside a JSON string the quotes need escaping too, so the field reads `"Condition": "PeacefulEnd.Parchment_ElementHasTag entry \"Crab Pot\""`.

    The `%Tags%` token quotes itself, so a tag with a space passes through `TagsInclude` and `TagsMatchInput` untouched. A **comma** is the one character it can't carry, since that's what the token joins tags with. Use `ElementHasTag` or `IsHoveringTag` for a tag that has to contain one.

## Reading tags from your own mod

If you're writing a C# mod and want to read Parchment's tags yourself, `BookMenu` exposes what the cursor is over:

| Field | Type | Holds |
| --- | --- | --- |
| `HoveredItem` | `Item?` | The item the hovered element is about |
| `HoveredNpc` | `NPC?` | The NPC the hovered element is about |
| `HoveredTags` | `IReadOnlyList<string>` | Every tag on the hovered element, including the derived `ItemId.` one |

All three are empty or null when nothing is hovered, and are dropped when the book closes.

!!! danger "These aren't part of the public API"
    `BookMenu` isn't reached through [Parchment's API](api.md), so read these by reflection rather than by referencing Parchment directly. Doing it that way also means your mod keeps working when Parchment isn't installed.
