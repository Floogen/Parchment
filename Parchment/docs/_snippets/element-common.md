<!-- Included by pages at docs/reference/elements/. The relative links below assume that depth. -->
| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `Type` | [element type](../elements/index.md#element-types) | <span class="req">required</span> | Which kind of element this is. Determines every other field below. |
| `Id` | string | *none* | An optional identifier for this element. Not used for navigation, purely for your own reference. |
| `Alignment` | `Left` \| `Center` \| `Right` | `Left` | Where the element sits within its container's width. Only has an effect when the element is narrower than the space available (see [Layout](../../concepts/layout.md#alignment)). |
| `Scale` | number | `1` | The element's scale. **Its meaning depends on the element type**: sprite scale for `Image`, `Panel`, `Banner`, `Button` and `Divider`. Font scale for `Title`, `Heading` and `Paragraph`. See [Units and scale](../../concepts/units.md). |
| `SpacingAfter` | integer | `8` | The gap between this element and the next one, in unscaled sprite pixels × `Scale`. Not applied after the last visible element, so a trailing gap can't appear at the bottom of a page or panel. |
| `MarginLeft` | integer | `0` | Space reserved to the element's left, in unscaled sprite pixels × `Scale`. This narrows the width the element measures against, so text wraps at the indented width rather than overflowing. |
| `MarginRight` | integer | `0` | Space reserved to the element's right, in unscaled sprite pixels × `Scale`. |
| `Position` | point | `0, 0` | The element's position, in **screen** pixels. Only used in `Background`, `Underlay` and `Overlay`, where elements are placed rather than stacked. Unlike every other spacing field this is **not** multiplied by `Scale`. Changing an element's scale resizes it in place rather than moving it. |
| `Condition` | string | *none* | A [game state query](../../concepts/conditions.md). When it evaluates false the element is hidden, and elements below it close the gap. Re-checked several times a second while the book is open. |
| `Action` | string | *none* | A [trigger action](../../concepts/actions.md) run when the element is clicked. When set, the element becomes interactive: any element type can have one, not just `Button`. |
| `Sound` | string | `bigSelect` | The cue played when the element is clicked. Only used when `Action` is set. Set to `null` for a silent click. |
| `DisplayName` | string | *none* | The bold title of the element's hover tooltip. |
| `Description` | string | *none* | The body of the element's hover tooltip. |
