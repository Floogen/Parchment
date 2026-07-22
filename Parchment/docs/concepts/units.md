# Units and scale

Almost every number in a book is in one of two units, and knowing which is which will save you a lot of confusion.

## Unscaled sprite pixels × `Scale`

**Anything measured against art.** You read these off your PNG at 1× (the size you drew it) and Parchment multiplies by `Scale` when it draws.

This covers `Padding`, `Width`, `Height`, `CapWidth`, `Thickness`, `TextArea`, `SpacingAfter`, `MarginLeft`, `MarginRight`, the book's `Layout` margins and the page curl offsets.

The point is that they track the art. Double a panel's `Scale` and its padding doubles with it, so the frame still looks the same, just bigger.

## Screen pixels

**`Position`, and only `Position`.** It's a coordinate, not a measurement. It says where on the page or book something sits, and that has nothing to do with how big the thing is.

So changing a positioned element's `Scale` resizes it **in place**, growing from its top-left, rather than moving it.

## What `Scale` means

`Scale` means different things on different elements:

| Element | `Scale` is | Text scaled by |
| --- | --- | --- |
| `Title`, `Heading`, `Paragraph` | the font scale | `Scale` |
| `Divider`, `Panel` | the sprite scale | — |
| `Image`, `Banner`, `Button` | the sprite scale | `TextScale` |

The split exists because a banner has both a sprite and text, and they have nothing to do with each other: a scroll is pixel art authored at 1× and drawn at 4× or 5×, while a font isn't. One knob couldn't serve both.

## `TextScale: 1` is the font's natural size

Whatever `FontType` you pick, `TextScale: 1` draws it at the size it was designed at. That means switching fonts changes the size, and `SpriteText`'s natural size is **large**, since it's the game's menu-header font. A dozen characters at `TextScale: 1` is around 300 pixels wide.

If SpriteText text is overflowing, that's why. `Small` is the usual choice for anything that isn't a title.

## Keep scales whole

A fractional `Scale` makes some source pixels three screen pixels wide and others four. On pixel art that's visible, and it reads as "my art is subtly wrong" rather than as a scale problem.

It's mostly not something you'd type deliberately. It comes from Parchment shrinking an oversized image to fit the column. If a picture looks smeared, it's probably too big for its page at the scale you asked for. Parchment logs a warning when it has to shrink one.
