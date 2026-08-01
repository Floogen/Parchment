# Variables

A variable is a named value a book sets and reads back, and unlike a [session flag](../concepts/actions.md#session-flags) it **survives the book being put down**. That makes a book able to hold a setting, a bookmark or a choice the reader made three chapters ago.

Every variable has to be declared on the book before anything touches it. A declaration gives it a starting value, a type and a lifetime, and it means an action naming a variable the book never declared fails with a message rather than quietly storing a typo into a save.

```json title="content.json"
{
  "Id": "{{ModId}}_Almanac",
  "Variables": [
    { "Id": "showSpoilers", "Type": "Boolean", "Default": "false" },
    { "Id": "units", "Type": "Text", "Default": "metric", "AllowedValues": [ "metric", "imperial" ], "Scope": "Global" }
  ],
  "Pages": [ ... ]
}
```

## Variable fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` <span class="req">required</span> | `string` | — | The name actions and queries address this variable by. Unique within the book. |
| `Type` <span class="opt">optional</span> | `Boolean` \| `Number` \| `Text` | `Boolean` | What the variable holds. See [Types](#types). |
| `Default` <span class="opt">optional</span> | `string` | *see below* | The value before anything sets it, and what `ClearVariable` returns it to. Omitted, it's `false`, `0` or empty text, whichever suits the `Type`. |
| `AllowedValues` <span class="opt">optional</span> | list of `string` | *anything* | The only values `SetVariable` accepts, compared ignoring case. |
| `Scope` <span class="opt">optional</span> | `Save` \| `Global` | `Save` | Whether the value belongs to the save file or to the whole installation. See [Scope](#scope). |

!!! warning "`AllowedValues` without a `Default`"
    The starting value has to be an allowed one. A `Text` variable with `AllowedValues` and no `Default` starts as empty text, which isn't in the list, and the book is rejected. Give it a `Default` whenever you give it `AllowedValues`.

---

## Types

`Type` decides what `SetVariable` accepts and how [`HasVariable`](../concepts/conditions.md#variables) compares.

| Type | Accepts | Compared as |
| --- | --- | --- |
| `Boolean` | `true` or `false` | Text |
| `Number` | Anything that parses as a number | A number, so `9` and `9.0` are the same value |
| `Text` | Anything | Text, ignoring case |

Only a `Boolean` can be toggled. `ToggleVariable` on anything else fails with a message naming the type it found.

## Scope

`Save` is the default and the one you want for most things. The value rides on the player, so the game saves it with everything else and each save file keeps its own. In multiplayer each player keeps their own value.

`Global` stores it once per installation, shared by every save file. That's for a setting a reader shouldn't have to set again on a new farm, such as a units preference or whether to show spoilers.

!!! note "A `Save` variable needs a save loaded"
    Setting one before a save is loaded fails, since there's no player to store it on. A book meant to be read from the title screen needs `Global` variables.

## Setting and reading

| Action | Arguments | What it does |
| --- | --- | --- |
| `PeacefulEnd.Parchment_SetVariable` | `<bookId> <variableId> <value>` | Set a variable. Everything past the variable ID counts as the value, so a phrase needs no quoting. |
| `PeacefulEnd.Parchment_ClearVariable` | `<bookId> <variableId>...` | Return one or more variables to their `Default`. |
| `PeacefulEnd.Parchment_ToggleVariable` | `<bookId> <variableId>...` | Flip one or more `Boolean` variables. |

Every one names the book that declares the variable, which is what lets them run from `Data/TriggerActions` rather than only from a button inside the book.

`ClearVariable` and `ToggleVariable` take a list, and they're **all or nothing**. Every name in the list is resolved and checked before any of them is written, so `PeacefulEnd.Parchment_ToggleVariable {{ModId}}_Almanac showSpoilers units` fails on `units` being `Text` and leaves `showSpoilers` alone rather than flipping it first.

Read one back with the [`%Variable:id%`](../concepts/actions.md#tokens) token in an element's text, or with the [`PeacefulEnd.Parchment_HasVariable`](../concepts/conditions.md#variables) query in a `Condition`. The token takes the open book as read, since it only appears inside one. The query names the book, so it works anywhere a game state query does.

A checkbox is the two put together, one element per state:

```json title="content.json"
{
  "Type": "Panel",
  "Children": [
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/checkbox",
      "TextureSourceRectangle": "0, 0, 9, 9",
      "Action": "PeacefulEnd.Parchment_ToggleVariable {{ModId}}_Almanac showSpoilers",
      "Condition": "!PeacefulEnd.Parchment_HasVariable {{ModId}}_Almanac showSpoilers true"
    },
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/checkbox",
      "TextureSourceRectangle": "9, 0, 9, 9",
      "Action": "PeacefulEnd.Parchment_ToggleVariable {{ModId}}_Almanac showSpoilers",
      "Condition": "PeacefulEnd.Parchment_HasVariable {{ModId}}_Almanac showSpoilers true"
    }
  ]
}
```

A cycling setting is one `Button` per value, each conditioned on the value before it:

```json title="content.json"
{
  "Type": "Button",
  "Text": "Units: %Variable:units%",
  "Action": "PeacefulEnd.Parchment_SetVariable {{ModId}}_Almanac units imperial",
  "Condition": "PeacefulEnd.Parchment_HasVariable {{ModId}}_Almanac units metric"
}
```

## Variables belong to a book

A variable is stored against the `Id` of the book that declares it, so two books declaring `showSpoilers` keep separate values. That's why every action and query names the book: the variable `Id` on its own is only unique within its book, and prefixing happens in the store rather than in what you write.

Naming the book also means nothing has to be open. A `DayStarted` trigger can set a variable, and an event or a dialogue line can ask about one, the same way the [reading history](../concepts/conditions.md#reading-history) queries work.

That last point is the limit worth knowing before you plan around it: a variable governs **what the book shows**. It can't change a map edit, a shop's stock or an NPC's schedule, because Content Patcher never sees it. For settings that have to reach the rest of a content pack, use Content Patcher's own [`ConfigSchema`](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/config.md) instead.

## Reading one from Content Patcher

Parchment registers a `{{PeacefulEnd.Parchment/Variables}}` token holding every declared variable as a `bookId/variableId=value` entry, so a pack's other patches can respond to what a reader chose:

```json title="content.json"
{
  "Action": "EditData",
  "Target": "Data/Shops",
  "When": { "PeacefulEnd.Parchment/Variables": "{{ModId}}_Almanac/hardMode=true" },
  "Entries": { ... }
}
```

To use it, list Parchment as a dependency in your pack's `manifest.json`, the same as any mod-provided token.

**Interpolating a value.** The token holds the whole list, so dropping it straight into a patch body gives you every variable rather than one. Bridge it with a [dynamic token](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide/tokens.md#dynamic-tokens), one entry per value:

```json title="content.json"
"DynamicTokens": [
  { "Name": "Units", "Value": "metric", "When": { "PeacefulEnd.Parchment/Variables": "{{ModId}}_Almanac/units=metric" } },
  { "Name": "Units", "Value": "imperial", "When": { "PeacefulEnd.Parchment/Variables": "{{ModId}}_Almanac/units=imperial" } }
]
```

`{{Units}}` then works anywhere in the pack. This suits a variable with `AllowedValues`, since you need one entry per possible value, and doesn't suit a `Number` variable holding anything at all.

!!! warning "Content Patcher sees a change later than the book does"
    `%Variable:id%` and `HasVariable` see a new value the moment it's set. The token doesn't: Content Patcher reads it on its own context updates, which happen on day start, on the ten-minute clock and on warping. In single player an open book pauses that clock, so a setting toggled in a book reaches your patches once the book is closed and time moves again.

## Declaring one from C\#

A book [built in code](building-books.md) declares its variables through `AddVariable`, which returns a builder covering the same fields as the JSON above:

```cs
book.AddVariable("units").Type("Text").Default("metric").Scope("Global").AllowedValue("metric").AllowedValue("imperial");
```

A variable answers as soon as it's declared, so a book can read its own variables while it assembles itself, before `TryRegister` or `TryOpen` is reached. Declaring is still what makes it findable, so put `AddVariable` above whatever reads it.

Everything after that works exactly as it does for a book from a content pack. The variable is keyed by the book's `Id`, so a book built in code and a book in a content pack sharing an ID would share values, which is one more reason to prefix book IDs with your mod's unique ID.

!!! note "A `TryOpen` book keeps its variables to itself"
    Variables work on a book opened through `TryOpen` as well as on a registered one, and they persist the same way. What that book can't do is publish them to the [Content Patcher token](#reading-one-from-content-patcher), which only lists the variables of books in the books asset. A book that never registers isn't in it.

## Reading one from C\#

A SMAPI mod can read and write a book's variables through the [API](api.md), which is how a mod backs a book's settings page with its own config file.

```csharp
if (api.TryGetVariable("{{ModId}}_Almanac", "showSpoilers", out string value) is true)
{
    this.Config.ShowSpoilers = bool.Parse(value);
    this.Helper.WriteConfig(this.Config);
}
```

## Gotchas

**A variable isn't a flag.** A [session flag](../concepts/actions.md#session-flags) is dropped when the book closes and needs no declaration. Use a flag for "has this happened during this reading" and a variable for anything that should outlive it.

**Clearing is a reset, not a removal.** `ClearVariable` puts the `Default` back. A declared variable always holds something, so there's no unset state to return to.

**Global values are written within a second of changing.** Parchment checks once a second and writes only when something moved, on top of writing when the book closes, when the game saves and when you return to the title. A `Save` variable needs none of this, since it rides on the player and is committed with the next game save like any other progress.

**A `Save` variable belongs to a player.** In multiplayer each farmer holds their own, so a condition evaluated against another player reads theirs rather than yours. `Global` variables are shared by everyone on that installation.
