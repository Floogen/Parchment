# Page

`PageData`

A page is a stack of elements. Two consecutive pages make a spread. Page 0 and 1 are the first spread's left and right leaves, 2 and 3 the second, and so on.

```json
{
  "Id": "cover",
  "ChapterId": "chapter-1",
  "Elements": [ ... ]
}
```

---

## Fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` <span class="req">required</span> | `string` | — | An identifier for the page, unique within the book. Actions and conditions can refer to a page by ID, which survives inserting pages in a way that a page number doesn't. |
| `ChapterId` <span class="opt">optional</span> | `string` | — | The chapter this page belongs to. Pages sharing a value belong to the same chapter and **must be listed consecutively**. See [Chapters](#chapters). |
| `Tags` <span class="opt">optional</span> | list of `string` | empty list | Keywords describing what's on the page, never shown to the reader. A contents entry or a search box matches against them. See [Tags](#tags). |
| `Elements` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | The page's content, stacked top to bottom in order. |
| `Background` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **behind** `Elements`, placed by their `Position` rather than stacked. They don't affect the layout, so they can't push anything around. Use them for flourishes, watermarks or page texture. They can carry a tooltip or an action, see [Background and foreground](#background-and-foreground). |
| `Foreground` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **over** `Elements`, placed by their `Position` rather than stacked. They don't affect the layout, so they can't push anything around. Use them for flourishes, watermarks or page texture. They can carry a tooltip or an action, see [Background and foreground](#background-and-foreground). |
| `OnView` <span class="opt">optional</span> | list of [`triggers`](#on-view) | empty list | Actions run each time the page becomes visible, without the reader clicking anything. See [On view](#on-view). |
| `OnKeyPress` <span class="opt">optional</span> | list of [`keybinds`](#on-key-press) | empty list | Keys running actions while the page is on screen, which can take a key over from the menu and from the [book's own binds](book.md#on-key-press). See [On key press](#on-key-press). |

---

## Background and foreground

`Background` and `Foreground` hold placed elements rather than stacked ones, drawn under and over `Elements` respectively. Everything else about an element still applies here: `Condition` hides it, `Frames` animate it and `DisplayName` and `Description` give it a hover tooltip.

The cursor works through the three lists from the top down, so `Foreground` gets first refusal, then `Elements`, then `Background`. Within a list the first match wins and a container's children are tested before the container itself.

`Alignment` and `VerticalAlignment` work here as well, anchoring the element within the page's content area with `Position` measured from that anchor, so a centred image or a footer pinned to the bottom needs no eyeballing against the page's size. See [Alignment anchors, position offsets](../concepts/layout.md#alignment-anchors-position-offsets).

```json
{
  "Id": "shrine",
  "Elements": [
    { "Type": "Paragraph", "Text": "..." }
  ],
  "Foreground": [
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/inkblot",
      "Position": { "X": 220, "Y": 96 },
      "DisplayName": "Ink blot",
      "Description": "Someone has spilled over this passage."
    }
  ]
}
```

### Decorative elements are transparent to the cursor

A placed element only claims the cursor when it has something to offer: a `Description`, a `DisplayName`, an `Action` or `Actions`, a `HoverAction` or `HoverActions`, or a `HoverTextureSourceRectangle`. An element with none of those is passed straight through as if it weren't there.

That rule exists because these two lists are usually art. A full-page border in `Foreground` would otherwise sit over every button on the page and swallow the lot.

A plain container is transparent even when its children aren't, so a `Panel` with no tooltip of its own can hold an `Image` that has one and only the image reacts.

!!! note "This applies to pages, not to the book"
    [`Book.Underlay` and `Book.Overlay`](book.md) are hit-tested whatever they contain, so a decorative element there does claim the cursor. `Page.Elements` is likewise always hit-tested, since a stacked element takes up space that nothing else can occupy anyway. `IgnoreCursor` is how an element in one of those lists opts out.

### Passing the cursor through

`IgnoreCursor` makes one element transparent to the cursor whatever list it sits in. It's the explicit form of the rule above, for the three lists that don't apply it on their own: `Page.Elements`, `Book.Underlay` and `Book.Overlay`.

```json title="A book overlay that doesn't cover the pages"
{
  "Overlay": [
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/gilt-frame",
      "IgnoreCursor": true
    }
  ]
}
```

It doesn't carry down to what the element contains. A `Panel` with `IgnoreCursor` still lets its children, its own `Background` and its own `Foreground` be hovered and clicked, so a decorative frame can hold live buttons.

!!! warning "It can't be combined with an action"
    An element with `IgnoreCursor` alongside `Action`, `Actions`, `HoverAction` or `HoverActions` fails validation, since the cursor never reaches it to run them. The same goes for an [`Input`](elements/input.md), which has to be clickable to work at all.

    `DisplayName`, `Description`, `HoverTextureSourceRectangle` and `HoverFrames` are allowed through with a warning in the log, so an [`Image`](elements/image.md) drawing an `ItemId` can drop the tooltip the item hands it without having to blank the fields by hand.

---

## Chapters

A chapter is a run of consecutive pages sharing a `ChapterId`. Chapters are **navigation-isolated**: turning a page never crosses a chapter boundary, and the corner curls disappear at a chapter's first and last spread the same way they do at the book's ends.

The only way in or out of a chapter is an [action](../concepts/actions.md), usually a `Button`. That's the point: it lets you build a book where a section is only reachable from a table of contents, or where the reader can't wander out of an appendix by turning pages.

Pages with no `ChapterId` form a chapter of their own, so a book that never mentions chapters is one chapter and behaves exactly as you'd expect.

Each chapter's spreads start fresh, so a chapter with an odd number of pages ends with a blank right leaf and the next chapter starts on a new spread. That's how a printed book behaves too.

!!! warning "Chapters must be contiguous"
    A chapter is derived from where its pages sit in the list, not declared separately. If pages with the same `ChapterId` appear in two separate runs, they become two chapters and only the first is reachable by ID. Parchment logs a warning when this happens.

---

## Tags

`Tags` are keywords a page carries for other pages to find it by. Nothing draws them, so they're free to hold terms the page's own text doesn't use, including alternative spellings and words a reader would search for but an author wouldn't write.

```json title="A page describing its own subject"
{
  "Id": "mushrooms",
  "ChapterId": "foraging",
  "Tags": [ "mushroom", "fungus", "morel", "chanterelle", "cave" ],
  "Elements": [ ... ]
}
```

Three queries read them, all matching case-insensitively:

| Query | Arguments | True when |
| --- | --- | --- |
| `PeacefulEnd.Parchment_CurrentPageHasTag` | `<tag>...` | Either page on screen carries any of the tags. |
| `PeacefulEnd.Parchment_PageHasTag` | `<pageId> <tag>...` | The named page carries any of the tags, wherever it is in the book. |
| `PeacefulEnd.Parchment_PageTagMatchesInput` | `<pageId> <inputId>` | What's typed into an [`Input`](elements/input.md) appears in any of the named page's tags. |

`CurrentPageHasTag` suits something that follows the reader, such as a book [`Overlay`](book.md#fields) marker that only appears on recipe pages. The other two take a page ID, so an entry on a contents page can ask about the page it links to rather than the page it sits on.

### A searchable contents page

`PageTagMatchesInput` is the pairing an index wants. Each entry hides itself when the reader's search doesn't match the page it points at:

```json
{
  "Id": "contents",
  "Elements": [
    { "Type": "Input", "InputId": "search", "TexturePath": "{{ModId}}/box", "Placeholder": "Search..." },
    {
      "Type": "Button",
      "TexturePath": "{{ModId}}/button",
      "Text": "Mushrooms",
      "Action": "PeacefulEnd.Parchment_JumpToPageId mushrooms",
      "Condition": "PeacefulEnd.Parchment_PageTagMatchesInput mushrooms search"
    }
  ]
}
```

An empty search box matches every **tagged** page, so the full contents shows until the reader types. A page with no tags at all never matches, which is the one asymmetry worth remembering: tag every page you want listed, even if only with its own name.

!!! note "Tags aren't searched across the book for you"
    Each entry names the page it asks about. There's no query that returns "every page matching this text", since a condition can only show or hide an element that already exists. See [the note on filtering](elements/input.md#filtering-a-list) for why a long list still has to fit the page.

## On view

`OnView` runs [trigger actions](../concepts/actions.md) when the page becomes visible, with no click involved. Each entry pairs a condition with a list of actions.

```json
{
  "Id": "shrine",
  "ChapterId": "rites",
  "Elements": [ ... ],
  "OnView": [
    {
      "Condition": "!PeacefulEnd.Parchment_HasSeenPageId {{ModId}}_CampingGuide rites shrine",
      "Actions": [ "AddMoney 500", "AddMail Current PeacefulEnd.Parchment_ExampleMailIdTest All" ]
    }
  ]
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Condition` <span class="opt">optional</span> | `string` | — | A [game state query](../concepts/conditions.md) deciding whether `Actions` run. When omitted, they always run. |
| `Actions` <span class="req">required</span> | list of `string` | — | [Trigger actions](../concepts/actions.md), run in order. At least one entry is required. |

Entries are independent. Each condition is checked and each list runs on its own, so one page can carry several triggers firing under different circumstances.

**It fires on every view, not once.** Turning back to a page runs its triggers again, and so does closing the book and reopening it there. If something should happen only once, say so in the `Condition`. Parchment doesn't track it for you.

!!! warning "The condition is checked once, not polled"
    Every other `Condition` in Parchment is re-evaluated [several times a second](../concepts/conditions.md#when-conditions-are-checked). This one is evaluated at a single instant, whenever the page appears.

**Triggers run once the book settles.** Conditions are not evaluated until the book is in the `Ready` state. An action can therefore close the book or turn a page without fighting an animation.

**Both pages of a spread trigger, with left first.** A left page's triggers run before the right page's. If a left-page action changes which pages are visible, using `NextPage`, `JumpToChapter` or `CloseBook`, the right page's triggers don't run at all: they belonged to a spread that's no longer on screen. Put navigation last, or on the right page, when the rest of the spread still needs to fire.

**Pages outside a chapter use a different query.** `HasSeenPageId` needs a chapter to name. A page with no `ChapterId` is addressed by [`PeacefulEnd.Parchment_HasSeenChapterlessPageId`](../concepts/conditions.md#reading-history) instead, which takes just the book and page ID.

---

## On key press

`OnKeyPress` binds keys to [trigger actions](../concepts/actions.md) for as long as the page is on screen. A bind takes the key over from the menu, so a page can send the exit key somewhere other than out of the book.

```json title="books.json"
{
  "Id": "riddle",
  "ChapterId": "riddles",
  "Elements": [ ... ],
  "OnKeyPress": [
    {
      "Keybind": "Escape",
      "Actions": [ "PeacefulEnd.Parchment_JumpToPageId riddles contents" ]
    }
  ]
}
```

--8<-- "keybind-common.md"

### How binds behave

**They only fire while the page is being read.** A bind is dead through the opening, turning and closing animations and on a shut cover, matching where the page itself is legible.

**Every match runs.** All entries whose key matches and whose condition passes fire, in the order they're listed. That differs from an element, where the cursor can only be on one thing at a time.

**Both pages of a spread are live, with left first.** If a left-page action changes which pages are visible, the right page's binds don't run at all: they belonged to a spread that's no longer on screen.

**A page bind wins over a book bind.** When the spread and its book both bind the same key, only the page's entries run. See [the book's own binds](book.md#on-key-press).

**The reader can always get out.** Holding the exit key down for three seconds shuts the book and leaves the menu, whatever the page has bound it to, and regardless of [`ExitToCover`](book.md). Nothing needs authoring for this and there's no way to switch it off, so a page that takes the exit key can't strand anyone.

!!! tip "Overriding the exit key"
    Pair a bind on `Escape` with `PeacefulEnd.Parchment_JumpToPageId` and a chapter behaves like a menu the reader backs out of rather than closes. Give the destination page no bind of its own and the second press leaves the book as usual.
