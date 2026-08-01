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

These only work while a book is open, apart from `MarkSeen`, `ClearSeen` and the variable actions, which read and write saved state and so work anywhere. Elsewhere the rest fail with a message in the SMAPI log rather than doing something strange.

| Action | Arguments | What it does |
| --- | --- | --- |
| `PeacefulEnd.Parchment_NextPage` | `[skipAnimation]` | Turn forward one spread. |
| `PeacefulEnd.Parchment_PreviousPage` | `[skipAnimation]` | Turn back one spread. |
| `PeacefulEnd.Parchment_GoBack` | `[skipAnimation]` | Return to wherever the reader came from, crossing chapters if that's where they were. Calling it again goes back a further step. See [Going back](#going-back). |
| `PeacefulEnd.Parchment_FirstPage` | `[skipAnimation]` | Jump to the **current chapter's** first page. |
| `PeacefulEnd.Parchment_LastPage` | `[skipAnimation]` | Jump to the **current chapter's** last page. |
| `PeacefulEnd.Parchment_JumpToPage` | `<pageIndex> [skipAnimation]` | Jump to a page by index, crossing chapters if needed. Indexes start at 0. |
| `PeacefulEnd.Parchment_JumpToChapter` | `<chapterId> [skipAnimation]` | Jump to a chapter's first page. |
| `PeacefulEnd.Parchment_JumpToChapterPage` | `<chapterId> <pageInChapter> [skipAnimation]` | Jump to a page counted from the start of a chapter. Indexes start at 0, so `0` is the same as `JumpToChapter`. |
| `PeacefulEnd.Parchment_JumpToPageId` | `<pageId> [chapterId] [skipAnimation]` | Jump to a page by its `Id`, crossing chapters if needed. Pass a `chapterId` to only search that chapter. |
| `PeacefulEnd.Parchment_GoToStart` | `[skipAnimation]` | Jump to the book's very first page, whatever chapter you're in. |
| `PeacefulEnd.Parchment_ViewCover` | — | Shut the book to its cover without leaving the menu. Works on any book, whatever its `ExitToCover` is set to. Fails when the book is already shut. See [Cover view](../reference/book.md#cover-view). |
| `PeacefulEnd.Parchment_CloseBook` | — | Close the book. |
| `PeacefulEnd.Parchment_SetInput` | `<inputId> <text>` | Replace an [`Input`](../reference/elements/input.md) element's text. Everything past the ID counts as the text, so a phrase needs no quoting. |
| `PeacefulEnd.Parchment_ClearInput` | `<inputId>` | Empty an `Input`. The same as `SetInput` with no text, spelled so a clear button reads as one. |
| `PeacefulEnd.Parchment_SetFlag` | `<flag>...` | Set one or more [session flags](#session-flags). |
| `PeacefulEnd.Parchment_ClearFlag` | `<flag>...` | Clear one or more session flags. |
| `PeacefulEnd.Parchment_MarkSeen` | `<bookId> <chapterId> [pageId]` | Mark a chapter as read, and a page too when one is given. See [Reading history](conditions.md#reading-history). |
| `PeacefulEnd.Parchment_ClearSeen` | `[bookId]` | Forget what the player has read, all of it or one book's worth. |
| `PeacefulEnd.Parchment_SetVariable` | `<bookId> <variableId> <value>` | Set a [variable](../reference/variables.md) a book declares. Everything past the variable ID counts as the value. |
| `PeacefulEnd.Parchment_ClearVariable` | `<bookId> <variableId>...` | Return one or more of a book's variables to their declared `Default`. |
| `PeacefulEnd.Parchment_ToggleVariable` | `<bookId> <variableId>...` | Flip one or more `Boolean` variables. |
| `PeacefulEnd.Parchment_IncrementVariable` | `<bookId> <variableId> [amount]` | Move a `Number` variable by `amount`, which defaults to `1`. Negative steps down. |
| `PeacefulEnd.Parchment_ShowElement` | `<elementId>` | Put up an element carrying a `Lifetime`. See [Timed elements](#timed-elements). |
| `PeacefulEnd.Parchment_RefreshBook` | | Ask the open book to rebuild itself. Only works for a [book built in C#](../reference/building-books.md#refreshing-an-open-book) whose builder was given an `OnRefresh`. |

### Skipping the turn

Every action that moves the reader takes an optional trailing `skipAnimation`. Pass `true` and the book lands on the target spread on the spot, with no page turning between:

```json
"Action": "PeacefulEnd.Parchment_JumpToPageId results true"
```

Use it where the turn would read as a delay rather than as a page being turned: swapping a browse view for a search result, returning to where the reader was after they clear a filter, or any jump that happens without the reader clicking anything.

Since it's always last, an action with optional arguments before it needs those filled in first. `JumpToPageId` searches the whole book when its `chapterId` is omitted, so reaching `skipAnimation` on it means naming a chapter or passing an explicit `null`:

```json
"Action": "PeacefulEnd.Parchment_JumpToPageId mushrooms foraging true"
```

Nothing is heard either. The turn sound belongs to a page being turned, and a swap the reader didn't watch happen has nothing to announce.

!!! note "A search box keeps the keyboard"
    An [`Input`](../reference/elements/input.md) on the book's own `Underlay` or `Overlay` holds keyboard focus through a page turn, skipped or not, since it's on screen whatever page is being read. A reader who triggers a jump by typing carries on typing. One on a page loses focus with the page, and shutting or closing the book drops it whatever holds it.

!!! note "`ViewCover` and `CloseBook` aren't included"
    Neither is a page turn. They play the book's shut and close animations, which are the book's own rather than the page's, and skipping those would need a different field on [`Animation`](../reference/book.md#animation).

### Scope

The distinction matters once you're using [chapters](../reference/page.md#chapters):

| Scope | Actions |
| --- | --- |
| **Chapter**: stays where you are | `NextPage`, `PreviousPage`, `FirstPage`, `LastPage` |
| **Book**: can cross chapters | `JumpToPage`, `JumpToChapter`, `JumpToChapterPage`, `JumpToPageId`, `GoToStart`, `GoBack` |

A chapter is navigation-isolated: page turning can't leave it, and neither can `NextPage`. The book-scoped actions are the only way out, which is why a chapter needs at least one of them somewhere in it.

`FirstPage` therefore means "back to the top of what I'm reading", and `GoToStart` means "back to the front of the book", usually your table of contents.

!!! tip "Name your way home"
    `JumpToChapter contents` says what it means and survives you reordering pages. `GoToStart` only works while your contents page happens to be first.

### Going back

`GoBack` is not `PreviousPage`. `PreviousPage` turns one spread towards the front of the chapter. `GoBack` returns to wherever the reader actually came from, which after a jump is a different place entirely.

Parchment records every spread the reader leaves, whether they got there by turning a page, clicking a corner or following a jump. `GoBack` pops the most recent entry and returns there, so calling it again goes back a further step and a chain of jumps unwinds in the order it was made.

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/button",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Text": "Back",
  "Action": "PeacefulEnd.Parchment_GoBack",
  "Condition": "PeacefulEnd.Parchment_CanGoBack"
}
```

[`PeacefulEnd.Parchment_CanGoBack`](conditions.md#the-page) is true while there is anywhere to return to, so a back button can take itself off the first page the reader lands on.

**Going back doesn't record a step of its own.** Backing out of a jump doesn't leave a way to jump forward into it again, which is what stops a pair of pages trapping the reader bouncing between them. There is no matching "forward".

**The history is a reading session, not a save.** It starts empty each time the book is opened and is forgotten when the reader leaves. Shutting the book to its [cover](../reference/book.md#cover-view) keeps it, since the reader hasn't gone anywhere.

**It remembers the last 64 spreads.** Past that the oldest entry is dropped. A reader would have to cross-link their way through a very large book to notice.

!!! tip "Pair it with a keybind"
    `GoBack` on an [`OnKeyPress`](../reference/page.md#on-key-press) bound to `Escape` turns a chapter into something the reader backs out of a step at a time. Put it on the [book](../reference/book.md#on-key-press) instead and it covers every page at once. Holding the key still leaves the book.

### Addressing a page

Three actions land on one specific page, differing only in how you name it:

| Action | Counts from | Survives reordering |
| --- | --- | --- |
| `JumpToPage 12` | The start of the book | No |
| `JumpToChapterPage rites 4` | The start of that chapter | Only across other chapters |
| `JumpToPageId shrine` | Nothing, it's a name | Yes |

`JumpToPageId` is the one to reach for in a table of contents. A page's [`Id`](../reference/page.md) is stable, so inserting a page ahead of the target doesn't quietly send readers somewhere else.

A duplicate `Id` resolves to the first match in book order, which is why the optional `chapterId` exists:

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/button",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Text": "The shrine",
  "Action": "PeacefulEnd.Parchment_JumpToPageId shrine rites"
}
```

Both of these land on a spread rather than a single page. Asking for the right-hand page of a spread shows that spread, so `JumpToChapterPage rites 1` and `JumpToChapterPage rites 0` look the same.

## Timed elements

An element with a `Lifetime` isn't part of the page. It starts hidden, appears when `ShowElement` names it and takes itself away when its time is up. Any type can do this, so it suits a warning bubble, a stamp that lands on a completed page or an icon that flashes up to confirm something:

```json title="content.json"
{
  "Id": "noSlotsBubble",
  "Type": "Banner",
  "Text": "{{i18n: bookmarks.full}}",
  "Lifetime": 3,
  "FadeAfter": 2,
  "IgnoreCursor": true
}
```

That holds for two seconds, fades over the third and goes. Pressing again while it's still up restarts the three seconds rather than putting up a second one.

!!! tip "Set `IgnoreCursor` on anything drawn over the page"
    A timed element sitting over the page would otherwise take the clicks meant for whatever is under it, including the button that brought it up.

A `Condition` still applies alongside the timer, and both have to pass, so a timed element can be tied to where the reader is as well as to how long it has been up. `ShowElement` finds the element wherever it sits, so an ID used on several pages brings it up on each of them.

---

## Session flags

A flag is a name a book sets to remember something for as long as it stays open. [`PeacefulEnd.Parchment_HasFlag`](conditions.md#session-flags) reads it back, so anything a condition can gate is gateable on something that happened earlier in the reading.

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/lever",
  "Text": "Pull the lever",
  "Action": "PeacefulEnd.Parchment_SetFlag leverPulled",
  "Condition": "!PeacefulEnd.Parchment_HasFlag leverPulled"
}
```

Both actions take a list, so `PeacefulEnd.Parchment_ClearFlag leverPulled doorOpened` resets several at once.

!!! warning "Flags don't survive the book closing"
    Every flag is dropped when the reader puts the book down, which is what makes them right for "has this happened during this reading" and wrong for "has the player ever done this". For the second, use a mail flag through the game's own `AddMail` action, which is saved.

Flags are shared across books, so a name set by one book is visible to another opened afterwards in the same session. Prefix them with your mod's ID if that matters to you.

## Variables

Where a flag is a name that lasts the reading, a [variable](../reference/variables.md) is a named value that outlives it. A book declares its variables up front, then sets them with `SetVariable`, `ClearVariable` and `ToggleVariable` and reads them back with `%Variable:id%` or [`PeacefulEnd.Parchment_HasVariable`](conditions.md#variables).

```json title="content.json"
{
  "Type": "Button",
  "Text": "Show spoilers: %Variable:showSpoilers%",
  "Action": "PeacefulEnd.Parchment_ToggleVariable {{ModId}}_Almanac showSpoilers"
}
```

`IncrementVariable` takes one variable rather than a list, since a trailing amount couldn't be told apart from another name. That's what a stepper wants:

```json title="content.json"
{
  "Type": "Button",
  "Text": "Bigger",
  "Action": "PeacefulEnd.Parchment_IncrementVariable {{ModId}}_Almanac fontSize",
  "Condition": "!PeacefulEnd.Parchment_HasVariable {{ModId}}_Almanac fontSize 5"
}
```

!!! tip "Declare the range and drop the condition"
    A variable with [`Min` and `Max`](../reference/variables.md#bounds) stops at its bounds on its own, so the `Condition` above is only needed when you also want the button to disappear at the end of the range. Without bounds a stepper keeps going as far as it's pressed.

Every variable action names the book that declares it, so they work from `Data/TriggerActions` as readily as from a button inside the book. The `%Variable:id%` token is the exception, since it only appears inside a book's text and takes that book as read.

Unlike flags, variables are per book and have to be declared. See [Variables](../reference/variables.md) for the declaration and what it buys you.

### Making the page respond

Setting a variable doesn't rewrite anything by itself. A [`Condition`](conditions.md#variables) on each element is what makes a page respond, and it does so within a few ticks of the click:

```json title="content.json"
{
  "Type": "Paragraph",
  "Text": "{{i18n: spoiler.pufferfish}}",
  "Condition": "PeacefulEnd.Parchment_HasVariable {{ModId}}_Almanac showSpoilers true"
}
```

!!! warning "A content pack can't rebuild a page"
    Conditions are the whole story for a pack, so write out every variant and let them decide which shows. The [`Variables` token](../reference/variables.md#reading-one-from-content-patcher) reaches patches on Content Patcher's next context update, and an open menu pauses the clock that runs on, so a patch keyed on a variable won't re-apply while the book is being read.

    `PeacefulEnd.Parchment_RefreshBook` doesn't help here either. Rebuilding means re-running the code that generated the pages, which only a C# book has.

## Tokens

A token is a placeholder replaced with something the book knows at the moment it's needed. They work in an action, resolved just before it runs, and in an element's [`Text`](../reference/elements/index.md), resolved as it's laid out. The same token means the same thing in both.

| Token | Means |
| --- | --- |
| `%Input%` | This element's own typed text. Only valid on an [`Input`](../reference/elements/input.md). |
| `%Input:someId%` | The text in the input with that `InputId`, from any element. |
| `%Item%` | The qualified item ID the element is showing. Only valid inside a [`Grid`](../reference/elements/grid.md#source) result cell. |
| `%Item.Something%` | A property of that item. See [Item properties](#item-properties). |
| `%Variable:someId%` | The current value of one of the open book's [variables](../reference/variables.md). |
| `%GridDisplayed:someId%` | How many cells a grid is currently showing. |
| `%GridMatched:someId%` | How many candidates matched, which is larger than the above once the matches outnumber the cells. |
| `%GridTotal:someId%` | How many candidates the grid has before any filtering. |

The grid tokens read a [`Grid`](../reference/elements/grid.md) by its `Id`, found on the book's own layers or on either visible page. They work on a grid with authored children too, where "matched" means visible children and "total" means all of them.

```json
{
  "Type": "Paragraph",
  "Text": "Showing %GridDisplayed:fish% of %GridMatched:fish% matches, out of %GridTotal:fish% fish."
}
```

### Item properties

Inside a result cell, `%Item%` on its own is the qualified ID, and a dot reaches one of the item's properties:

| Token | Gives | Sorts as |
| --- | --- | --- |
| `%Item.Id%` | The unqualified ID, `128` rather than `(O)128`. | text |
| `%Item.Name%` | The display name, translated. | text |
| `%Item.InternalName%` | The internal name, which never translates. | text |
| `%Item.Description%` | The description. | text |
| `%Item.Type%` | The object type, such as `Fish` or `Arch`. | text |
| `%Item.Category%` | The category **name**, such as `Fish`, rather than the number behind it. | text |
| `%Item.Price%` | The sale price. | number |

```json
{
  "Type": "Panel",
  "Children": [
    { "Type": "Image", "Scale": 3, "Alignment": "Center" },
    { "Type": "Paragraph", "Text": "%Item.Name%", "Alignment": "Center" }
  ]
}
```

The same names are what a [`Grid`](../reference/elements/grid.md#ordering)'s `Source` orders by, and the last column is how each one compares when it does. `Id` sorts as text rather than as a number, since a mod's item is as likely to be `Bob.Cool_Sword` as it is to be `128`.

The list is fixed rather than reaching into the item for whatever it happens to have. That keeps these names Parchment's to keep: a game update that renames something underneath one of them is a fix here rather than a break in your book. Ask for something not on the list and the token is left in place, with the accepted names logged.

!!! note "An unknown token is left alone"
    `%GridTotal:fsh%` isn't replaced with nothing, it stays in the text and is logged. Anything the vocabulary doesn't recognise is passed through untouched, so ordinary prose containing a `%` survives. Write `%%` where you need a literal one next to something token-shaped.

```json
{
  "Type": "Input",
  "InputId": "entry",
  "TexturePath": "{{ModId}}/box",
  "Placeholder": "Go to entry...",
  "SubmitAction": "PeacefulEnd.Parchment_JumpToPageId %Input%"
}
```

In an **action** a value is substituted already quoted, so a typed phrase arrives as one argument rather than several, and quotes the reader typed are dropped since trigger actions have no way to escape them. In **text** it's substituted as-is.

!!! warning "Text tokens cost a relayout"
    A token in text changes what the element measures, so the page is laid out again whenever one resolves differently. That's fine at typing speed, and worth thinking about if you ever point a token at something that changes every tick.

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

A page can also bind actions to a key through [`OnKeyPress`](../reference/page.md#on-key-press), which fires while the page is on screen and takes the key over from the menu:

```json
{
  "Id": "riddle",
  "ChapterId": "riddles",
  "OnKeyPress": [
    {
      "Keybind": "Escape",
      "Actions": [ "PeacefulEnd.Parchment_JumpToPageId riddles contents" ]
    }
  ]
}
```

That example turns the exit key into a back button for one chapter. The reader can still leave by holding it for three seconds, which Parchment handles for you.


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

**A missing page ID fails at click time.** `JumpToPageId` can't be checked when the book loads, so a renamed or deleted page shows up as a warning in the SMAPI log the moment a reader presses the button rather than when you author it.

**Nothing validates the action name up front.** A typo'd action fails when the player clicks it, with a warning in the SMAPI log naming the string you wrote. Test your buttons.

**An action that can't run still plays its sound and looks clickable.** `PreviousPage` on a chapter's first page does nothing. There's no way to grey a button out. Use a `Condition` to hide it instead.

**A list runs to the end regardless.** There's no per-action condition inside `Actions` or `HoverActions`, and no early exit on failure. Wrap an individual entry in `If <query> ## <action>` when only part of the list should apply.

**An empty entry drops the element.** `Actions` or `HoverActions` containing `""` fails validation and the element is skipped with a warning, matching how a page's [`OnView`](../reference/page.md#on-view) treats one.

**A long `HoverActions` list is easy to trip over repeatedly.** A reader crosses elements just by moving the mouse, and every entry runs each time. Keep the whole list harmless to repeat, not just its first action.

**Every matching keybind fires, not just the first.** Two [`OnKeyPress`](../reference/page.md#on-key-press) entries bound to the same key both run, and so does a matching entry on the other page of the spread. An element's `Action` doesn't behave this way because the cursor can only be on one element.
