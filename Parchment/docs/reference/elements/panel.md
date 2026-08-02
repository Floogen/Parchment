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
| `Background` <span class="opt">optional</span> | list of [`elements`](index.md) | — | Elements drawn behind `Children`, placed at their own `Position` inside the panel rather than stacked. These never change the panel's size. |
| `Foreground` <span class="opt">optional</span> | list of [`elements`](index.md) | — | Elements drawn over `Children`, placed the same way. |
| `Padding` <span class="opt">optional</span> | `int` | `0` | Extra space between the frame's **inner edge** and the children, in unscaled sprite pixels × `Scale`. `0` means flush against the inside of the border. You don't need to account for the border's thickness yourself. |
| `Sizing` <span class="opt">optional</span> | [`sizing mode`](index.md#sizing-modes) | `Fill` | How wide the panel is. `ShrinkToFit` hugs the widest child. |
| `Width` <span class="opt">optional</span> | `int?` | — | The **content** width in unscaled sprite pixels × `Scale`. The border and padding are added around it. Required when `Sizing` is `Fixed`. |
| `Height` <span class="opt">optional</span> | `int?` | — | The content height in unscaled sprite pixels × `Scale`. When omitted, the panel is as tall as its children need. When set, it's exactly this tall and children that would stack past it are dropped. Independent of `Sizing`, which only controls width. |

## Background and Foreground

A panel takes its own placed layers, working the same way a page's [`Background` and `Foreground`](../page.md) do. `Children` stack, while these two sit wherever their `Position` puts them:

```json
{
  "Type": "Panel",
  "TexturePath": "{{ModId}}/panelFrame2",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 24, "Height": 24 },
  "Scale": 4,
  "Padding": 4,
  "Background": [
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/watermark",
      "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 32, "Height": 32 },
      "Alignment": "Center",
      "VerticalAlignment": "Middle"
    }
  ],
  "Children": [
    { "Type": "Paragraph", "Text": "Pitch on level ground.", "Alignment": "Center" }
  ],
  "Foreground": [
    {
      "Type": "Image",
      "TexturePath": "{{ModId}}/seal",
      "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
      "Alignment": "Right",
      "VerticalAlignment": "Bottom"
    }
  ]
}
```

Draw order is `Background`, then `Children`, then `Foreground`.

Both layers are anchored to the panel's **content area**, the same rectangle the children occupy, so the border and `Padding` inset them exactly as they inset a child. `Alignment` and `VerticalAlignment` anchor the element within that area first and `Position` is then an offset from that anchor, matching how a page's layers behave.

!!! warning "A placed layer can't size the panel"
    A `ShrinkToFit` panel hugs its widest **child**, and a panel without `Height` is as tall as its **children** need. Neither layer contributes, since the panel has to know its own size before it can place anything inside it. A background wider than the panel is simply clipped by nothing (it draws past the frame). Size the panel with `Width` and `Height` when the layer is what matters.

Placed elements are only reachable by the cursor when they have something to offer, such as a `Description`, `DisplayName` or an `Action`. Purely decorative art in either layer passes the cursor through to the children beneath it, which holds regardless of whether the panel itself is in a page's stacked `Elements` or in one of its layers.

The panel itself is reachable in the stacked list whatever it holds, so a panel with no tooltip of its own still claims the cursor over its own padding. Set [`IgnoreCursor`](../page.md#passing-the-cursor-through) on it to hand that back to whatever is drawn beneath, without affecting the children.

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
