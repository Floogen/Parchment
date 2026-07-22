# Divider

A horizontal rule. With a texture it's a decorative flourish. Without one it's a plain line.

```json
{
  "Type": "Divider",
  "TexturePath": "Assets/PeacefulEnd.Parchment/divider1",
  "Sizing": "ShrinkToFit",
  "Alignment": "Center",
  "Scale": 2
}
```

```json
{
  "Type": "Divider",
  "TintColor": "255 0 0",
  "Sizing": "Fixed",
  "Width": 64,
  "Alignment": "Center",
  "Scale": 4
}
```

A textured divider's height comes from its source rectangle. A textureless one's comes from `Thickness`.

## Divider fields

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `Sizing` | [sizing mode](index.md#sizing-modes) | `Fill` | How wide the divider is. `Fill` stretches it across the column. `ShrinkToFit` draws the sprite at its natural size, placed by `Alignment`. `ShrinkToFit` requires a `TexturePath`, since a plain line has no natural width. |
| `Width` | integer | *none* | The divider's width in unscaled sprite pixels × `Scale`. Required when `Sizing` is `Fixed`. |
| `Thickness` | integer | `1` | The line's height in unscaled sprite pixels × `Scale`. **Only used when there's no texture** (with one, the art decides how thick the divider is). |

## Sprite fields

`HoverTextureSourceRectangle` has no effect: dividers aren't interactive unless you give them an `Action`, and a hovering rule would be odd.

--8<-- "sprite.md"

## Common fields

--8<-- "element-common.md"

!!! tip "`Fill` stretches the art"
    A `Fill` divider stretches its sprite to the full column width, which gives uneven pixels unless the art is a flat, repeatable rule. A one-pixel-wide source stretches perfectly to any width. For an ornate divider with detail in it, use `ShrinkToFit`. See [Preparing your art](../../concepts/art.md#stretched-rules).
