# Button

A nine-sliced frame with a label, for running an [action](../../concepts/actions.md). Shorthand for the most common interactive element, but any element can have an `Action`, so use an [`Image`](image.md) when you want a bookmark or a tab rather than a labelled button.

```json
{
  "Type": "Button",
  "TexturePath": "Assets/PeacefulEnd.Parchment/button1",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "HoverTextureSourceRectangle": { "X": 0, "Y": 18, "Width": 18, "Height": 18 },
  "Text": "To the start!",
  "FontType": "Small",
  "Sizing": "ShrinkToFit",
  "Alignment": "Center",
  "Scale": 2,
  "Padding": 2,
  "Action": "PeacefulEnd.Parchment_GoToStart"
}
```

`Action` or [`Actions`](../../concepts/actions.md#running-more-than-one-action) is required. A button that does nothing is an authoring mistake, and Parchment will skip it with a warning.

## Button fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Padding` <span class="opt">optional</span> | `int` | `0` | Extra space between the frame's inner edge and the label, in unscaled sprite pixels × `Scale`. `0` means flush against the inside of the border. |
| `Sizing` <span class="opt">optional</span> | [`sizing mode`](index.md#sizing-modes) | `ShrinkToFit` | How wide the button is. `ShrinkToFit` hugs the label. |
| `Width` <span class="opt">optional</span> | `int?` | — | The **content** width in unscaled sprite pixels × `Scale`. The border and padding are added around it. Required when `Sizing` is `Fixed`. |
| `TextScale` <span class="opt">optional</span> | `number` | `1` | The label's scale, independent of `Scale`, which sizes the frame. |

## Text fields

The label is always centred in the button. `Small` is usually the right `FontType`. `SpriteText` at `TextScale: 1` makes for an enormous button.

--8<-- "text-content.md"

## Sprite fields

`HoverTextureSourceRectangle` is what gives the button a pressed or highlighted state. Put the hover art directly beneath the normal art in the sheet and the two rectangles differ only in `Y`.

The texture must nine-slice: the border is a third of the shorter side. A 16×16 button gives 5-pixel corners and a 6×6 middle. Keep the visible frame inside those 5 pixels and the middle flat. See [Preparing your art](../../concepts/art.md#nine-slice-frames).

--8<-- "sprite.md"

## Common fields

`Scale` on a `Button` is the **frame** scale. Use `TextScale` for the label. The two are separate so you can thicken the border without growing the text.

--8<-- "element-common.md"
