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

Conditions work on every element, in every list: a page's `Elements`, `Background` or `Foreground`, the book's `Underlay` or `Overlay` and a panel's `Children`. Individual [animation frames](#animation-frames) take one too.

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

Parchment adds queries about the book being read, plus one pair about what the player has read before.

Most describe the open book, so they only work while a book is open (otherwise they return false). The [reading history](#reading-history) queries are the exception: they read stored page / chapter read history.

### The book

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_IsBookOpen` | — | A Parchment book is open. |
| `PeacefulEnd.Parchment_CurrentBookId` | `<bookId>` | The open book has this `Id`. |
| `PeacefulEnd.Parchment_CurrentBookState` | `<state>` | The book is in this state. See [Book states](#book-states). |

### Page tags

These read a page's [`Tags`](../reference/page.md#tags), the keywords a page carries so other pages can find it. Matching ignores case.

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_CurrentPageHasTag` | `<tag>...` | Either page on screen carries any of the tags. |
| `PeacefulEnd.Parchment_PageHasTag` | `<pageId> <tag>...` | The named page carries any of the tags, wherever it sits in the book. |
| `PeacefulEnd.Parchment_PageTagMatchesInput` | `<pageId> <inputId>` | What's typed into an [`Input`](../reference/elements/input.md) appears in any of the named page's tags. An empty input matches every tagged page, and a page with no tags never matches. |

### Reader input

These read what the reader has typed into an [`Input`](../reference/elements/input.md) element. Unlike the rest, they don't need a book open, since the text lasts for the reading session.

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_InputMatches` | `<inputId> <text>` | The typed text appears in `<text>`, ignoring case. **An empty input matches everything**, so an untouched search box leaves the whole list showing. Everything past the ID counts as one piece of text, so a phrase needs no quoting. |
| `PeacefulEnd.Parchment_InputEquals` | `<inputId> <value>...` | The typed text is exactly one of the values, ignoring case. |
| `PeacefulEnd.Parchment_HasInputText` | `<inputId>` | The input has anything typed into it. |

The direction of `InputMatches` is the thing to keep straight: the typed text is what you're searching **for**, the argument is what you're searching **in**. So a row for tulips reads `PeacefulEnd.Parchment_InputMatches search Tulip` and appears whenever what the reader typed fits inside the word Tulip.

### The page

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_CurrentPageId` | `<pageId>` | Either visible page has this `Id`. |
| `PeacefulEnd.Parchment_CurrentPageIndex` | `<pageIndex>` | Either visible page is at this index. Indexes start at **0**, so the first spread is pages 0 and 1. |
| `PeacefulEnd.Parchment_CurrentChapterId` | `<chapterId>` | The reader is in this chapter. |
| `PeacefulEnd.Parchment_IsFirstPage` | — | The book's very first page is visible, not the current chapter's. |
| `PeacefulEnd.Parchment_IsLastPage` | — | The book's very last page is visible, not the current chapter's. |
| `PeacefulEnd.Parchment_IsPagingForward` | — | A page turn is in progress and it's going forward. Only meaningful while the book is `Turning`. |
| `PeacefulEnd.Parchment_CanGoBack` | — | The reader has somewhere to return to, so [`GoBack`](actions.md#going-back) would do something. |

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

### Reading history

Two queries ask what the current player has read before, rather than what's on screen now. Unlike the queries above they don't need a book open, so you can use them anywhere a game state query is accepted: on a page to mark a chapter as already read, but also in an event, a dialogue line or another mod's condition.

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_HasSeenChapterId` | `<bookId> <chapterId>` | The player has seen that chapter of that book. |
| `PeacefulEnd.Parchment_HasSeenPageId` | `<bookId> <chapterId> <pageId>` | The player has seen that page of that chapter. |
| `PeacefulEnd.Parchment_HasSeenChapterlessPageId` | `<bookId> <pageId>` | The player has seen that page, where the page has no `ChapterId`. |

```json
{
  "Type": "Heading",
  "Text": "You've read this before.",
  "Condition": "PeacefulEnd.Parchment_HasSeenChapterId {{ModId}}_CampingGuide tents"
}
```

**Where it's stored.** The history lives in two data assets Parchment provides, `Data/PeacefulEnd.Parchment/SeenChapters` and `Data/PeacefulEnd.Parchment/SeenPages`. Each is a dictionary keyed by player name, whose value is the list of entries that player has seen:

- a seen **chapter** is `<bookId>.<chapterId>`, for example `{{ModId}}_CampingGuide.tents`
- a seen **page** is `<bookId>.<chapterId>.<pageId>`, for example `{{ModId}}_CampingGuide.tents.page_one`
- a page with no `ChapterId` leaves the middle segment empty, so it reads `<bookId>..<pageId>`. That's what `HasSeenChapterlessPageId` looks up

Because they're ordinary data assets, you can edit them with Content Patcher. Parchment reloads its copy whenever the asset changes, so an edit takes effect on the spot. Blanking a player's list resets their history:

```json
{
  "Action": "EditData",
  "Target": "Data/PeacefulEnd.Parchment/SeenChapters",
  "Entries": {
    "{{PlayerName}}": null
  }
}
```

The key is the player's name (the same `Name` the game stores on the farmer), so Content Patcher's `{{PlayerName}}` token is an easy way to target the current player's list.

Setting the entry to `null` drops it entirely. A missing player reads as having seen nothing, the same as an empty list. `Data/PeacefulEnd.Parchment/SeenPages` clears the same way.

To change individual entries instead of replacing the whole list, use Content Patcher's `TargetField` to descend into one player's list first. A player's value is a `List<string>`, so it takes the same add / remove / replace shorthand Content Patcher uses on a string list like `Data/Objects` → `ContextTags`, where each entry acts as its own key.

The entries here are whole seen keys, not book IDs:

```json
{
  "Action": "EditData",
  "Target": "Data/PeacefulEnd.Parchment/SeenChapters",
  "TargetField": [ "{{PlayerName}}" ],
  "Entries": {
    "{{ModId}}_CampingGuide.tents": "{{ModId}}_CampingGuide.tents", // mark this chapter seen
    "{{ModId}}_CampingGuide.cover": null                            // mark this chapter unseen
  }
}
```

`SeenPages` works the same way, keyed by the three-part page entry, for example `{{ModId}}_CampingGuide.tents.page_one`.

`TargetField` only descends into an entry that already exists, so the player needs a list before you can edit it this way (they get one the first time they read anything). To seed a player who has none yet, set their whole list with a top-level `Entries` patch instead:

```json
{
  "Action": "EditData",
  "Target": "Data/PeacefulEnd.Parchment/SeenChapters",
  "Entries": {
    "{{PlayerName}}": [ "{{ModId}}_CampingGuide.tents" ]
  }
}
```

Either route lets you mark something read without the player ever opening the book, using the key formats above.

## Book states

`CurrentBookState` takes one of:

| State | Meaning |
| --- | --- |
| `Sliding` | The closed book is sliding up from the bottom of the screen. |
| `Opening` | The book is playing its open animation. |
| `Ready` | The book is open and settled. This is the normal state. |
| `Turning` | A page turn is in progress. |
| `Covering` | The book is shutting to its cover, without leaving the menu. |
| `Cover` | The book is shut with its cover on screen, either before it's first opened or after it's closed. See [cover view](../reference/book.md#cover-view). |
| `Closing` | The book is playing its close animation before leaving. |

Every state is testable, including the brief ones, and conditions keep being re-evaluated throughout. Bear in mind what's actually on screen in each: pages aren't drawn until the book is open, so a condition on `Sliding` or `Opening` only means anything for the book's own [`Underlay` and `Overlay`](../reference/book.md#fields).

`Cover` is how you decorate a closed book, since the pages aren't drawn there and the book's `Overlay` is all that's left:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/coverTitle",
  "Position": { "X": 44, "Y": 72 },
  "Condition": "PeacefulEnd.Parchment_CurrentBookState Cover"
}
```

Test `Covering` as well as `Cover` when the art should be there for the shutting animation rather than appearing once it finishes. The same pairing works at the other end: `Sliding` and `Opening` cover the book's arrival.

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

## Animation frames

An [`Image`](../reference/elements/image.md)'s animation frames each accept a `Condition`, so parts of an animation can come and go without the element itself doing so.

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/pond",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 32, "Height": 32 },
  "Frames": [
    { "SourcePoint": { "X": 0, "Y": 0 } },
    { "SourcePoint": { "X": 32, "Y": 0 } },
    { "SourcePoint": { "X": 64, "Y": 0 }, "Condition": "!SEASON Winter" }
  ]
}
```

A failing frame is skipped rather than held on, so the cycle shortens and the surviving frames run closer together. It's the same behaviour as a hidden element letting the ones below it close the gap.

When every frame fails, the element draws `TextureSourceRectangle` by itself and the animation simply stops. That makes an all-conditional animation a clean way to say "move only under these circumstances", provided the source rectangle points at a sprite worth showing still.

Frame conditions are checked on the same schedule as element conditions and they never affect layout: an image is sized by its source rectangle, not by whichever frame is showing.

Because a changed frame list is a changed animation, the cycle restarts from its first frame whenever one of these conditions flips. Gate *every* frame on `PeacefulEnd.Parchment_CurrentPageId <your page>` and you get an animation that plays from the top each time the reader turns to that page.

## When conditions are checked

Several times a second for as long as the menu is up, from the moment the book starts sliding in through to it leaving. On top of that they're re-checked immediately whenever something might have changed the answer: on every change of book state, and when an element's action runs.

That means a condition can react to the player gaining an item from a button on the facing page. It also means a condition is a *live* question, not a one-time filter. Don't write one that's expensive to answer.

An element with a `Condition` starts hidden and appears once the query first passes, so a book never flashes content it shouldn't.

## Gotchas

**A malformed query is false.** A typo hides the element rather than raising an error, which looks identical to a condition that simply didn't pass. If an element won't appear and you can't see why, check the query by hand. The failure is quiet.

**Conditions can't ask about layout.** There's no query for "is this element off the page" or "how much room is left". Conditions decide *whether* something appears, not *where*.

**Chapter-scoped actions, book-scoped queries.** `IsFirstPage` and `IsLastPage` are about the **book**, while the [`FirstPage` and `LastPage` actions](actions.md#scope) are about the **current chapter**. Hiding a "last page" button with `!IsLastPage` will do the wrong thing inside a chapter. Use `!CurrentPageId <the chapter's last page>` instead.
