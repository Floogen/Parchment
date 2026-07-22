# Book

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

## Fields

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `Format` | string | `1.0.0` | The schema version this book was written against. |
| `Id` | string | <span class="req">required</span> | A [unique string ID](https://stardewvalleywiki.com/Modding:Common_data_field_types#Unique_string_ID) for the book, conventionally `{ModId}_{Name}`. This is how actions and items refer to it, and it's the name Parchment uses in log messages. |
| `SpritePath` | string | *none* | The sprite used for the book's item. |
| `Pages` | list of [pages](page.md) | *empty* | The book's pages, in order. Two consecutive pages make a spread. |
| `Underlay` | list of [elements](elements/index.md) | *none* | Elements drawn **behind** the book sprite, positioned by their `Position` relative to the book's top-left. Negative coordinates place them outside the book's edges. This is how you make a bookmark that sticks out of the side, with the part that overlaps the book hidden behind it. Drawn during the open and close animations, so they ride in with the book. |
| `Overlay` | list of [elements](elements/index.md) | *none* | Elements drawn **in front of** the book sprite and its pages, positioned by their `Position` relative to the book's top-left. Only drawn once the book is open. |
| `Appearance` | [appearance](#appearance) | *the default book* | The book's sprite and animation frames. |
| `PageCurl` | [page curl](#page-curl) | *the default corners* | The corner curl sprite and its clickable areas. |
| `Animation` | [animation](#animation) | *see below* | Animation timings and sounds. |
| `Layout` | [layout](#layout) | *see below* | The page margins. |

!!! note "The book's name and description live on the item"
    A book has no `Title` or `Description` of its own. What the player sees comes from the item that opens it, its `DisplayName` and `Description` in `Data/Objects`. That also means those are localisable through the usual `[LocalizedText ...]` tokens.

## Appearance

Everything about how the book itself is drawn. All defaults describe Parchment's built-in book, so you only need this block if you're making your own book art, and if you are, you'll want to set all of it, since the frame counts and offsets are facts about a specific sprite sheet.

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `TexturePath` | string | *the built-in book* | The book's sprite sheet. Frames run **horizontally**, each `FrameWidth` × `FrameHeight`: first the open frames (index 0 fully closed, the last fully open), then the page-turn frames. The close animation is the open frames played backwards, so you don't supply those separately. |
| `GrayscaleTexturePath` | string | *the built-in book* | An optional greyscale layer drawn beneath `TexturePath` and tinted by the book's `TintColor`. Multiplying a colour into greyscale art gives you that colour. Set to `null` to skip the recoloring. |
| `TintColor` | [color](elements/index.md#colors) | *white* | A colour multiplied into the book's greyscale layer. |
| `FrameWidth` | integer | `219` | The width of one frame, in unscaled sprite pixels. |
| `FrameHeight` | integer | `158` | The height of one frame, in unscaled sprite pixels. |
| `OpenFrameCount` | integer | `4` | How many open frames the sheet starts with. |
| `TurnFrameCount` | integer | `6` | How many page-turn frames follow the open frames. |
| `Scale` | number | `5` | How much the book sprite is magnified. Everything measured against the book art (the page margins, the curl offsets) scales with this. |
| `Offset` | point | `0, 0` | A nudge applied to the book's centred position, in unscaled sprite pixels. The book is centred on its **frame**, so if your frame has empty space around the art it won't look centred. This is how you correct that. |

## Page curl

The corner you click to turn a page. The offsets are relative to the book frame, so they follow the book when it moves or changes scale.

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `TexturePath` | string | *the built-in corner* | The curl sprite sheet. Frames run horizontally: frame 0 is flat, the last is fully curled. Hovering plays forward, un-hovering plays back. |
| `FrameWidth` | integer | `32` | The width of one frame, in unscaled sprite pixels. |
| `FrameHeight` | integer | `32` | The height of one frame, in unscaled sprite pixels. |
| `FrameCount` | integer | `7` | How many frames the curl animation has. |
| `Scale` | number | `5` | How much the curl sprite is magnified. |
| `PreviousPageOffset` | point | `1, 113` | The top-left of the back-turn corner, in unscaled sprite pixels relative to the book frame's top-left. |
| `NextPageOffset` | point | `186, 112` | The top-left of the forward-turn corner, in unscaled sprite pixels relative to the book frame's top-left. |

!!! info "The corner and its hotspot are the same rectangle"
    Each corner's clickable area is exactly the sprite you see: the offset above, sized `FrameWidth` × `FrameHeight` × `Scale`. There's no separate hotspot to keep in sync.

The left corner is drawn mirrored, so one piece of art serves both sides.

## Animation

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `SlideDuration` | number | `350` | How long the closed book takes to slide up from the bottom of the screen, in milliseconds. Eased, so it starts fast and lands softly. |
| `OpenDuration` | number | `250` | How long the open animation takes, in milliseconds. The open frames are spread evenly across it. |
| `CloseDuration` | number | `400` | How long the close animation takes, in milliseconds. |
| `TurnDuration` | number | `500` | How long a page turn takes, in milliseconds. |
| `CurlDuration` | number | `250` | How long the corner curl takes to play through all its frames, in milliseconds. |
| `ContentSwapProgress` | number | `0.5` | The point in a page turn at which the page content changes over, from 0 to 1. Tune this so the swap happens while the turning page hides it. |
| `OpenSound` | string | `shwip` | Played when the book lands and again when it finishes opening. `null` for silence. |
| `TurnSound` | string | `shwip` | Played when a page turn starts. |
| `CloseSound` | string | *none* | Played when the book starts closing. |

## Layout

Where the page content sits within the book art, in unscaled sprite pixels, so these scale with `Appearance.Scale`. Measure them off your book PNG: find where the paper starts relative to the frame's top-left.

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `MarginOuter` | integer | `12` | The gap between the book frame's left or right edge and the page content. |
| `MarginSpine` | integer | `6` | The gap between the spine and the page content, on each side. |
| `MarginTop` | integer | `27` | The gap between the book frame's top edge and the page content. |
| `MarginBottom` | integer | `28` | The gap between the book frame's bottom edge and the page content. |

Together these define each page's content area, which is what every element's width is measured against and what `Fill` fills.
