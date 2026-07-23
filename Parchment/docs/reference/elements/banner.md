# Banner

A three-sliced strip with centred text: a scroll, a ribbon, a plaque. The left and right caps are fixed art and the middle stretches, so a banner grows sideways to fit its text but never vertically.

```json
{
  "Type": "Banner",
  "Text": "Chapter 2",
  "FontType": "Small",
  "CapWidth": 19,
  "TexturePath": "Assets/PeacefulEnd.Parchment/bannerTitle1",
  "Sizing": "ShrinkToFit",
  "Alignment": "Center",
  "Scale": 5
}
```

A banner's height is always its source rectangle's height × `Scale`. If you need something that grows in both directions, use a [`Panel`](panel.md) with a text child. That's what nine-slicing is for.

## Banner fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `CapWidth` <span class="opt">optional</span> | `int` | *a third of the strip* | The width of the left and right caps, in unscaled sprite pixels. Everything between them is the middle segment. |
| `Padding` <span class="opt">optional</span> | `int` | `0` | Extra space between the caps and the text, in unscaled sprite pixels × `Scale`. |
| `Sizing` <span class="opt">optional</span> | [`sizing mode`](index.md#sizing-modes) | `ShrinkToFit` | How wide the banner is. `ShrinkToFit` hugs the text. |
| `Width` <span class="opt">optional</span> | `int?` | — | The **content** width in unscaled sprite pixels × `Scale`. The caps and padding are added around it. Required when `Sizing` is `Fixed`. |
| `TextScale` <span class="opt">optional</span> | `number` | `1` | The text's scale, independent of `Scale`, which sizes the sprite. |
| `TextOffset` <span class="opt">optional</span> | `Point` | `{ X: 0, Y: 0 }` | An offset applied to the text, from the origin point in the center of the banner. |

!!! tip "Make the middle segment one pixel wide"
    The middle stretches to whatever width the banner ends up. A one-pixel middle stretches perfectly to any width. A wider patterned one gives uneven columns. A 49-pixel strip with `CapWidth: 24` is the shape you want, not equal thirds, which is only the default because it's the least surprising reading of "three segments". See [Preparing your art](../../concepts/art.md#three-slice-banners).

## Text fields

Text is always centred in the middle segment, whatever the banner's own `Alignment` is. A right-aligned banner still has centred text.

--8<-- "text-content.md"

## Sprite fields

--8<-- "sprite.md"

## Common fields

`Scale` on a `Banner` is the **sprite** scale. Use `TextScale` for the text. Keep `Scale` a whole number. Fractional scaling distorts the cap art.

--8<-- "element-common.md"

!!! warning "Text taller than the banner will overflow"
    A short strip can't hold a large font. If the text spills past the scroll, lower `TextScale` or pick a smaller `FontType`. Parchment logs a warning telling you both numbers.
