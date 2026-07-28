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
| `Pages` <span class="opt">optional</span> | list of [`pages`](page.md) | empty list | The book's pages, in order. Two consecutive pages make a spread. |
| `Underlay` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **behind** the book sprite, positioned by their `Position` relative to the book's top-left. Negative coordinates place them outside the book's edges. This is how you make a bookmark that sticks out of the side, with the part that overlaps the book hidden behind it. Drawn during the open and close animations, so they ride in with the book. |
| `Overlay` <span class="opt">optional</span> | list of [`elements`](elements/index.md) | empty list | Elements drawn **in front of** the book sprite and its pages, positioned by their `Position` relative to the book's top-left. Only drawn once the book is open, and on the shut cover during [cover view](#cover-view). |
| `Appearance` <span class="opt">optional</span> | [`Appearance`](#appearance) | *the default book* | The book's sprite and animation frames. |
| `PageCurl` <span class="opt">optional</span> | [`PageCurl`](#page-curl) | *the default corners* | The corner curl sprite and its clickable areas. |
| `Animation` <span class="opt">optional</span> | [`Animation`](#animation) | *see below* | Animation timings and sounds. |
| `Layout` <span class="opt">optional</span> | [`Layout`](#layout) | *see below* | The page margins. |
| `StartOnCover` <span class="opt">optional</span> | `boolean` | `false` | Whether the book arrives shut and holds on its cover until the reader clicks it open. See [Cover view](#cover-view). |
| `ExitToCover` <span class="opt">optional</span> | `boolean` | `false` | Whether closing the book shuts it in place first, leaving its cover on screen, rather than leaving the menu. See [Cover view](#cover-view). |

!!! note "Underlay and overlay elements always take the cursor"
    Unlike a page's [`Background` and `Foreground`](page.md#background-and-foreground), a decorative element here is hit-tested whether or not it has a tooltip or an action, and the overlay is tested before the pages. Keep overlay art to the area it actually covers rather than stretching a transparent sheet over the whole book.

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

!!! warning "`OnView` and the cover"
    A page's [`OnView`](page.md#on-view) triggers run when it's actually on screen, so with `StartOnCover` the first page's triggers wait until the reader opens the book rather than firing as it arrives. Reopening from the cover re-runs them, the same as turning back to a page would, so gate anything that shouldn't repeat on `HasSeenPageId`.

## Appearance

Everything about how the book itself is drawn. All defaults describe Parchment's built-in book, so you only need this block if you're making your own book art, and if you are, you'll want to set all of it, since the frame counts and offsets are facts about a specific sprite sheet.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `TexturePath` <span class="opt">optional</span> | `string` | *the built-in book* | The book's sprite sheet. Frames run **horizontally**, each `FrameWidth` × `FrameHeight`: first the open frames (index 0 fully closed, the last fully open), then the page-turn frames. The close animation is the open frames played backwards, so you don't supply those separately. |
| `GrayscaleTexturePath` <span class="opt">optional</span> | `string?` | *the built-in book* | An optional greyscale layer drawn beneath `TexturePath` and tinted by the book's `TintColor`. Multiplying a colour into greyscale art gives you that colour. Set to `null` to skip the recoloring. |
| `TintColor` <span class="opt">optional</span> | [`color`](elements/index.md#colors) | *white* | A colour multiplied into the book's greyscale layer. |
| `FrameWidth` <span class="opt">optional</span> | `int` | `219` | The width of one frame, in unscaled sprite pixels. |
| `FrameHeight` <span class="opt">optional</span> | `int` | `158` | The height of one frame, in unscaled sprite pixels. |
| `OpenFrameCount` <span class="opt">optional</span> | `int` | `4` | How many open frames the sheet starts with. |
| `TurnFrameCount` <span class="opt">optional</span> | `int` | `6` | How many page-turn frames follow the open frames. |
| `Scale` <span class="opt">optional</span> | `number` | `5` | How much the book sprite is magnified. Everything measured against the book art (the page margins, the curl offsets) scales with this. |
| `Offset` <span class="opt">optional</span> | `Point` | `{ X: 0, Y: 0 }` | A nudge applied to the book's centred position, in unscaled sprite pixels. The book is centred on its **frame**, so if your frame has empty space around the art it won't look centred. |

---

## Page curl

The corner you click to turn a page. The offsets are relative to the book frame, so they follow the book when it moves or changes scale.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
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

---

## Animation

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `SlideDuration` <span class="opt">optional</span> | `number` | `350` | How long the closed book takes to slide up from the bottom of the screen, in milliseconds. Eased, so it starts fast and lands softly. |
| `OpenDuration` <span class="opt">optional</span> | `number` | `250` | How long the open animation takes, in milliseconds. The open frames are spread evenly across it. |
| `CloseDuration` <span class="opt">optional</span> | `number` | `400` | How long the close animation takes, in milliseconds. |
| `TurnDuration` <span class="opt">optional</span> | `number` | `500` | How long a page turn takes, in milliseconds. |
| `CurlDuration` <span class="opt">optional</span> | `number` | `250` | How long the corner curl takes to play through all its frames, in milliseconds. |
| `ContentSwapProgress` <span class="opt">optional</span> | `number` | `0.5` | The point in a page turn at which the page content changes over, from 0 to 1. Tune this so the swap happens while the turning page hides it. |
| `OpenSound` <span class="opt">optional</span> | `string` | `shwip` | Played when the book lands and again when it finishes opening. `null` for silence. |
| `TurnSound` <span class="opt">optional</span> | `string` | `shwip` | Played when a page turn starts. |
| `CloseSound` <span class="opt">optional</span> | `string?` | — | Played when the book starts closing. |

---

## Layout

Where the page content sits within the book art, in unscaled sprite pixels, so these scale with `Appearance.Scale`. Measure them off your book PNG: find where the paper starts relative to the frame's top-left.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `MarginOuter` <span class="opt">optional</span> | `int` | `12` | The gap between the book frame's left or right edge and the page content. |
| `MarginSpine` <span class="opt">optional</span> | `int` | `6` | The gap between the spine and the page content, on each side. |
| `MarginTop` <span class="opt">optional</span> | `int` | `27` | The gap between the book frame's top edge and the page content. |
| `MarginBottom` <span class="opt">optional</span> | `int` | `28` | The gap between the book frame's bottom edge and the page content. |

Together these define each page's content area, which is what every element's width is measured against and what `Fill` fills.
