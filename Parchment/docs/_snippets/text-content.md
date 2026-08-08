<!-- Included by pages at docs/reference/elements/. The relative links below assume that depth. -->
| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Text` <span class="opt">optional</span> | `string` | — | The text to draw. Wraps automatically to the available width, including breaking words that are too long for a line on their own. `\n` forces a line break. Can carry [tokens](../../concepts/actions.md#tokens), both Parchment's `%Token%` forms and the game's [tokenizable strings](../../concepts/actions.md#game-tokens). |
| `FontType` <span class="opt">optional</span> | [`font type`](../elements/index.md#font-types) | *varies* | Which font to draw with. The default differs per element type. |
| `TextColor` <span class="opt">optional</span> | [`color`](../elements/index.md#colors) | *the book's default* | The text color. |
| `ShadowColor` <span class="opt">optional</span> | [`color`](../elements/index.md#colors) | *the game's shadow color* | The color of the drop shadow drawn behind the text, alpha included. Left off, the shadow follows `TextColor`'s alpha instead. Ignored when `FontType` is `SpriteText`, which draws its own outline. |
