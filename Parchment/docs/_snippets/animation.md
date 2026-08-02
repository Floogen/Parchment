<!-- Included by pages at docs/reference/elements/. The relative links below assume that depth. -->
| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Frames` <span class="opt">optional</span> | list of [`frames`](../elements/index.md#frame-fields) | — | Animation frames. When omitted, the element draws where it was laid out. |
| `HoverFrames` <span class="opt">optional</span> | list of [`frames`](../elements/index.md#frame-fields) | — | Animation frames played while the cursor is over the element, replacing `Frames` for as long as it stays there. See [Hover frames](../elements/image.md#hover-frames). |
| `FrameDuration` <span class="opt">optional</span> | `number` | `100` | How long a frame is shown when it doesn't specify its own `Duration`, in milliseconds. |

### Frame fields

Each entry in `Frames` or `HoverFrames`:

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Offset` <span class="opt">optional</span> | `Point` | `{ X: 0, Y: 0 }` | How far this frame is shifted from where the element sits, in unscaled sprite pixels × the element's `Scale`. Positive moves right and down. See [Frame offset](../elements/image.md#frame-offset). |
| `Duration` <span class="opt">optional</span> | `number` | *the element's `FrameDuration`* | How long this frame is shown in milliseconds. |
| `Condition` <span class="opt">optional</span> | `string` | — | A [game state query](../../concepts/conditions.md) deciding whether this frame plays. When omitted the frame always plays. |
| `Action` <span class="opt">optional</span> | `string` | — | A [trigger action](../../concepts/actions.md) run each time this frame starts. See [Frame actions](../elements/image.md#frame-actions). |
| `Actions` <span class="opt">optional</span> | list of `string` | — | Trigger actions run in order each time this frame starts. Combined with `Action` rather than replacing it. |
| `SourcePoint` <span class="opt">optional</span> | `Point` | *the element's own sprite* | **[Sprite elements](index.md#sprite-fields) only.** The coordinate of the art for this frame, inheriting the element's `TextureSourceRectangle` for width and height. |
| `Scale` <span class="opt">optional</span> | `number` | `1` | **[`Image`](../elements/image.md) only.** A multiplier on the element's `Scale` while this frame draws. See [Frame scale](../elements/image.md#frame-scale). |

`SourcePoint` works on anything that draws art, so a `Panel`, `Button`, `Banner`, `Divider`, `Grid` or `Input` can step through a sheet the same way an `Image` does. It only moves where the art is read from, never how much of it, so a nine-sliced element keeps the border and the inset it was measured with.

`Scale` is the one field an `Image` alone can apply, since it's the only element drawn as a single quad with nothing inside it sized against it. Setting either field where it doesn't apply fails validation with a message saying so, rather than being quietly ignored.

Frames loop, and the cycle is timed from the moment the animation starts, so the first frame is the one that draws when it does.

A frame whose `Condition` fails is **skipped**, not paused on. The cycle gets shorter and the remaining frames close the gap, the same way a hidden element lets the ones below it close up. Conditions are re-checked while the book is open, so an animation can gain and lose frames as the game state changes.

**Gaining or losing a frame starts the animation over.** A cycle whose frame list changed isn't the cycle that was playing, so it restarts rather than resuming partway. That's what lets an animation gated behind a condition play properly: gate every frame on `PeacefulEnd.Parchment_CurrentPageId <your page>` and the whole thing plays from the top when the reader arrives, instead of catching it mid-cycle.

!!! tip "Timing a pause into a loop"
    Since the animation restarts when it becomes active, a long final frame reads as a delay before the next repeat. A ten-frame flourish followed by a frame of `60000` plays once on arrival then holds still for a minute, over and over, without needing anything to trigger it.

### Animating a panel's frame

`SourcePoint` swaps the art without touching the layout, so a nine-sliced border can flicker, pulse or react to the game state. Every frame has to point at a patch laid out the same way and the same size, since the element was measured once against those dimensions:

```json title="A panel border that shifts between two patches"
{
  "Type": "Panel",
  "TexturePath": "{{ModId}}/frames",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Frames": [
    { "Duration": 400 },
    { "Duration": 400, "SourcePoint": { "X": 18, "Y": 0 } }
  ]
}
```

!!! warning "Nothing checks that the art fits"
    A source point aimed at a patch that isn't nine-sliceable, or that has a different border, draws a mangled box rather than raising an error. There's no way to tell intent apart from a mistake here, so the sheet is yours to lay out carefully.

### Moving an element

`Offset` is the field that works on every element type, sprite or not. The element is measured once and keeps that space and that hitbox, so it slides over its own footprint rather than pushing the page around:

```json title="A panel that drifts up and down"
{
  "Type": "Panel",
  "TexturePath": "{{ModId}}/frame",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Frames": [
    { "Duration": 700 },
    { "Duration": 700, "Offset": { "X": 0, "Y": -1 } }
  ],
  "Children": [
    { "Type": "Paragraph", "Text": "Everything in here comes along for the ride." }
  ]
}
```

A container carries its children and its own `Background` and `Foreground` with it, since they are all drawn relative to the rectangle it was given.

!!! warning "Offsets add up"
    A moving element inside a moving container is shifted by both. Animate the container or its contents, not usually both, or pick offsets that read well together.
