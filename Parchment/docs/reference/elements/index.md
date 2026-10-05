# Elements

An element is one piece of content on a page: a heading, a picture, a framed callout. Every element has a `Type`, which decides which other fields it understands.

```json
{
  "Type": "Heading",
  "Text": "Setting up camp",
  "Alignment": "Center"
}
```

Elements appear in five places, and where they appear changes how they're positioned:

| Where | Positioning |
| --- | --- |
| [`Page.Elements`](../page.md) | Stacked top to bottom. |
| [`Page.Background`](../page.md) | Placed by `Position`, drawn behind the page's elements. |
| [`Page.Foreground`](../page.md) | Placed by `Position`, drawn over the page's elements. |
| [`Book.Underlay`](../book.md) | Placed by `Position` relative to the book, drawn behind the book sprite. |
| [`Book.Overlay`](../book.md) | Placed by `Position` relative to the book, drawn in front of everything. |

Where an element appears doesn't change what it can do. A tooltip, an `Action` or a `HoverAction` works the same in any of the five. Note that an element in `Page.Background` or `Page.Foreground` with none of those is [transparent to the cursor](../page.md#background-and-foreground), so decorative art doesn't cover the page.

---

## Element types

<div class="grid cards" markdown>

-   **[Title](title.md)** (`Title`)

    Large heading text.

-   **[Heading](heading.md)** (`Heading`)

    Section heading text.

-   **[Paragraph](paragraph.md)** (`Paragraph`)

    Body text.

-   **[Image](image.md)** (`Image`)

    A sprite, an animation or an item's icon, optionally with text drawn on it.

-   **[Divider](divider.md)** (`Divider`)

    A horizontal rule, plain or decorative.

-   **[Panel](panel.md)** (`Panel`)

    A nine-sliced frame containing other elements.

-   **[Banner](banner.md)** (`Banner`)

    A three-sliced strip with text in the middle, a scroll or ribbon.

-   **[Button](button.md)** (`Button`)

    A nine-sliced frame with a label, for running an action.

-   **[Page number](page-number.md)** (`PageNumber`)

    The page's own number, filled in automatically.

-   **[Grid](grid.md)** (`Grid`)

    A container laying its children out across fixed-size cells.

-   **[Input](input.md)** (`Input`)

    A text box the reader types into, for filtering a page against what they've typed.

</div>

An unrecognised `Type` is skipped with a warning rather than breaking the book.

---

## Common fields

Every element understands these, whatever its type.

--8<-- "element-common.md"

!!! tip "Any element can be clickable"
    `Action` lives on every element, not just `Button`. An `Image` with an `Action` is a perfectly good bookmark or tab. `Button` is just the shorthand for the common case of a framed label.

## Text fields

Understood by [`Title`](title.md), [`Heading`](heading.md), [`Paragraph`](paragraph.md), [`Banner`](banner.md), [`Button`](button.md), [`Image`](image.md) and [`Input`](input.md).

Any element's `Text` can carry [tokens](../../concepts/actions.md#tokens), placeholders replaced with something the book knows as the element is laid out. That covers Parchment's own `%Token%` forms and the game's `[Token]` [tokenizable strings](../../concepts/actions.md#game-tokens). Part of it can also be colored with [inline color](#inline-color) markup, given a [text effect](#text-effects) or turned into a [link](#links) on a `Title`, `Heading`, `Paragraph` or `PageNumber`.

--8<-- "text-content.md"

## Sprite fields

Understood by [`Image`](image.md), [`Panel`](panel.md), [`Grid`](grid.md), [`Banner`](banner.md), [`Button`](button.md), [`Divider`](divider.md) and [`Input`](input.md).

--8<-- "sprite.md"

## Animation fields

Understood by every element type. On an [`Image`](image.md) a frame steps through a sprite sheet. Everywhere else it moves the element, times a [trigger action](../../concepts/actions.md), or both.

--8<-- "animation.md"

---

## Font types

| Value | What it is |
| --- | --- |
| `Dialogue` | The game's main dialogue font. Large. |
| `Small` | The game's small font. The usual choice for body text and labels. |
| `Tiny` | The game's tiny font. |
| `SpriteText` | The game's bitmap title font, the one vanilla uses for menu headers. Its natural size is **large**: a dozen characters at `TextScale: 1` is around 300 pixels wide. |

`TextScale: 1` means each font's own natural size, so switching `FontType` changes the size. On elements that have both a sprite and text (`Banner`, `Button` and a text-bearing `Image`) `TextScale` sizes the text and `Scale` sizes the sprite, independently.

## Sizing modes

Used by [`Panel`](panel.md), [`Banner`](banner.md), [`Button`](button.md) and [`Divider`](divider.md) to decide how wide they are.

| Value | Behaviour |
| --- | --- |
| `Fill` | Take the full width available. |
| `ShrinkToFit` | Be exactly as wide as the contents need. The element is then placed by its `Alignment`. |
| `Fixed` | Be exactly `Width` wide. Requires `Width`. |

In every mode the result is clamped to the space available, so an element can never be wider than its container.

## Colors

Color fields accept any of:

| Form | Example |
| --- | --- |
| A color name | `"SkyBlue"` |
| RGB hex | `"#8B4513"` |
| RGBA hex | `"#8B4513FF"` |
| 8-bit RGB | `"34 139 34"` |
| 8-bit RGBA | `"34 139 34 255"` |

Values are space-separated, not comma-separated. An unparsable color logs a warning and falls back to the default.

Alpha is optional and full strength when left off. Write the color you want at full strength and let the alpha fade it, the same way the game does elsewhere: `"255 0 0 128"` is a half-faded red rather than a brighter one. The channels are scaled by the alpha before anything is drawn, so a translucent color fades towards whatever is behind it instead of washing out towards white.

## Inline color

A text element can color part of its `Text` by wrapping it in `[color=...]` and `[/color]`:

```json
{
  "Type": "Paragraph",
  "Text": "You reeled in a [color=Gold]Legend[/color] at [color=#4A90D9]Mountain Lake[/color]."
}
```

The value takes any of the [color forms](#colors) above. Everything outside a tag keeps the element's `TextColor`.

- **Nesting.** Tags nest, so `[color=Red]a [color=Blue]b[/color] c[/color]` draws `c` red again.
- **Tokens.** Tokens work inside a run, game tokens included: `[color=Gold][ItemName (O)128][/color]`. The markup is read from the text as written before any token resolves, so a value a token brings in (such as something the player typed into an `Input`) is always drawn as it is and can't color anything.
- **Shadow.** A colored run keeps the element's `ShadowColor`. Left unset, the shadow follows the run's alpha the same way it follows `TextColor`'s.
- **Case.** Tags are case-insensitive, so `[Color=Red]` works too.
- **Mistakes.** A `[color]` that is never closed runs to the end of the text. A stray `[/color]` is dropped. A color that won't parse keeps the color around it. Each logs a warning.

Markup is read on every text element except `Input`, whatever `ParseTokenizableStrings` is set to.

!!! note "Where markup isn't drawn"
    `SpriteText` keeps its own color, so `[color]` and the coloring effects (`[rainbow]`, `[gradient]` and `[pulse]`) on an element drawn in it are ignored with a warning. A tooltip's `DisplayName` and `Description` draw plain text, so any markup in them is taken out rather than shown.

## Links

A `Title`, `Heading`, `Paragraph` or `PageNumber` can turn part of its text into a link by wrapping it in `[link=id]` and `[/link]`. The `id` names an entry in the element's `Links`:

```json
{
  "Type": "Paragraph",
  "Text": "You reeled in a [link=legend]Legend[/link] at [color=Blue]Mountain Lake[/color].",
  "Links": {
    "legend": {
      "TextColor": "Gold",
      "HoverTextColor": "Orange",
      "DisplayName": "Legend",
      "Description": "Only bites in spring rain.",
      "Action": "PeacefulEnd.Parchment_JumpToPageId legendary-fish"
    }
  }
}
```

A link behaves like an element of its own: it has its own tooltip, runs its own actions and carries its own tags, all reached through the stretch of text it covers.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Condition` <span class="opt">optional</span> | `string` | — | A [game state query](../../concepts/conditions.md) deciding whether the link applies wherever it's used. When it fails the text is drawn plain (no color, tooltip or action) and can't be reached. See [Tag conditions](#tag-conditions). |
| `TextColor` <span class="opt">optional</span> | [`color`](#colors) | *the color around it* | The linked text's color. |
| `HoverTextColor` <span class="opt">optional</span> | [`color`](#colors) | — | The linked text's color while the cursor is over it. Covers any `[color]` markup inside the link. |
| `DisplayName` <span class="opt">optional</span> | `string` | — | The bold title of the link's hover tooltip. Can carry [tokens](../../concepts/actions.md#tokens). |
| `Description` <span class="opt">optional</span> | `string` | — | The body of the link's hover tooltip. Can carry [tokens](../../concepts/actions.md#tokens). |
| `Action` <span class="opt">optional</span> | `string` | — | A [trigger action](../../concepts/actions.md) to run when the link is clicked. Shorthand for a single-entry `Actions` that runs first when both are given. |
| `Actions` <span class="opt">optional</span> | `string[]` | — | Trigger actions to run in order when the link is clicked. |
| `HoverAction` <span class="opt">optional</span> | `string` | — | A trigger action to run when the cursor moves onto the link. Shorthand for a single-entry `HoverActions` that runs first when both are given. |
| `HoverActions` <span class="opt">optional</span> | `string[]` | — | Trigger actions to run in order when the cursor moves onto the link. |
| `Tags` <span class="opt">optional</span> | `string[]` | — | [Tags](../tags.md) carried by the link, read the same way as an element's own. |
| `HoverEffect` <span class="opt">optional</span> | `string` | — | A [text effect](#text-effects) applied to the linked text while the cursor is over it. Shorthand for a single-entry `HoverEffects` that applies first when both are given. |
| `HoverEffects` <span class="opt">optional</span> | `string[]` | — | Text effects applied in order to the linked text while the cursor is over it. See [Hover effects](#hover-effects). |

- **Each occurrence is its own link.** The same id used twice gives two links that are hovered, highlighted and reached by a controller separately.
- **Wrapping.** A link that runs onto a second line is reached through its text on both lines, never through the words between them.
- **Nesting.** `[color]` works inside and around a link. A link inside another link takes over the text it covers. Tags of the two kinds may overlap rather than nest, in which case each closing tag closes the innermost tag of its own kind.
- **Tokens and actions.** A link's tooltip and actions resolve tokens against the link. It follows the element's `ParseTokenizableStrings` and plays the element's `Sound` when clicked.
- **The rest of the text.** Text outside every link still shows the element's own tooltip and runs the element's own actions. So does a link that only sets `TextColor`, since it gives the cursor nothing to do.
- **Controller.** Each link is a stop of its own, placed on the first line it covers.
- **Hover effects.** A link can change effects under the cursor with [hover effects](#hover-effects).
- **Mistakes.** A `[link]` naming an id that isn't in the element's `Links` or the [book's](#shared-links) is drawn as plain text with a warning. So is a `[link]` on any other element. Unclosed and stray tags behave as they do for [inline color](#inline-color).

!!! note "Links in `SpriteText`"
    A link drawn in `SpriteText` keeps its tooltip, actions and tags. Its `TextColor` and `HoverTextColor` are ignored, as `SpriteText` keeps its own color.

### Shared links

A link used in many places can be defined once in the [book's](../book.md#fields) own `Links` rather than on every element that points at it:

```json
{
  "Id": "you.FishingJournal_Book",
  "Links": {
    "legend": {
      "TextColor": "Gold",
      "DisplayName": "Legend",
      "Description": "Only bites in spring rain."
    }
  },
  "Pages": [
    {
      "Id": "catches",
      "Elements": [
        { "Type": "Paragraph", "Text": "You reeled in a [link=legend]Legend[/link]." }
      ]
    }
  ]
}
```

A `[link]` looks in the element's own `Links` first and the book's second. When both define the same id, the element's entry replaces the book's whole for that element rather than filling in around it, so a link's values always come from one place.

## Text effects

These tags move or recolor each character of the text they wrap, without changing how it's laid out:

| Tag | What it does | Value | Default |
| --- | --- | --- | --- |
| `[wave]` | Bobs each character up and down a beat behind the one before it. | amplitude and period | `2` and `1000` |
| `[bounce]` | Hops each character up off the line and back down, a beat behind the one before it. | amplitude and period | `2` and `800` |
| `[shake]` | Jitters each character to a new spot every period. | amplitude and period | `1` and `80` |
| `[rainbow]` | Colors each character one step further along the game's rainbow, cycling through every color once per period. | period | `2000` |
| `[gradient]` | Blends the text from one color to the next across its whole length, without moving. | two or more colors | *required* |
| `[pulse]` | Fades the whole stretch towards a color and back once per period. | color and period | *a color is required*, then `1500` |
| `[typewriter]` | Reveals the text one character at a time, the way the game's dialogue box does. See [Typewriter](#typewriter). | speed, delay and options | `30` and `0` |
| `[underline]` | Draws a line under the text. See [Decorations](#decorations). | color | the text's color |
| `[strike]` | Draws a line through the text. | color | the text's color |
| `[highlight]` | Draws a marker box behind the text. | color | a soft yellow |
| `[redact]` | Covers the text with a solid bar, hiding it while keeping its width. | color | the text's color |

```json
{
  "Type": "Paragraph",
  "Text": "A [wave]legendary[/wave] catch! The line [shake=2]snapped[/shake] on a [rainbow]prismatic[/rainbow] fish. [bounce]Hooray![/bounce]"
}
```

```json
{
  "Type": "Heading",
  "Text": "[gradient=Orange|Gold|255 240 160]Sunset Lake[/gradient] holds a [pulse=Gold|800]secret[/pulse]."
}
```

- **Values.** A tag's value is split into parts by `|`, the same for every effect. Every part except a color is optional. `[wave=4]` sets only the amplitude and `[wave=4|500]` sets both. `[bounce]` and `[shake]` read their values the same way. `[rainbow]` takes only a period, so `[rainbow=1000]` cycles twice as fast and `[rainbow=0]` holds still, the way the game draws its own rainbow text.
- **Colors.** A color keeps any spaces of its own, such as `255 215 0`, which is why parts aren't split on spaces. `[gradient]` takes two or more colors, spread evenly from the first character to the last. `[pulse]` takes one color and then an optional period, such as `[pulse=Gold]` or `[pulse=Gold|800]`. Both take any of the [color forms](#colors).
- **Units.** An amplitude is in unscaled pixels multiplied by the text's scale (its `Scale`, which is `TextScale` on an element with a sprite). A period is in milliseconds: how long one bob or hop takes for a wave or a bounce, how long each spot is held for a shake, how long a full trip through the colors takes for a rainbow and how long one fade out and back takes for a pulse.
- **Layout.** An effect only changes the drawing. Lines keep the room they were laid out with, so a large amplitude overlaps the lines around it rather than pushing them apart. A moving link is still reached where its text rests.
- **Combining.** Effects stack, so `[shake][rainbow]...[/rainbow][/shake]` both jitters and recolors. They also work inside and around `[color]` and `[link]`.
- **Color order.** Coloring effects apply from the outside in. A rainbow or a gradient replaces the color around it (any `[color]` included), while a pulse fades from whatever color is around it, so `[rainbow][pulse=White]...[/pulse][/rainbow]` pulses the rainbow. A hovered link's `HoverTextColor` covers the coloring effects in its text so the link still shows it's under the cursor, though its own [hover effects](#hover-effects) start from it.
- **Continuity.** An effect carries on unbroken across a color change or a line break. A gradient spreads over its whole stretch rather than restarting on each line.
- **Fonts.** `[wave]`, `[bounce]` and `[shake]` work in every font, `SpriteText` included. `[rainbow]`, `[gradient]` and `[pulse]` are colors, which `SpriteText` ignores with a warning.
- **Cost.** Each character under an effect is drawn on its own, so keep effects to a word or a phrase rather than a whole page.
- **Mistakes.** An amplitude or period that won't parse keeps its default with a warning. When it was written with a space rather than `|`, such as `[wave=4 500]`, the warning shows the corrected tag. A color that won't parse is left out of a gradient. A gradient left with fewer than two colors keeps the color around it with a warning. So does a pulse without a color. Unclosed and stray tags behave as they do for [inline color](#inline-color).

### Decorations

`[underline]`, `[strike]`, `[highlight]` and `[redact]` draw a line or a box over the stretch of text they wrap rather than changing the characters themselves:

```json
{
  "Type": "Paragraph",
  "Text": "[strike]Catch a carp[/strike] done! Next, the [highlight]Legend[/highlight] in [underline=SkyBlue]Mountain Lake[/underline]."
}
```

Each takes one optional color, such as `[highlight=255 200 200 128]` or `[underline=Red]`. Left off, a highlight is a soft translucent yellow and the rest take the color of the text they decorate.

- **Steady.** A decoration stays where the text was laid out, so `[wave][underline]...[/underline][/wave]` keeps a straight underline under the moving letters.
- **Across lines.** A decoration that wraps is drawn on each line it reaches, joining up wherever its text continues.
- **Redacting.** `[redact]` draws only its bar, so nothing of the text underneath shows. A link whose text is fully redacted can't be reached by the cursor or a controller either, so its tooltip can't give away what's hidden. Paired with a [tag condition](#tag-conditions), it hides something until the player has earned it: `[redact=condition=!PLAYER_HAS_CAUGHT_FISH Current (O)163]Legend[/redact]`.
- **Typewriters.** Under a typewriter, a decoration grows along with the text it covers rather than appearing ahead of it.
- **Hover effects.** `[underline]`, `[strike]` and `[highlight]` work as a link's [hover effects](#hover-effects), so `"HoverEffect": "underline"` underlines a link while the cursor is on it. `[redact]` doesn't, as it would hide the very text the cursor is on.
- **Fonts.** Decorations are drawn by Parchment rather than the font, so they work in `SpriteText` too. Give them a color there, as `SpriteText` draws its letters in its own color rather than the element's.

### Typewriter

`[typewriter]` hides the text it wraps and then reveals it a character at a time, at the same pace as the game's dialogue box:

```json
{
  "Type": "Paragraph",
  "Text": "[typewriter]Dear friend, the fish are biting again.[/typewriter]"
}
```

Its value takes two numbers and then any options, all separated by `|` and all optional:

| Part | What it sets | Default |
| --- | --- | --- |
| first number | Milliseconds each character takes to appear. | `30` |
| second number | Milliseconds to wait before the first character. | `0` |
| `immediate` | Start as soon as the text is on screen instead of waiting for the typewriters before it. | waits its turn |
| `fade` or `fade=milliseconds` | Fade each character in instead of popping it in. A bare `fade` takes 100 milliseconds. | pops in |
| `sound=cue` | Play a sound cue as characters appear, such as `sound=dialogueCharacter` for the game's own typing sound. | no sound |
| `condition=query` | Type only when a [game state query](../../concepts/conditions.md) passes, checked once when the typewriter would start. When it fails the text shows in full at once. See [Tag conditions](#tag-conditions). | always types |

So `[typewriter=20|500|fade|sound=dialogueCharacter]` waits half a second, then types quickly with each character fading in to the game's typing sound.

- **When it starts.** A typewriter on a page starts once its spread has settled into view, after any page turn. One in an element that's hidden until a `Condition` passes or a `ShowElement` names it starts when that element appears instead. One on the book's own `Underlay` or `Overlay` starts when the book first settles, open or on its cover.
- **Order.** Typewriters on a spread type one after another in the order they're drawn, left page then right, so the spread reads like it's being written. Each waits for the one before it to finish, plus its own delay. `immediate` starts one alongside the others instead, without holding up the ones after it. A hidden element doesn't hold the others up either.
- **Replaying.** Each typewriter types once per reading. Turning back to a spread shows its text in full, while closing the book and opening it again types it out afresh.
- **Skipping.** While anything on screen is still typing, a click (or a controller's confirm button) finishes all of it at once and does nothing else, the same as the game's dialogue. It doesn't also press the button it landed on or turn the page. Keybinds are unaffected.
- **Links.** A link in text that hasn't been typed out yet can't be reached by the cursor or a controller until all of its text is showing.
- **Sound.** Only one cue plays at a time however many typewriters are revealing at once. It follows the player's own typing sound option, the same as the game's dialogue.
- **Layout.** The text takes its full space from the start, so lines never reflow as they fill in.
- **Combining.** Other effects work inside and around a typewriter, so `[typewriter][wave]...[/wave][/typewriter]` waves each character as it appears. A typewriter can't be a link's [hover effect](#hover-effects), as it reveals text once rather than coming and going with the cursor.

#### Pauses

A `[pause]` inside a typewriter holds the reveal for a moment before carrying on, for pacing a line the way it would be spoken:

```json
{
  "Type": "Paragraph",
  "Text": "[typewriter]Wait…[pause=600] what was that?[/typewriter]"
}
```

- **Length.** Its value is how many milliseconds it holds. A bare `[pause]` holds for 500.
- **No closing tag.** Unlike every other tag, `[pause]` marks a single point in the text rather than wrapping any.
- **What waits.** A pause lengthens its typewriter, so the typewriters after it and the element's [typed actions](#typed-actions) wait for it too. One placed just before `[/typewriter]` holds whatever comes next without holding any of its own text.
- **Conditions.** `[pause=600|condition=...]` only holds when its [condition](#tag-conditions) passes, checked once when the typewriter starts, the same as a typewriter's own.
- **Skipping.** A click finishes the whole typewriter, pauses included.
- **Mistakes.** A `[pause]` outside a typewriter has nothing to hold, so it's ignored with a warning.

#### Typed actions

An element's `TypedAction` and `TypedActions` run once every typewriter in its text has finished. Pair them with something that should only appear once the text is written, such as a button below a letter:

```json
{
  "Type": "Paragraph",
  "Id": "letter",
  "Text": "[typewriter]Dear friend, the fish are biting again.[/typewriter]",
  "TypedAction": "PeacefulEnd.Parchment_SetFlag letterWritten"
}
```

- **Finishing early.** They run whether the text typed out or the reader clicked to finish it, so skipping never loses what they do.
- **Once per reading.** They run once each reading, the same as the typing. Turning back to the page doesn't run them again, while closing the book and opening it again does.
- **Turning away.** Typing finished by turning away from the page runs them the next time that page is on screen, so nothing they do lands while the page is turning.
- **Chaining.** A hidden element with its own typewriter starts typing as it appears, so a postscript can follow a letter by giving it a `Condition` on the flag a typed action sets (or on `HasFinishedTyping` below).
- **Without an action.** The [`HasFinishedTyping`](../../concepts/conditions.md#the-page) query lets another element's `Condition` wait on the typing directly, such as `"Condition": "PeacefulEnd.Parchment_HasFinishedTyping letter"`. Conditions are checked on a short timer, so it can appear a moment after the typing ends.
- **Mistakes.** An element with a typed action but no `[typewriter]` in its text logs a warning when the book loads, as they could never run.

### Hover effects

A link's `HoverEffect` and `HoverEffects` apply [text effects](#text-effects) to the linked text only while the cursor (or a controller) is on it. Each entry is written the way its tag is, without the brackets:

```json
"Links": {
  "legend": {
    "TextColor": "Gold",
    "HoverTextColor": "Orange",
    "HoverEffects": [ "wave=3|600", "pulse=White" ]
  }
}
```

- **Easing in.** The effects start from rest when the cursor arrives and grow to full size over a moment, so a wave never appears mid-cycle. They stop as soon as the cursor leaves.
- **Order.** Hover effects apply after any effects written in the text itself, so they stack on top. A coloring hover effect starts from the link's `HoverTextColor` when one is set, so `HoverTextColor: White` with `pulse=Gold` fades between white and gold.
- **Whole link.** Each effect covers the whole of the link's text, so a gradient spreads across all of it, even when a link nested inside cuts it in two.
- **Interactivity.** A link with hover effects is reachable by the cursor even when it has no tooltip or action.
- **Mistakes.** An entry naming no effect Parchment knows is skipped with a warning listing the ones it does. Values that won't parse behave as they do in the text.

## Tag conditions

Any tag can take a `condition=` part holding a [game state query](../../concepts/conditions.md). The tag only applies while the query passes:

```json
{
  "Type": "Paragraph",
  "Text": "The lake is [color=SkyBlue|condition=WEATHER Here Rain]full of rain[/color] and the [link=legend|condition=PLAYER_HAS_SEEN_EVENT Current 123]Legend[/link] stirs.",
  "Links": {
    "legend": { "TextColor": "Gold", "Description": "Only bites in spring rain." }
  }
}
```

It goes alongside the tag's other parts, separated by `|` the same way, in any position: `[wave=4|500|condition=SEASON spring]` or `[gradient=Red|Blue|condition=!IS_FESTIVAL_DAY]`. On a tag with nothing else to set, the condition is its whole value: `[underline=condition=SEASON spring]`.

- **When it fails.** The tag steps aside and its text is drawn as though the tag weren't there. A failed `[color]` keeps the color around it, a failed effect leaves its characters still, a failed decoration isn't drawn and a failed `[link]` is plain text that the cursor can't reach. The text itself always shows.
- **When it's checked.** Alongside element [conditions](../../concepts/conditions.md#when-conditions-are-checked), so the text follows the query while the book is open. The query isn't run every time the text is drawn.
- **Typewriters.** A `[typewriter]`'s condition is checked once, when it would start, so its text can't appear and vanish as the query comes and goes. When it fails the text shows in full at once and the element's [typed actions](#typed-actions) still run.
- **Links.** A link's own `Condition` switches it off everywhere it's used, while a `condition=` on a `[link]` tag switches off just that one use. Both have to pass.
- **Tokens.** Parchment's `%Token%` forms resolve against the element, the same as in its `Condition`. `!` negation and comma-separated queries work as usual.
- **Limits.** A tag's value can't hold square brackets, so the game's `[Token]` forms can't go in an inline condition. Nor can a `|`, which would start the tag's next part. Set a [flag](../../concepts/actions.md#session-flags) or [variable](../variables.md) elsewhere and check that instead. The element's own `Condition` can also hold the query.
- **Hover effects.** A link's [hover effects](#hover-effects) don't take a condition, since the link's own `Condition` already governs them. One given is ignored with a warning.

## Rectangles and points

Rectangles and points are objects:

```json
"TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 16, "Height": 16 },
"Position": { "X": -64, "Y": 192 }
```
