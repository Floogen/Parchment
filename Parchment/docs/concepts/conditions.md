# Conditions

Any element can carry a `Condition`, a **game state query** deciding whether it appears.

```json
{
  "Type": "Paragraph",
  "Text": "The river is frozen over.",
  "Condition": "SEASON Winter"
}
```

When the query is false the element is hidden and the elements below it close the gap. It's a flow hide, not an invisibility toggle: nothing leaves a hole behind.

Conditions work on every element, in every list: a page's `Elements`, `Background` or `Foreground`, the book's `Underlay` or `Overlay` and a panel's `Children`.

## Writing queries

Game state queries are the game's own condition language, shared with Content Patcher and most 1.6 data assets. If you've written `"When": { ... }` in a CP pack, you've used the same vocabulary.

The full syntax and the list of vanilla queries are on the wiki:

> **[Modding: Game state queries](https://stardewvalleywiki.com/Modding:Game_state_queries)**

The parts you'll reach for most:

| Form | Meaning |
| --- | --- |
| `SEASON Winter` | A single query. |
| `!SEASON Winter` | Negated. |
| `SEASON Winter, WEATHER Here Snow` | Comma means **and**, every query must pass. |
| `ANY "SEASON Winter" "SEASON Fall"` | Or. |
| `PLAYER_HAS_ITEM Current (O)24` | Arguments are space-delimited. |
| `PLAYER_HAS_MAIL Current "some flag"` | Quote an argument containing spaces. |

## Parchment's queries

Parchment adds queries about the book being read. They only work while a book is open. Anywhere else they return false, which is the sensible answer.

### The book

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_IsBookOpen` | — | A Parchment book is open. |
| `PeacefulEnd.Parchment_CurrentBookId` | `<bookId>` | The open book has this `Id`. |
| `PeacefulEnd.Parchment_CurrentBookState` | `<state>` | The book is in this state. See [Book states](#book-states). |

### The page

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_CurrentPageId` | `<pageId>` | Either visible page has this `Id`. |
| `PeacefulEnd.Parchment_CurrentPageIndex` | `<pageIndex>` | Either visible page is at this index. Indexes start at **0**, so the first spread is pages 0 and 1. |
| `PeacefulEnd.Parchment_CurrentChapterId` | `<chapterId>` | The reader is in this chapter. |
| `PeacefulEnd.Parchment_IsFirstPage` | — | The book's very first page is visible, not the current chapter's. |
| `PeacefulEnd.Parchment_IsLastPage` | — | The book's very last page is visible, not the current chapter's. |
| `PeacefulEnd.Parchment_IsPagingForward` | — | A page turn is in progress and it's going forward. Only meaningful while the book is `Turning`. |

### The cursor

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_IsHoveringLeftPage` | — | The cursor is over the left page's content area. |
| `PeacefulEnd.Parchment_IsHoveringRightPage` | — | The cursor is over the right page's content area. |

!!! tip "Prefer `CurrentPageId` over `CurrentPageIndex`"
    A page ID survives you inserting a page halfway through the book. An index doesn't, and neither does anything written against it.

A worked example, from a bookmark that hides itself on the first page:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/bookmark",
  "Position": { "X": -64, "Y": 192 },
  "Action": "PeacefulEnd.Parchment_FirstPage",
  "Condition": "!PeacefulEnd.Parchment_CurrentPageId cover"
}
```

## Book states

`CurrentBookState` takes one of:

| State | Meaning |
| --- | --- |
| `Sliding` | The closed book is sliding up from the bottom of the screen. |
| `Opening` | The book is playing its open animation. |
| `Ready` | The book is open and settled. This is the normal state. |
| `Turning` | A page turn is in progress. |
| `Closing` | The book is playing its close animation. |

Only `Ready` and `Turning` are usefully testable: conditions aren't re-evaluated during the other three, so a condition can never see them. In practice `CurrentBookState` answers one question, "is a page being turned right now?", which is what you want it for.

Pair it with `IsPagingForward` to decorate a turn:

```json
{
  "Type": "Image",
  "TexturePath": "Characters/Junimo",
  "TextureSourceRectangle": { "X": 48, "Y": 0, "Width": 16, "Height": 16 },
  "Position": { "X": 128, "Y": 82 },
  "Scale": 4,
  "Condition": "PeacefulEnd.Parchment_CurrentBookState Turning, PeacefulEnd.Parchment_IsPagingForward"
}
```

## When conditions are checked

Several times a second while the book is open or turning, and immediately whenever something might have changed the answer: when a page turn lands, when an element's action runs and when the book finishes opening.

That means a condition can react to the player gaining an item from a button on the facing page. It also means a condition is a *live* question, not a one-time filter. Don't write one that's expensive to answer.

An element with a `Condition` starts hidden and appears once the query first passes, so a book never flashes content it shouldn't.

## Gotchas

**A malformed query is false.** A typo hides the element rather than raising an error, which looks identical to a condition that simply didn't pass. If an element won't appear and you can't see why, check the query by hand. The failure is quiet.

**Conditions can't ask about layout.** There's no query for "is this element off the page" or "how much room is left". Conditions decide *whether* something appears, not *where*.

**Chapter-scoped actions, book-scoped queries.** `IsFirstPage` and `IsLastPage` are about the **book**, while the [`FirstPage` and `LastPage` actions](actions.md#scope) are about the **current chapter**. Hiding a "last page" button with `!IsLastPage` will do the wrong thing inside a chapter. Use `!CurrentPageId <the chapter's last page>` instead.
