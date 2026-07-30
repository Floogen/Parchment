# Trigger actions

Any element can carry an `Action`, something the game does when the element is clicked. An element with an `Action` is interactive. One without it isn't.

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/button",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Text": "Next",
  "Action": "PeacefulEnd.Parchment_NextPage"
}
```

!!! tip "Buttons aren't special"
    `Action` lives on every element type. [`Button`](../reference/elements/button.md) is just the shorthand for a framed label. Put an `Action` on an [`Image`](../reference/elements/image.md) and you have a bookmark or a tab.

`Sound` controls the click cue, defaulting to `bigSelect`. Set it to `null` for a silent one.

## Running more than one action

`Actions` takes a list, run top to bottom on a single click:

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/button",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Text": "Claim reward",
  "Actions": [
    "AddMoney 500",
    "AddMail Current {{ModId}}_RewardClaimed All",
    "PeacefulEnd.Parchment_NextPage"
  ]
}
```

`Action` and `Actions` are combined, not exclusive. An element can carry both, in which case `Action` runs first and the list follows. `Action` stays the shorthand for the common case of a single action, and there's no need to rewrite existing books as a list.

`HoverAction` and `HoverActions` work the same way, with the list running in order on cursor entry.

`Sound` plays once on the click, not once per action.

!!! warning "Order matters at the end"
    An action that fails doesn't stop the ones after it, and neither does one that navigates. `PeacefulEnd.Parchment_CloseBook` followed by `PeacefulEnd.Parchment_NextPage` closes the book and then logs a warning that no book menu is open. Put navigation and `CloseBook` last.

## Writing actions

Actions are the game's own trigger actions, shared with `Data/TriggerActions` and dialogue commands. That means a button in your book can do anything the game can do, not just turn pages.

The full syntax and the list of vanilla actions are on the wiki:

> **[Modding: Trigger actions](https://stardewvalleywiki.com/Modding:Trigger_actions)**

The parts to know:

| Form | Meaning |
| --- | --- |
| `AddMoney 500` | An action and its arguments, space-delimited. |
| `AddFriendshipPoints "Mister Qi" 10` | Quote an argument containing spaces. |
| `If SEASON Winter ## AddMoney 500` | Run the action only if the [query](conditions.md) passes. |

So a button can give a reward, start a quest, play a sound or set a mail flag. Vanilla's whole action list is available.

## Parchment's actions

These only work while a book is open. Elsewhere they fail with a message in the SMAPI log rather than doing something strange.

| Action | Arguments | What it does |
| --- | --- | --- |
| `PeacefulEnd.Parchment_NextPage` | — | Turn forward one spread. |
| `PeacefulEnd.Parchment_PreviousPage` | — | Turn back one spread. |
| `PeacefulEnd.Parchment_FirstPage` | — | Jump to the **current chapter's** first page. |
| `PeacefulEnd.Parchment_LastPage` | — | Jump to the **current chapter's** last page. |
| `PeacefulEnd.Parchment_JumpToPage` | `<pageIndex>` | Jump to a page by index, crossing chapters if needed. Indexes start at 0. |
| `PeacefulEnd.Parchment_JumpToChapter` | `<chapterId>` | Jump to a chapter's first page. |
| `PeacefulEnd.Parchment_GoToStart` | — | Jump to the book's very first page, whatever chapter you're in. |
| `PeacefulEnd.Parchment_ViewCover` | — | Shut the book to its cover without leaving the menu. Works on any book, whatever its `ExitToCover` is set to. Fails when the book is already shut. See [Cover view](../reference/book.md#cover-view). |
| `PeacefulEnd.Parchment_CloseBook` | — | Close the book. |

### Scope

The distinction matters once you're using [chapters](../reference/page.md#chapters):

| Scope | Actions |
| --- | --- |
| **Chapter**: stays where you are | `NextPage`, `PreviousPage`, `FirstPage`, `LastPage` |
| **Book**: can cross chapters | `JumpToPage`, `JumpToChapter`, `GoToStart` |

A chapter is navigation-isolated: page turning can't leave it, and neither can `NextPage`. The book-scoped actions are the only way out, which is why a chapter needs at least one of them somewhere in it.

`FirstPage` therefore means "back to the top of what I'm reading", and `GoToStart` means "back to the front of the book", usually your table of contents.

!!! tip "Name your way home"
    `JumpToChapter contents` says what it means and survives you reordering pages. `GoToStart` only works while your contents page happens to be first.

## Combining with conditions

An action and a [`Condition`](conditions.md) on the same element gives you navigation that appears when it's useful:

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/button",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Text": "Back to contents",
  "Action": "PeacefulEnd.Parchment_JumpToChapter contents",
  "Condition": "!PeacefulEnd.Parchment_CurrentPageId contents-index"
}
```

Conditions are re-checked right after an action runs, so a button that changes the game state can make a neighbouring element appear on the same click.

## Running actions without a click

A page can run actions the moment it becomes visible, through [`OnView`](../reference/page.md#on-view):

```json
{
  "Id": "shrine",
  "ChapterId": "rites",
  "OnView": [
    { 
      "Condition": "SEASON Winter", 
      "Actions": [ "AddMail Current PeacefulEnd.Parchment_ExampleMailIdTest All" ]
    }
  ]
}
```

The distinction to hold onto: an element's `Action` runs when the reader chooses it, while an `OnView` action runs whether they intended to or not. Use it for things that follow from having read the page (such as setting mail flags).

Its `Condition` also behaves differently from every other one in Parchment, in that it is checked once per view. [On view](../reference/page.md#on-view) covers that and the spread ordering rules.


A [`HoverAction`](../reference/elements/index.md) runs when the cursor moves onto an element, once per entry rather than continuously. Moving away and back runs it again, and `Sound` doesn't apply. `HoverActions` takes a list for more than one:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/shrine",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 32, "Height": 32 },
  "HoverActions": [
    "AddMail Current {{ModId}}_SawTheShrine All",
    "PlaySound secret1"
  ]
}
```

!!! warning "Hovering"
    A reader crosses elements just by moving the mouse, so they may run a `HoverAction` without meaning to. Keep them to things that are harmless to repeat, such as setting a flag.

An element's `Condition` gates hovering too: a hidden element can't be hovered, so a hover action whose own effect fails the condition removes itself.

## Gotchas

**Nothing validates the action name up front.** A typo'd action fails when the player clicks it, with a warning in the SMAPI log naming the string you wrote. Test your buttons.

**An action that can't run still plays its sound and looks clickable.** `PreviousPage` on a chapter's first page does nothing. There's no way to grey a button out. Use a `Condition` to hide it instead.

**A list runs to the end regardless.** There's no per-action condition inside `Actions` or `HoverActions`, and no early exit on failure. Wrap an individual entry in `If <query> ## <action>` when only part of the list should apply.

**An empty entry drops the element.** `Actions` or `HoverActions` containing `""` fails validation and the element is skipped with a warning, matching how a page's [`OnView`](../reference/page.md#on-view) treats one.

**A long `HoverActions` list is easy to trip over repeatedly.** A reader crosses elements just by moving the mouse, and every entry runs each time. Keep the whole list harmless to repeat, not just its first action.
