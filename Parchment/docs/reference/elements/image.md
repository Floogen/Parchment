# Image

A sprite. It can be a static picture, an animation or an item's icon, and it can have text drawn on top of it: a sign, a plaque, a labelled diagram.

```json
{
  "Type": "Image",
  "TexturePath": "Data/PeacefulEnd_Campgrounds/Campgrounds/Textures/StarterTent",
  "TextureSourceRectangle": { "X": 0, "Y": 5, "Width": 48, "Height": 59 },
  "Scale": 2,
  "Alignment": "Center"
}
```

An image is sized by its sprite: `TextureSourceRectangle` × `Scale`. If that's wider than the space available it's scaled down to fit, with a warning. Text never widens it.

## Image fields

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `ItemId` | string | *none* | A qualified item ID such as `(O)24`, whose icon is drawn. When set, `TexturePath` and `TextureSourceRectangle` are ignored. The item's name and description also fill in `DisplayName` and `Description` automatically, so `ItemId` alone gives you the sprite *and* a vanilla-style tooltip. |
| `Frames` | list of [frames](#frames) | *none* | Animation frames. When omitted, the sprite is static. |
| `FrameDuration` | number | `100` | How long a frame is shown when it doesn't specify its own `Duration`, in milliseconds. |
| `TextArea` | rectangle | *the whole sprite* | Where text is drawn, in unscaled sprite pixels **relative to `TextureSourceRectangle`'s top-left**, not to the texture. This is how you place a label inside a sign's recessed panel. The text block is centred vertically within this area. |
| `TextScale` | number | `1` | The text's scale, independent of `Scale`, which sizes the sprite. |
| `TextAlignment` | `Left` \| `Center` \| `Right` | `Center` | How each line of text is aligned within `TextArea`. Distinct from `Alignment`, which places the whole image on the page. |
| `Rotation` | number | `0` | How much rotation is applied to the texture. Note: Does not affect text! |

### Frames

Each entry in `Frames`:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `SourceRectangle` | rectangle | <span class="req">required</span> | The area of the sprite sheet for this frame. **Must be the same size** as the element's `TextureSourceRectangle`, which is what the layout is measured from. |
| `Duration` | number | *the element's `FrameDuration`* | How long this frame is shown, in milliseconds. |

Frames loop, and the cycle runs off game time, so two identical animations on a page play in lockstep.

!!! important "`TextureSourceRectangle` is required when animating"
    It's the measuring stick: it defines the element's size, while `Frames` defines what's drawn. Without it, the whole sprite sheet becomes the element and your bird is a 128-pixel-wide strip. Frame 0 is usually the same rectangle, and that's fine. The duplication is what keeps the rule simple.

```json
{
  "Type": "Image",
  "TexturePath": "LooseSprites/GemBird",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 32, "Height": 32 },
  "Scale": 3,
  "Alignment": "Center",
  "Frames": [
    { "Duration": 1000, "SourceRectangle": { "X": 0, "Y": 0, "Width": 32, "Height": 32 } },
    { "Duration": 100, "SourceRectangle": { "X": 32, "Y": 0, "Width": 32, "Height": 32 } },
    { "Duration": 100, "SourceRectangle": { "X": 64, "Y": 0, "Width": 32, "Height": 32 } }
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
