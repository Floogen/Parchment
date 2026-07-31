# Image

A sprite. It can be a static picture, an animation or an item's icon, and it can have text drawn on top of it: a sign, a plaque, a labelled diagram.

```json
{
  "Type": "Image",
  "TexturePath": "LooseSprites/Cursors_1_6",
  "TextureSourceRectangle": { "X": 0, "Y": 192, "Width": 48, "Height": 64 },
  "Scale": 2,
  "Alignment": "Center"
}
```

An image is sized by its sprite: `TextureSourceRectangle` × `Scale`. If that's wider than the space available it's scaled down to fit, with a warning. Text never widens it.

## Image fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ItemId` <span class="opt">optional</span> | `string` | — | A qualified item ID such as `(O)24`, whose icon is drawn. When set, `TexturePath` and `TextureSourceRectangle` are ignored. The item's name and description also fill in `DisplayName` and `Description` automatically, so `ItemId` alone gives you the sprite *and* a vanilla-style tooltip. It can still be animated: see [Animating an item](#animating-an-item). |
| `Frames` <span class="opt">optional</span> | list of [`frames`](#frames) | — | Animation frames. When omitted, the sprite is static. |
| `HoverFrames` <span class="opt">optional</span> | list of [`frames`](#frames) | — | Animation frames played while the cursor is over the element, replacing `Frames` for as long as it stays there. See [Hover frames](#hover-frames). |
| `FrameDuration` <span class="opt">optional</span> | `number` | `100` | How long a frame is shown when it doesn't specify its own `Duration`, in milliseconds. |
| `TextArea` <span class="opt">optional</span> | `Rectangle` | *the whole sprite* | Where text is drawn, in unscaled sprite pixels **relative to `TextureSourceRectangle`'s top-left**, not to the texture. This is how you place a label inside a sign's recessed panel. The text block is centred vertically within this area. |
| `TextScale` <span class="opt">optional</span> | `number` | `1` | The text's scale, independent of `Scale`, which sizes the sprite. |
| `TextAlignment` <span class="opt">optional</span> | `Left` \| `Center` \| `Right` | `Center` | How each line of text is aligned within `TextArea`. Distinct from `Alignment`, which places the whole image on the page. |
| `Rotation` <span class="opt">optional</span> | `number` | `0` | How much rotation is applied to the texture. Note: does not affect text! |
| `Origin` <span class="opt">optional</span> | `Vector2` | `{ X: 0.0, Y: 0.0 }` | The pivot point the sprite rotates and scales around, in unscaled sprite pixels relative to `TextureSourceRectangle`'s top-left. It changes what the sprite turns and grows about, never where it rests, so a still sprite at its own `Scale` looks identical at any value. Note: does not affect text! |

### Frames

Each entry in `Frames`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `SourcePoint` <span class="opt">optional</span> | `Point` | *the element's own sprite* | The coordinate of the sprite for this frame. Automatically inherits the element's `TextureSourceRectangle` for height and width. Omit it and the frame draws whatever the element already draws, which is how you vary only `Duration`, `Scale` or `Condition`. |
| `Duration` <span class="opt">optional</span> | `number` | *the element's `FrameDuration`* | How long this frame is shown in milliseconds. |
| `Scale` <span class="opt">optional</span> | `number` | `1` | A multiplier on the element's `Scale` while this frame draws. See [Frame scale](#frame-scale). |
| `Offset` <span class="opt">optional</span> | `Point` | `{ X: 0, Y: 0 }` | How far this frame is shifted from where the element sits, in unscaled sprite pixels × `Scale`. Positive moves right and down. See [Frame offset](#frame-offset). |
| `Action` <span class="opt">optional</span> | `string` | — | A [trigger action](../../concepts/actions.md) run each time this frame starts. See [Frame actions](#frame-actions). |
| `Actions` <span class="opt">optional</span> | list of `string` | — | Trigger actions run in order each time this frame starts. Combined with `Action` rather than replacing it. |
| `Condition` <span class="opt">optional</span> | `string` | — | A [game state query](../../concepts/conditions.md) deciding whether this frame plays. When omitted the frame always plays. |

Frames loop, and the cycle is timed from the moment the animation starts, so the first frame is the one that draws when it does.

A frame whose `Condition` fails is **skipped**, not paused on. The cycle gets shorter and the remaining frames close the gap, the same way a hidden element lets the ones below it close up. Conditions are re-checked while the book is open, so an animation can gain and lose frames as the game state changes.

**Gaining or losing a frame starts the animation over.** A cycle whose frame list changed isn't the cycle that was playing, so it restarts rather than resuming partway. That's what lets an animation gated behind a condition play properly: gate every frame on `PeacefulEnd.Parchment_CurrentPageId <your page>` and the whole thing plays from the top when the reader arrives, instead of catching it mid-cycle.

!!! tip "Timing a pause into a loop"
    Since the animation restarts when it becomes active, a long final frame reads as a delay before the next repeat. A ten-frame flourish followed by a frame of `60000` plays once on arrival then holds still for a minute, over and over, without needing anything to trigger it.

When *every* frame's condition fails, the element falls back to drawing `TextureSourceRectangle` on its own. An animation that's entirely conditional therefore goes still rather than disappearing.

!!! warning "`TextureSourceRectangle` is required when animating"
    It's the measuring stick: it defines the element's size, while `Frames` defines what's drawn. Without it, the whole sprite sheet becomes the element. The one exception is `ItemId`, which brings a measuring stick of its own.

!!! tip "Point it at a frame you'd be happy to see"
    Because it's the fallback, `TextureSourceRectangle` should be a sprite that stands on its own. Aim it at a blank cell and a fully conditional animation renders as nothing.

```json
{
  "Type": "Image",
  "TexturePath": "LooseSprites/GemBird",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 32, "Height": 32 },
  "Scale": 3,
  "Alignment": "Center",
  "Frames": [
    { "Duration": 1000, "SourcePoint": { "X": 0, "Y": 0 } },
    { "Duration": 100, "SourcePoint": { "X": 32, "Y": 0 } },
    { "Duration": 100, "SourcePoint": { "X": 64, "Y": 0 } }
  ]
}
```

A candle that only flickers after dark. Both frames drop out during the day, leaving the unlit sprite the source rectangle points at:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/candle",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
  "Scale": 4,
  "Frames": [
    { "SourcePoint": { "X": 16, "Y": 0 }, "Condition": "TIME 1800 2600" },
    { "SourcePoint": { "X": 32, "Y": 0 }, "Condition": "TIME 1800 2600" }
  ]
}
```

### Frame scale

`Scale` on a frame is the one thing that changes a sprite's size mid-animation. The element is measured once, at `TextureSourceRectangle` × the element's own `Scale`, and that measurement is what reserves space on the page and what the cursor is tested against. A frame at `1.2` draws twenty percent larger over the top of that reserved space rather than pushing the elements below it down.

It grows from `Origin`, which defaults to the sprite's top-left corner, so a scaled frame spreads right and down unless you move the pivot. Put `Origin` in the middle of the source rectangle (`8, 8` for a 16×16 sprite) and the frame grows evenly in every direction instead. The pivot itself doesn't move as the frame scales, so a pulse stays put rather than creeping across the page.

A pulse needs no extra art at all, just the same cell drawn bigger for a moment. None of these frames moves anywhere in the sheet, so all three leave `SourcePoint` out:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/pulse",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
  "Scale": 4,
  "Origin": { "X": 8, "Y": 8 },
  "SpacingAfter": 24,
  "Frames": [
    { "Duration": 700 },
    { "Duration": 120, "Scale": 1.15 },
    { "Duration": 200 }
  ]
}
```

### Frame offset

`Offset` moves what a frame draws without moving where the element lives. Like [frame scale](#frame-scale), the element is measured once and keeps that space and that hitbox, so an offset frame slides over its own bounds rather than pushing the elements below it around or dragging its clickable area along.

Two or three frames are enough for a bob, and none of them needs new art:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/lantern",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
  "Scale": 4,
  "Frames": [
    { "Duration": 500 },
    { "Duration": 500, "Offset": { "X": 0, "Y": -1 } }
  ]
}
```

At `Scale: 4` that single unscaled pixel is four screen pixels, since `Offset` is a measurement on the sprite rather than a coordinate on the page. That's the opposite of [`Position`](../../concepts/layout.md#placed-elements), which is a coordinate and deliberately doesn't scale.

Paired with [hover frames](#hover-frames), one offset frame gives you art that lifts under the cursor and settles when it leaves:

```json
"HoverFrames": [
  { "Offset": { "X": 0, "Y": -2 } }
]
```

!!! note "`Offset` carries the text, `Scale` doesn't"
    A frame's `Scale` leaves any [text on the image](#text-fields) at its own size, since scaling reads as emphasis on the art. An offset moves the whole element, text included, because a label left standing where a sprite used to be reads as a bug rather than as an effect.

Offsets are rounded to whole screen pixels. A still sprite sits happily on a fractional position, but one that moves every tick shimmers there, so the rounding is deliberate rather than incidental.

### Frame actions

A frame can run [trigger actions](../../concepts/actions.md) at the moment it starts. Actions are dispatched every tick, so they keep time with the animation rather than with the slower interval conditions are checked on.

!!! danger "They run on every cycle, forever"
    A three-frame loop with an action on the middle frame runs it several times a second for as long as the page is open. Nothing rate-limits this. Either keep the actions harmless to repeat, the way [hover actions](index.md#common-fields) have to be, or condition the frames so the loop stops or gets skipped.

### Playing an animation once

There's no `PlayOnce` field, because the pieces already here compose into one. The last frame sets a flag, every frame is conditioned on that flag being unset, and the animation drops out rather than looping:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/seal",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
  "Scale": 4,
  "FrameDuration": 120,
  "Frames": [
    { "SourcePoint": { "X": 16, "Y": 0 }, "Condition": "!PeacefulEnd.Parchment_HasInputText sealPlayed" },
    { "SourcePoint": { "X": 32, "Y": 0 }, "Condition": "!PeacefulEnd.Parchment_HasInputText sealPlayed" },
    { "SourcePoint": { "X": 48, "Y": 0 }, "Condition": "!PeacefulEnd.Parchment_HasInputText sealPlayed", "Action": "PeacefulEnd.Parchment_SetInput sealPlayed 1" }
  ]
}
```

Two things make this work, and both are easy to get wrong:

**`TextureSourceRectangle` is what's left when it ends.** Conditioning every frame out doesn't hold the last frame, it falls back to the element's own source rectangle. Point that at the resting pose and the animation plays once and settles. Point it anywhere else and the sprite changes into something unrelated the moment the flourish finishes.

**The flag has to outlive the frame, not the save.** The example uses an [`Input`](input.md) as the store, since that's cleared when the book closes, so the animation plays again next time the reader opens it. A mail flag would make it play once ever, on any save.

!!! note "The last frame is cut short"
    Actions fire as a frame *starts*, and the flag conditions the frames out within the same tick. The final frame therefore never gets its full `Duration`. It doesn't show, because what replaces it is the fallback sprite, but it's why you shouldn't put the pose you want to end on in the last frame rather than in `TextureSourceRectangle`.

### Animating an item

`ItemId` animates the same way, with one difference: the item's own icon is the measuring stick that `TextureSourceRectangle` usually is, so you don't need one. Leave `SourcePoint` off every frame and the item's sprite is what each frame draws, leaving `Duration`, `Scale` and `Condition` to do the work.

A parsnip that gives a little pulse, and a bigger one on hover:

```json
{
  "Type": "Image",
  "ItemId": "(O)24",
  "Scale": 4,
  "Origin": { "X": 8, "Y": 8 },
  "Alignment": "Center",
  "Frames": [
    { "Duration": 900 },
    { "Duration": 150, "Scale": 1.1 },
    { "Duration": 250 }
  ],
  "HoverFrames": [
    { "Duration": 150, "Scale": 1.2 },
    { "Duration": 150, "Scale": 1.05 }
  ]
}
```

Set `Origin` to the middle of the item's sprite (`8, 8`, since item icons are 16×16) so it grows in every direction rather than down and to the right.

!!! note "A `SourcePoint` on an item frame is measured in its sheet"
    Nothing stops you giving one, and it's read as a coordinate in whichever sheet the item lives in, such as `Maps/springobjects`. Beware that an item can move within its sheet between game versions and a modded item's sheet isn't yours at all. Use `TexturePath` when you want to pick sprites yourself.

### Hover frames

`HoverFrames` is a second frame list that takes over while the cursor is on the element. It's the animated counterpart to [`HoverTextureSourceRectangle`](#sprite-fields), which swaps a single still.

Both lists are sized by `TextureSourceRectangle`, or by the item's icon when `ItemId` is used, so this changes what's drawn and never the element's layout. Everything a frame understands works in either list: `Duration`, `Condition` and `Scale` all behave the same, and both lists share the element's `FrameDuration` as their default.

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/lantern",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
  "Scale": 4,
  "DisplayName": "Lantern",
  "Frames": [
    { "Duration": 600, "SourcePoint": { "X": 0, "Y": 0 } },
    { "Duration": 600, "SourcePoint": { "X": 16, "Y": 0 } }
  ],
  "HoverFrames": [
    { "Duration": 120, "SourcePoint": { "X": 0, "Y": 16 } },
    { "Duration": 120, "SourcePoint": { "X": 16, "Y": 16 } }
  ]
}
```

Leaving `HoverFrames` out means the normal animation simply keeps playing under the cursor, which is the behaviour every existing book already has.

**An empty hover animation falls back rather than freezing.** If every frame in `HoverFrames` is conditioned out, the element carries on with `Frames` instead of dropping to a still. The order of preference is `HoverFrames`, then `Frames`, then `TextureSourceRectangle`, so a hover animation can come and go with the game state without interrupting the idle loop.

**`HoverFrames` alone makes an element hoverable.** In a page's `Background` or `Foreground`, an element with nothing else to offer is [transparent to the cursor](../page.md#background-and-foreground). A hover animation counts as something to offer, the same way a `HoverTextureSourceRectangle` does, so it will be reachable without needing a tooltip or an action.

**Both animations restart on the swap.** The hover animation plays from its first frame when the cursor arrives, and the normal animation plays from its first frame when the cursor leaves. Each is a fresh cycle rather than one picked up wherever the other left it, so a one-shot reveal on hover works as written.

!!! note "Elements without `HoverFrames` are untouched"
    The restart only happens when a hover animation actually took over. An element whose `Frames` keep playing under the cursor never stops, so it never jumps back to its first frame when the cursor moves away.

## Text fields

Optional: leave `Text` out for a plain picture.

--8<-- "text-content.md"

## Sprite fields

--8<-- "sprite.md"

## Common fields

`Scale` on an `Image` is the **sprite** scale. Use `TextScale` for the text.

--8<-- "element-common.md"

!!! tip "Watch for transparent padding"
    An image's size is its source rectangle, not the pixels painted in it. If your sprite has empty rows at the top or bottom of its rectangle, the element reserves space for them and the picture looks oddly offset. Tighten the rectangle to the art. See [Preparing your art](../../concepts/art.md#tighten-your-source-rectangles).
