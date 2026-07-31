# Variables

A variable is a named value a book sets and reads back, and unlike a [session flag](../concepts/actions.md#session-flags) it **survives the book being closed**. That makes a book able to hold a setting, a bookmark or a choice the reader made three chapters ago.

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

## Reading one from C#

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

**Global values are written when the book closes.** They're also written when the game saves and when you return to the title, so a crash mid-reading is the only way to lose one.

**A `Save` variable belongs to a player.** In multiplayer each farmer holds their own, so a condition evaluated against another player reads theirs rather than yours. `Global` variables are shared by everyone on that installation.
