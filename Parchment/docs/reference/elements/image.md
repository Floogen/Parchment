# Image

A sprite. It can be a static picture, an animation or an item's icon, and it can have text drawn on top of it: a sign, a plaque, a labelled diagram.

```json
{
  "Type": "Image",
  "TexturePath": "Data/PeacefulEnd.Campgrounds/Campgrounds/Textures/StarterTent",
  "TextureSourceRectangle": { "X": 0, "Y": 5, "Width": 48, "Height": 59 },
  "Scale": 2,
  "Alignment": "Center"
}
```

An image is sized by its sprite: `TextureSourceRectangle` × `Scale`. If that's wider than the space available it's scaled down to fit, with a warning. Text never widens it.

## Image fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ItemId` <span class="opt">optional</span> | `string` | — | A qualified item ID such as `(O)24`, whose icon is drawn. When set, `TexturePath` and `TextureSourceRectangle` are ignored. The item's name and description also fill in `DisplayName` and `Description` automatically, so `ItemId` alone gives you the sprite *and* a vanilla-style tooltip. |
| `Frames` <span class="opt">optional</span> | list of [`frames`](#frames) | — | Animation frames. When omitted, the sprite is static. |
| `FrameDuration` <span class="opt">optional</span> | `number` | `100` | How long a frame is shown when it doesn't specify its own `Duration`, in milliseconds. |
| `TextArea` <span class="opt">optional</span> | `Rectangle` | *the whole sprite* | Where text is drawn, in unscaled sprite pixels **relative to `TextureSourceRectangle`'s top-left**, not to the texture. This is how you place a label inside a sign's recessed panel. The text block is centred vertically within this area. |
| `TextScale` <span class="opt">optional</span> | `number` | `1` | The text's scale, independent of `Scale`, which sizes the sprite. |
| `TextAlignment` <span class="opt">optional</span> | `Left` \| `Center` \| `Right` | `Center` | How each line of text is aligned within `TextArea`. Distinct from `Alignment`, which places the whole image on the page. |
| `Rotation` <span class="opt">optional</span> | `number` | `0` | How much rotation is applied to the texture. Note: does not affect text! |
| `Origin` <span class="opt">optional</span> | `Vector2` | `{ X: 0.0, Y: 0.0 }` | The pivot point the sprite rotates and scales around. Note: does not affect text! |

### Frames

Each entry in `Frames`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `SourcePoint` <span class="req">required</span> | `Point` | — | The coordinate of the sprite for this frame. Automatically inherits the element's `TextureSourceRectangle` for height and width. |
| `Duration` <span class="opt">optional</span> | `number` | *the element's `FrameDuration`* | How long this frame is shown in milliseconds. |
| `Scale` <span class="opt">optional</span> | `number` | `1` | A multiplier on the element's `Scale` while this frame draws. See [Frame scale](#frame-scale). |
| `Condition` <span class="opt">optional</span> | `string` | — | A [game state query](../../concepts/conditions.md) deciding whether this frame plays. When omitted the frame always plays. |

Frames loop, and the cycle runs off game time, so two identical animations on a page play in lockstep.

A frame whose `Condition` fails is **skipped**, not paused on. The cycle gets shorter and the remaining frames close the gap, the same way a hidden element lets the ones below it close up. Conditions are re-checked while the book is open, so an animation can gain and lose frames as the game state changes.

When *every* frame's condition fails, the element falls back to drawing `TextureSourceRectangle` on its own. An animation that's entirely conditional therefore goes still rather than disappearing.

!!! warning "`TextureSourceRectangle` is required when animating"
    It's the measuring stick: it defines the element's size, while `Frames` defines what's drawn. Without it, the whole sprite sheet becomes the element.

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

It grows from `Origin`, which defaults to the sprite's top-left corner, so a scaled frame spreads right and down unless you move the pivot.

A pulse needs no extra art at all, just the same cell drawn bigger for a moment:

```json
{
  "Type": "Image",
  "TexturePath": "{{ModId}}/pulse",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
  "Scale": 4,
  "SpacingAfter": 24,
  "Frames": [
    { "Duration": 700, "SourcePoint": { "X": 0, "Y": 0 } },
    { "Duration": 120, "SourcePoint": { "X": 0, "Y": 0 }, "Scale": 1.15 },
    { "Duration": 200, "SourcePoint": { "X": 0, "Y": 0 } }
  ]
}
```

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
