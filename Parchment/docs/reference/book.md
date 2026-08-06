# Book

`BookData`

A book is one entry in Parchment's book data. It owns the physical book (its sprite, its animations, the geometry of its pages) plus the pages themselves and any decoration drawn outside them.

```json
{
  "Format": "1.0.0",
  "Id": "PeacefulEnd.Parchment_CampingGuide",
  "Appearance": {
    "TintColor": "165 42 42"
  },
  "Pages": [ ... ]
}
```

---

## Fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Format` <span class="opt">optional</span> | `string` | `1.0.0` | The schema version this book was written against. |
| `Id` <span class="req">required</span> | `string` | — | A [unique string ID](https://stardewvalleywiki.com/Modding:Common_data_field_types#Unique_string_ID) for the book, conventionally `{ModId}_{Name}`. This is how actions and items refer to it, and it's the name Parchment uses in log messages. |
| `SpritePath` <span class="opt">optional</span> | `string` | — | The sprite used for the book's item. |
| `Pages` <span class="opt">optional</span> | list of [`pages`](page.md) | empty list | The book's pages, in reading order, except that pages sharing a [`ChapterId`](page.md#chapters) are read together wherever they're listed. Two consecutive pages make a spread. |
| `Underlay` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **behind** the book sprite, positioned by their `Position` relative to the book's top-left. Negative coordinates place them outside the book's edges. This is how you make a bookmark that sticks out of the side, with the part that overlaps the book hidden behind it. Drawn during the open and close animations, so they ride in with the book. |
| `Overlay` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **in front of** the book sprite and its pages, positioned by their `Position` relative to the book's top-left. Drawn in every state, so they ride in with the book, sit over the open spread and stay on the shut cover during [cover view](#cover-view). Give an element a [condition](../concepts/conditions.md#book-states) to hold it back to one of those. |
| `Appearance` <span class="opt">optional</span> | [`Appearance`](#appearance) | *the default book* | The book's sprite and animation frames. |
| `PageCurl` <span class="opt">optional</span> | [`PageCurl`](#page-curl) | *the default corners* | The corner curl sprite and its clickable areas. |
| `Animation` <span class="opt">optional</span> | [`Animation`](#animation) | *see below* | Animation timings and sounds. |
| `Layout` <span class="opt">optional</span> | [`Layout`](#layout) | *see below* | The page margins. |
| `OnKeyPress` <span class="opt">optional</span> | list of [`keybinds`](#on-key-press) | empty list | Keys running actions on every page of the book and on its shut cover. A page binding the same key takes it over. See [On key press](#on-key-press). |
| `Variables` <span class="opt">optional</span> | list of [`variables`](variables.md) | empty list | Named values the book sets and reads back, which survive the book being closed. See [Variables](variables.md). |
| `StartOnCover` <span class="opt">optional</span> | `boolean` | `false` | Whether the book arrives shut and holds on its cover until the reader clicks it open. See [Cover view](#cover-view). |
| `ExitToCover` <span class="opt">optional</span> | `boolean` | `false` | Whether closing the book shuts it in place first, leaving its cover on screen, rather than leaving the menu. See [Cover view](#cover-view). |

!!! note "Underlay and overlay elements always take the cursor"
    Unlike a page's [`Background` and `Foreground`](page.md#background-and-foreground), a decorative element here is hit-tested whether or not it has a tooltip or an action, and the overlay is tested before the pages. Keep overlay art to the area it actually covers rather than stretching a transparent sheet over the whole book, or set [`IgnoreCursor`](page.md#passing-the-cursor-through) on it so the cursor reaches the pages beneath.

!!! note "Both layers ride in with the book"
    Neither layer waits for the book to open. They're drawn from the moment it starts sliding up, through the open and close animations and on the cover. An overlay laid out against the open spread will therefore appear over the closed cover on the way in, so give it a [book state](../concepts/conditions.md#book-states) condition such as `ANY "PeacefulEnd.Parchment_CurrentBookState Ready" "PeacefulEnd.Parchment_CurrentBookState Turning"` when it should only exist once the book is open.

!!! note "The book's name and description live on the item"
    A book has no `Title` or `Description` of its own. What the player sees comes from the item that opens it, its `DisplayName` and `Description` in `Data/Objects`. That also means those are localisable through the usual `[LocalizedText ...]` tokens.

---

## Cover view

A book normally opens itself as soon as it has slid into view, and leaves the menu when it's closed. Two independent flags put a shut book on screen at either end of that:

| Field | What it changes |
| --- | --- |
| `StartOnCover` | The book arrives shut and waits. Clicking it opens it. |
| `ExitToCover` | Closing shuts the book in place instead of leaving. Clicking it reopens at the spread the reader left off on, and closing again leaves. |

```json title="content.json"
{
  "Format": "1.4.0",
  "Id": "you.CampingGuide_Book",
  "StartOnCover": true,
  "ExitToCover": true
}
```

Set both and the book behaves like a physical one: it's handed to the reader shut, opens when they open it and shuts without being taken away. Set neither (the default) and it opens and closes on its own. Either alone is fine, since they don't depend on each other.

It suits a book whose cover is worth looking at, and `ExitToCover` gives a reader somewhere to pause without losing their place.

!!! note "Skipping the slide"
    Clicking while the book slides in still skips the slide, and then respects `StartOnCover`: it lands on the cover rather than jumping the book open. Clicking during the opening animation still goes straight to the pages, since the reader has already asked for it open.

The book is drawn shut, so there are no pages, no page curls and no page elements. What remains is the book's own [`Overlay`](#fields), which draws in front of the shut cover. Give an element a condition on the `Cover` [book state](../concepts/conditions.md#book-states) and it appears only there:

```json
{
  "Overlay": [
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/coverTitle",
      "Position": { "X": 44, "Y": 72 },
      "Condition": "PeacefulEnd.Parchment_CurrentBookState Cover"
    }
  ]
}
```

`Underlay` still draws too, but it sits *behind* the book sprite, so it's for something poking out past the edges rather than for cover art.

An overlay element with an `Action` stays clickable on the cover, which is how you'd author a "Read" button. The click only falls through to reopening the book when it doesn't land on one.

!!! note "Reaching the cover from an action"
    [`PeacefulEnd.Parchment_ViewCover`](../concepts/actions.md#parchments-actions) shuts the book to its cover from a button. Neither flag gates the cover itself, they only decide whether the book goes there on its own.

!!! tip "Opening the book from an action"
    A navigating action such as [`GoToStart`](../concepts/actions.md#parchments-actions) runs on the cover too, opening the book onto the page it names rather than failing. That's what a "Read" button on the cover puts in its `Action`, and it means a cover can offer several ways in (the start, the last chapter, a bookmark) rather than the one the click gives. See [Navigating from the cover](../concepts/actions.md#navigating-from-the-cover).

!!! warning "`OnView` and the cover"
    A page's [`OnView`](page.md#on-view) triggers run when it's actually on screen, so with `StartOnCover` the first page's triggers wait until the reader opens the book rather than firing as it arrives. Reopening from the cover re-runs them, the same as turning back to a page would, so gate anything that shouldn't repeat on `HasSeenPageId`.

## Appearance

Everything about how the book itself is drawn. All defaults describe Parchment's built-in book, so you only need this block if you're making your own book art, and if you are, you'll want to set all of it, since the frame counts and offsets are facts about a specific sprite sheet.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `TexturePath` <span class="opt">optional</span> | `string` | *the built-in book* | The book's sprite sheet. Frames run **horizontally**, each `FrameWidth` × `FrameHeight`: first the open frames (index 0 fully closed, the last fully open), then the page-turn frames. The close animation is the open frames played backwards, so you don't supply those separately. |
| `GrayscaleTexturePath` <span class="opt">optional</span> | `string?` | *the built-in book* | An optional greyscale layer drawn beneath `TexturePath` and tinted by the book's `TintColor`. Multiplying a color into greyscale art gives you that color. Set to `null` to skip the recoloring. |
| `TintColor` <span class="opt">optional</span> | [`color`](elements/index.md#colors) | *white* | A color multiplied into the book's greyscale layer. |
| `FrameWidth` <span class="opt">optional</span> | `int` | `219` | The width of one frame, in unscaled sprite pixels. |
| `FrameHeight` <span class="opt">optional</span> | `int` | `158` | The height of one frame, in unscaled sprite pixels. |
| `OpenFrameCount` <span class="opt">optional</span> | `int` | `4` | How many open frames the sheet starts with. At least 1, since frame 0 is the book itself. |
| `TurnFrameCount` <span class="opt">optional</span> | `int` | `6` | How many page-turn frames follow the open frames. `0` for no turn animation, see [Books without a turn animation](#books-without-a-turn-animation). |
| `Scale` <span class="opt">optional</span> | `number` | `5` | How much the book sprite is magnified. Everything measured against the book art (the page margins, the curl offsets) scales with this. |
| `Offset` <span class="opt">optional</span> | `Point` | `{ X: 0, Y: 0 }` | A nudge applied to the book's centred position, in unscaled sprite pixels. The book is centred on its **frame**, so if your frame has empty space around the art it won't look centred. |

### Books without a turn animation

Set `TurnFrameCount` to `0` and the sheet holds nothing but the open frames. Turning still works, it just lands on the target spread at once rather than playing anything over the book. `TurnSound` still plays, since the reader asked for the turn and only the art is missing.

```json title="books.json"
{
  "Id": "you.Board_Book",
  "Appearance": {
    "TexturePath": "Mods/Your.BookModId/Board",
    "FrameWidth": 200,
    "FrameHeight": 140,
    "OpenFrameCount": 1,
    "TurnFrameCount": 0
  }
}
```

[`TurnDuration`](#animation) and [`ContentSwapProgress`](#animation) then do nothing, as there's no turn to spread across and no turning page for the content to change over behind.

!!! note "`OpenFrameCount` can't be `0`"
    Frame 0 isn't a step in an animation, it's the book: it's what's drawn while the book sits open or shut on its cover, and what the book's size on screen is measured from. Every book needs that one frame. A board that never animates is `OpenFrameCount: 1`, where frame 0 serves as the closed frame, the open frame and the cover all at once.

A one frame board that also wants no slide or open animation sets [`SlideDuration`](#animation) and [`OpenDuration`](#animation) to `1`, which is the shortest either accepts. They can't be `0`.

---

## Page curl

The corner you click to turn a page. The offsets are relative to the book frame, so they follow the book when it moves or changes scale.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `IsEnabled` <span class="opt">optional</span> | `bool` | `true` | Whether the book has curl corners. See [Books without corners](#books-without-corners). |
| `TexturePath` <span class="opt">optional</span> | `string` | *the built-in corner* | The curl sprite sheet. Frames run horizontally: frame 0 is flat, the last is fully curled. Hovering plays forward, un-hovering plays back. |
| `FrameWidth` <span class="opt">optional</span> | `int` | `32` | The width of one frame, in unscaled sprite pixels. |
| `FrameHeight` <span class="opt">optional</span> | `int` | `32` | The height of one frame, in unscaled sprite pixels. |
| `FrameCount` <span class="opt">optional</span> | `int` | `7` | How many frames the curl animation has. |
| `Scale` <span class="opt">optional</span> | `number` | `5` | How much the curl sprite is magnified. |
| `PreviousPageOffset` <span class="opt">optional</span> | `Point` | `{ X: 1, Y: 113 }` | The top-left of the back-turn corner, in unscaled sprite pixels relative to the book frame's top-left. |
| `NextPageOffset` <span class="opt">optional</span> | `Point` | `{ X: 186, Y: 112 }` | The top-left of the forward-turn corner, in unscaled sprite pixels relative to the book frame's top-left. |

!!! note "The corner and its hotspot are the same rectangle"
    Each corner's clickable area is exactly the sprite you see: the offset above, sized `FrameWidth` × `FrameHeight` × `Scale`. There's no separate hotspot to keep in sync.

The left corner is drawn mirrored, so one piece of art serves both sides.

### Books without corners

Set `IsEnabled` to `false` and neither corner is drawn or clickable. The rest of this section is then ignored, so there's no art to supply and no frame values to get right.

```json title="books.json"
{
  "Id": "you.Notepad_Book",
  "PageCurl": {
    "IsEnabled": false
  }
}
```

!!! danger "Give the reader another way to turn"
    The corners are the only page turning Parchment offers a mouse on its own, a controller having the triggers as well (see [controller support](../concepts/controller.md)). A book without corners and without any other way forward is a book stuck on its first spread for anyone reading with a mouse, and Parchment can't detect that at load, since a page turn can be bound anywhere.

    Provide a [`Button`](elements/button.md) running `PeacefulEnd.Parchment_NextPage`, or an [`OnKeyPress`](#on-key-press) bind on the book so the keys follow the reader through every page.

To keep the corners clickable but invisible, leave `IsEnabled` alone and point `TexturePath` at a transparent sprite.

---

## Animation

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `SlideDuration` <span class="opt">optional</span> | `number` | `350` | How long the closed book takes to slide up from the bottom of the screen, in milliseconds. Eased, so it starts fast and lands softly. |
| `OpenDuration` <span class="opt">optional</span> | `number` | `250` | How long the open animation takes, in milliseconds. The open frames are spread evenly across it. |
| `CloseDuration` <span class="opt">optional</span> | `number` | `400` | How long the close animation takes, in milliseconds. |
| `TurnDuration` <span class="opt">optional</span> | `number` | `500` | How long a page turn takes, in milliseconds. Ignored when `TurnFrameCount` is `0`. |
| `CurlDuration` <span class="opt">optional</span> | `number` | `250` | How long the corner curl takes to play through all its frames, in milliseconds. |
| `ContentSwapProgress` <span class="opt">optional</span> | `number` | `0.5` | The point in a page turn at which the page content changes over, from 0 to 1. Tune this so the swap happens while the turning page hides it. Ignored when `TurnFrameCount` is `0`. |
| `OpenSound` <span class="opt">optional</span> | `string` | `shwip` | Played when the book lands and again when it finishes opening. `null` for silence. |
| `TurnSound` <span class="opt">optional</span> | `string` | `shwip` | Played when a page turn starts, including when `TurnFrameCount` is `0` and there's nothing to see. |
| `CloseSound` <span class="opt">optional</span> | `string?` | — | Played when the book starts closing. |

---

## Layout

Where the page content sits within the book art, in unscaled sprite pixels, so these scale with `Appearance.Scale`. Measure them off your book PNG: find where the paper starts relative to the frame's top-left.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `MarginOuter` <span class="opt">optional</span> | `int` | `12` | The gap between the book frame's left or right edge and the page content. |
| `MarginSpine` <span class="opt">optional</span> | `int` | `6` | The gap between the spine and the page content, on each side. Serves as the right hand margin when `IsSinglePage` is set. |
| `MarginTop` <span class="opt">optional</span> | `int` | `27` | The gap between the book frame's top edge and the page content. |
| `MarginBottom` <span class="opt">optional</span> | `int` | `28` | The gap between the book frame's bottom edge and the page content. |
| `IsSinglePage` <span class="opt">optional</span> | `bool` | `false` | Whether the book shows one page at a time rather than a spread of two. See [Single page books](#single-page-books). |

Together these define each page's content area, which is what every element's width is measured against and what `Fill` fills.

### Single page books

Set `IsSinglePage` for a notepad, a letter, a sign or anything else that isn't a bound book with a spine. The page then runs the whole width of the frame rather than half of it, and each page is a spread of its own, so turning moves one page at a time.

```json title="books.json"
{
  "Id": "you.Notepad_Book",
  "Layout": {
    "IsSinglePage": true,
    "MarginOuter": 10,
    "MarginSpine": 10
  }
}
```

`MarginOuter` is the left hand margin and `MarginSpine` the right, so the two edges stay separately adjustable even though there's no spine between them.

Everything else works as it does for a two page book. Supply notepad art through [`Appearance`](#appearance) with its own `OpenFrameCount` and `TurnFrameCount`, and place both corners of [`PageCurl`](#page-curl) over the one page.

!!! note "There is no right page"
    `PeacefulEnd.Parchment_IsHoveringRightPage` is never true, `TryGetRightPageBounds` on the API returns false, and `PeacefulEnd.Parchment_IsHoveringLeftPage` means the cursor is over the page. Anything that would have run on a right page (triggers, keybinds, frame actions) simply has nothing to run on.

---

## On key press

`OnKeyPress` binds keys to [trigger actions](../concepts/actions.md) on every page of the book and on its shut cover. It's the same schema a page uses, and it's where a bind goes when it should follow the reader through the whole book rather than belong to one spread.

```json title="books.json"
{
  "Id": "you.CampingGuide_Book",
  "Pages": [ ... ],
  "OnKeyPress": [
    {
      "Keybind": "Escape",
      "Actions": [ "PeacefulEnd.Parchment_GoBack" ]
    }
  ]
}
```

--8<-- "keybind-common.md"

### A page's binds win

When the visible spread and the book both bind the same key, only the page's entries run. The book's are left alone rather than running after them, so a page can take a key off the book for as long as it's on screen and hand it back when the reader turns away.

The test is what actually ran, not what merely matched. A page bind whose `Condition` fails hasn't run, so the book's bind still gets its turn. That's the way to author a book-wide default with a page-specific exception: give the page the special case behind a condition and let the book handle everything else.

A page bind on **either** leaf of the spread takes the key, since both are being read.

### Everything else matches a page's binds

| Behaviour | |
| --- | --- |
| **When they fire** | While a spread is on screen and on the shut cover. A book bind is dead through the opening, turning and closing animations, the same as a page's. |
| **How many run** | Every entry whose key matches and whose condition passes, in the order they're listed. |
| **Getting out** | Holding the exit key for three seconds shuts the book and leaves the menu, whatever the book has bound it to. See [On key press](page.md#on-key-press). |

### They fire on the cover too

A book's binds are live while the cover is shut, which is what lets a key open the book or act on it before the reader ever turns a page. No page is on screen there, so nothing can take a key over and the book's entries always run. A [navigating action](../concepts/actions.md#navigating-from-the-cover) bound there opens the book onto the page it names.

A bind that only makes sense inside the book can say so with a [book state](../concepts/conditions.md#book-states) condition:

```json title="books.json"
{
  "Keybind": "Escape",
  "Condition": "PeacefulEnd.Parchment_CurrentBookState Ready",
  "Actions": [ "PeacefulEnd.Parchment_GoBack" ]
}
```

!!! tip "A book-wide back button"
    `Escape` bound to [`PeacefulEnd.Parchment_GoBack`](../concepts/actions.md#going-back) turns the whole book into something the reader unwinds a step at a time, with the hold still there for leaving outright. A page that needs `Escape` for something else simply binds it and the book's entry stands aside.
