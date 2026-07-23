# Panel

A nine-sliced frame containing other elements. Use it for callouts, boxed asides or anything that needs a border around a group of content.

```json
{
  "Type": "Panel",
  "TexturePath": "Assets/PeacefulEnd.Parchment/panelFrame2",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 24, "Height": 24 },
  "Sizing": "Fill",
  "Alignment": "Center",
  "Scale": 4,
  "Children": [
    { "Type": "Paragraph", "Text": "Pitch on level ground.", "Alignment": "Center" }
  ]
}
```

Children stack inside the panel exactly as they do on a page, and the panel is as tall as they need unless you set `Height`.

## Panel fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Children` <span class="opt">optional</span> | list of [`elements`](index.md) | empty list | The panel's contents, stacked top to bottom. Panels can contain panels. |
| `Padding` <span class="opt">optional</span> | `int` | `0` | Extra space between the frame's **inner edge** and the children, in unscaled sprite pixels × `Scale`. `0` means flush against the inside of the border. You don't need to account for the border's thickness yourself. |
| `Sizing` <span class="opt">optional</span> | [`sizing mode`](index.md#sizing-modes) | `Fill` | How wide the panel is. `ShrinkToFit` hugs the widest child. |
| `Width` <span class="opt">optional</span> | `int?` | — | The **content** width in unscaled sprite pixels × `Scale`. The border and padding are added around it. Required when `Sizing` is `Fixed`. |
| `Height` <span class="opt">optional</span> | `int?` | — | The content height in unscaled sprite pixels × `Scale`. When omitted, the panel is as tall as its children need. When set, it's exactly this tall and children that would stack past it are dropped. Independent of `Sizing`, which only controls width. |

## Sprite fields

`TexturePath` is optional. A panel without one is an invisible container, which is a fine way to group and indent content.

The texture **must nine-slice**: the border is a third of the shorter side, and the middle stretches. For a 24×24 frame that's 8-pixel corners and an 8×8 middle. Keep the visible border inside those corners and the middle flat, or the stretch will smear whatever detail crosses the boundary (see [Preparing your art](../../concepts/art.md#nine-slice-frames)).

`SpriteEffects` is ignored. A nine-sliced frame has no meaningful flip.

--8<-- "sprite.md"

## Common fields

`Scale` on a `Panel` is the **sprite** scale, and it drives the border's thickness: a 24×24 frame at `Scale: 4` has a 32-pixel border. To thicken the frame without inflating the panel, raise `Scale` and lower `Padding`.

--8<-- "element-common.md"

!!! note "Children are measured against the panel, not the page"
    A narrow panel's children wrap at the panel's inner width, and their `Alignment` is resolved within the panel. A centred heading inside a left-aligned narrow panel centres within the panel, which is correct, if briefly surprising.
