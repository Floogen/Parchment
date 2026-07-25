# Preparing your art

Parchment slices, stretches and scales your sprites. Art that doesn't expect that will look subtly wrong in ways that are hard to trace back to the PNG, so learn the rules before you draw, rather than after.

All of them come down to the same idea: **Parchment only knows about rectangles, not about the pixels inside them.**

## Draw at 1×, scale in the book

Draw your art at the size you'd draw any Stardew sprite (16, 24, 32 pixels) and let `Scale` magnify it. Never pre-scale a PNG to 4× and set `Scale: 1`. You'll get a blurry sprite that can't be scaled down cleanly and a source rectangle four times bigger than it needs to be.

**Keep `Scale` a whole number.** A fractional scale makes some source pixels three screen pixels wide and others four, which on pixel art reads as "my art is subtly broken" rather than as a scale problem.

You mostly won't type a fractional scale. You'll get one. If an image is too wide for its column, Parchment shrinks it to fit, and the fitted scale is rarely whole. It warns when this happens. The fix is to make the art smaller or the `Scale` lower, not to live with it.

## Tighten your source rectangles

An element's size is its **source rectangle**, not the pixels painted in it. Empty rows at the top of a rectangle become empty space in the layout, and the picture looks mysteriously offset within its own box.

This is the single most common art problem, and it has two tells:

- The element seems to have a gap above or beside it that no `SpacingAfter` explains.
- `SpriteEffects: FlipHorizontally` appears to *move* the sprite sideways. It can't. Flipping mirrors within the bounds. What moved is the art inside the cell, by exactly twice the padding difference.

Turn on `parchment_debug` and look at the green box. If it's bigger than the picture, the rectangle is too loose.

## Hover art must be the same size

`HoverTextureSourceRectangle` gives you a highlighted or pressed state. It must have **the same width and height** as `TextureSourceRectangle`.

The reason is structural: the element measures itself from the normal rectangle once, and reuses that size when drawing the hover state. A 24-wide hover sprite in an element measured at 20 draws four pixels too wide, overhanging its own bounds and its own hitbox.

The easy convention is to stack the two states vertically in the sheet, so the rectangles differ only in `Y`:

```json
"TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
"HoverTextureSourceRectangle": { "X": 0, "Y": 18, "Width": 18, "Height": 18 }
```

A hover state is normally a recolour or a small highlight, not a redraw, so this is rarely a constraint in practice.

## Nine-slice frames

[`Panel`](../reference/elements/panel.md) and [`Button`](../reference/elements/button.md) are nine-sliced: the four corners are drawn as-is, the four edges stretch along one axis and the middle stretches both ways.

**The border is a third of the shorter side.** For a 24×24 frame that's 8-pixel corners and an 8×8 middle. For a 16×16 button, 5-pixel corners and a 6×6 middle. Note the truncation, `16 ÷ 3` isn't whole.

Two rules follow:

**Keep the visible frame inside the corner.** If your 16×16 button has a 4-pixel border with a highlight at pixel 5, that highlight is in the *stretched* region and will be smeared across the button's whole width. "My nine-slice looks wrong" is almost always art whose detail doesn't respect the third-boundaries.

**Keep the middle flat.** It stretches to whatever size the element ends up. A flat fill stretches perfectly. A pattern doesn't.

If your art doesn't divide cleanly, repad it. 15×15 gives exactly 5/5/5. 24×24 gives 8/8/8. Square frames give predictable thirds. A non-square frame still renders, but the border is derived from the shorter side and it won't look the way you drew it.

The scale drives the border's thickness: a 24×24 frame at `Scale: 4` has a 32-pixel border. To thicken a frame without inflating the element, raise `Scale` and lower `Padding` to compensate.

## Three-slice banners

A [`Banner`](../reference/elements/banner.md) is three-sliced: a fixed left cap, a stretching middle, a fixed right cap. `CapWidth` says where the caps end.

**Make the middle one pixel wide.** A 1-pixel middle stretches perfectly to any width. A wider patterned middle gives uneven columns as it stretches, and the wider the banner the worse it gets. A 49-pixel strip with `CapWidth: 24` is the shape you want.

The default `CapWidth` is a third of the strip, which is only the default because it's the least surprising reading of "three segments". It's rarely what you want.

A banner's height is always its source rectangle's height × `Scale`. It grows sideways, never down. If your text is taller than the strip it'll overflow, and the fix is a smaller `TextScale` or a taller strip, not a bigger banner.

## Stretched rules

A [`Divider`](../reference/elements/divider.md) with `Sizing: Fill` stretches across the whole column. Same rule as a banner's middle: flat art only.

A 1×3 strip stretches to any width perfectly and is all most dividers need. For an ornate divider with actual detail in it, use `ShrinkToFit` and let it sit at its natural size. Stretching it will ruin it.

## Animation frames

Every frame in an [`Image`](../reference/elements/image.md)'s `Frames` list must be **the same size** as the element's `TextureSourceRectangle`.

That rectangle is the measuring stick. It's what sets the element's size, once, at layout time. The frames are just what gets drawn. Parchment rejects mismatched frames at load rather than letting a frame overhang its own element.

Frames don't have to be adjacent, or in one row or in any particular order. Each names its own rectangle. That means a hover row and an animation row can coexist in one sheet: hover changes `Y`, animation walks `X`.

The source rectangle earns a second job if you use [frame conditions](../reference/elements/image.md#frames): it's what draws when every frame has been conditioned out. Point it at a cell that reads as a finished sprite rather than at whichever frame happened to be first, and a stopped animation still looks deliberate.

## Tinting wants greyscale

`TintColor` multiplies. Red on grey gives red. Red on blue gives near-black. Anything on black stays black.

So a sprite you intend to be recolourable should be drawn in greys. That's exactly why the book's own art has a separate greyscale layer. The greyscale takes the cover colour, and the coloured layer on top carries the details that shouldn't tint.

## A quick checklist

Before you finalize a sprite:

- Drawn at 1×, and the book's `Scale` is a whole number
- Source rectangle tight to the painted pixels
- Hover rectangle the same size as the normal one
- Nine-slice: visible border inside the corner third, middle flat
- Three-slice: middle one pixel wide
- Animation frames all the same size as the source rectangle
- Source rectangle usable as a still, since conditional animations fall back to it
- Anything tintable drawn in greys
