# Input

A text box the reader types into. What they type is held for as long as the book is open and read back by [conditions](../../concepts/conditions.md#reader-input), which is how you build a page that filters itself as the reader searches.

```json
{
  "Type": "Input",
  "InputId": "search",
  "TexturePath": "Assets/PeacefulEnd.Parchment/panelFrame1",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 18, "Height": 18 },
  "Placeholder": "Search...",
  "FontType": "Small",
  "Scale": 2,
  "Padding": 3,
  "MaxLength": 32
}
```

`InputId` is required. It's the handle everything else uses to reach the text, so give it something you'll recognise in a condition.

## Input fields

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `InputId` <span class="req">required</span> | `string` | — | The name conditions and actions use to reach this input's text. Expected to be unique within the book. |
| `Placeholder` <span class="opt">optional</span> | `string` | — | A prompt shown while the box is empty. Conditions see an empty input, not this. |
| `PlaceholderColor` <span class="opt">optional</span> | `string` | *a faded `TextColor`* | The prompt's colour, as a name such as `"Gray"` or a value such as `"128 128 128"`. |
| `MaxLength` <span class="opt">optional</span> | `int?` | — | The most characters the reader can type. Unbounded when omitted. |
| `Padding` <span class="opt">optional</span> | `int` | `0` | Space between the frame's inner edge and the text, in unscaled sprite pixels × `Scale`. |
| `Sizing` <span class="opt">optional</span> | [`sizing mode`](index.md#sizing-modes) | `Fill` | How wide the box is. `ShrinkToFit` hugs the `Placeholder` rather than the typed text, so the box doesn't grow and shrink under the reader as they type. |
| `Width` <span class="opt">optional</span> | `int?` | — | The **content** width in unscaled sprite pixels × `Scale`. Required when `Sizing` is `Fixed`. |
| `Height` <span class="opt">optional</span> | `int?` | *one line of text* | The **content** height in unscaled sprite pixels × `Scale`. Set it when you want `Scale` to size the box's height the way it sizes its width. |
| `TextScale` <span class="opt">optional</span> | `number` | `1` | The text's scale, independent of `Scale`, which sizes the frame. |
| `SubmitAction` <span class="opt">optional</span> | `string` | — | A [trigger action](../../concepts/actions.md) run when the reader presses enter. |
| `SubmitActions` <span class="opt">optional</span> | list of `string` | — | Trigger actions run in order on enter. Combined with `SubmitAction` rather than replacing it. |
| `TextChangedAction` <span class="opt">optional</span> | `string` | — | A trigger action run once the text has stopped changing. See [Reacting to typing](#reacting-to-typing). |
| `TextChangedActions` <span class="opt">optional</span> | list of `string` | — | Trigger actions run in order once the text settles. Combined with `TextChangedAction` rather than replacing it. |
| `TextChangedDelay` <span class="opt">optional</span> | `number` | `250` | How long the text has to sit still before the text changed actions run, in milliseconds. Each change restarts the wait. |

`Text` is the box's **starting** text rather than its label. The reader can edit it, and clearing the box doesn't bring it back.

---

## Filtering a list

The point of an input is that other elements can condition themselves on it. [`PeacefulEnd.Parchment_InputMatches`](../../concepts/conditions.md#reader-input) is true when the typed text appears in the text you give it, and true for everything while the box is empty:

```json
{
  "Elements": [
    { "Type": "Input", "InputId": "search", "TexturePath": "{{ModId}}/box", "Placeholder": "Search..." },
    { "Type": "Paragraph", "Text": "Tulip", "Condition": "PeacefulEnd.Parchment_InputMatches search Tulip" },
    { "Type": "Paragraph", "Text": "Blue Jazz", "Condition": "PeacefulEnd.Parchment_InputMatches search Blue Jazz" },
    { "Type": "Paragraph", "Text": "Nothing found.", "Condition": "PeacefulEnd.Parchment_HasInputText search" }
  ]
}
```

Typing narrows the list on the next keystroke, and the elements below close the gap because a hidden element takes up no space. Matching ignores case, and everything after the input's ID counts as the text being tested, so a phrase needs no quoting.

!!! warning "The list has to fit the page"
    Parchment [doesn't reflow](../../concepts/layout.md#when-content-doesnt-fit). Filtering hides elements, it doesn't move them onto another page, so a search over more entries than a page holds shows the first screenful and drops the rest with a warning. Keep the candidate list to a page, or narrow it with a second condition.

## Passing the text to an action

`%Input%` in any action is replaced with the box's current text just before the action runs. On the input itself the bare form means its own text, elsewhere name the box:

```json
{
  "Type": "Input",
  "InputId": "entry",
  "TexturePath": "{{ModId}}/box",
  "Placeholder": "Go to entry...",
  "SubmitAction": "PeacefulEnd.Parchment_JumpToPageId %Input%"
}
```

Paired with [`JumpToPageId`](../../concepts/actions.md#addressing-a-page) that gives you a box the reader types a page's `Id` into. It's an exact match rather than a search, so it suits a book whose pages have names worth typing.

```json
{
  "Type": "Button",
  "TexturePath": "{{ModId}}/button",
  "Text": "Go",
  "Action": "PeacefulEnd.Parchment_JumpToChapter %Input:chapterBox%"
}
```

The text is substituted already quoted, so a typed phrase stays a single argument. See [Passing input text](../../concepts/actions.md#passing-input-text).

## Reacting to typing

Conditions already keep up with the reader on their own, so a list filtered with `Parchment_InputMatches` needs nothing here. `TextChangedAction` is for the work that shouldn't happen on every keystroke: setting a flag, jumping to a results page, anything with a cost.

```json
{
  "Type": "Input",
  "InputId": "search",
  "TexturePath": "{{ModId}}/box",
  "Placeholder": "Search...",
  "TextChangedAction": "PeacefulEnd.Parchment_SetFlag searching",
  "TextChangedDelay": 250
}
```

The wait restarts on every change, so typing a word runs the actions once, when the reader pauses. `TextChangedDelay: 0` runs them on the next tick after each keystroke instead.

The text is watched rather than hooked to the keyboard, so **anything** that changes it counts, including [`Parchment_SetInput`](../../concepts/actions.md#parchments-actions) and a clear button. That's what lets a clear button reset whatever the search put in place, without the book having to run the same actions from two places.

!!! note "The starting text isn't a change"
    An input's authored `Text` is recorded the first time the element is looked at, without running anything, so a book doesn't fire its text changed actions the moment it opens.

## Gotchas

**Only one box has the keyboard.** Clicking a box focuses it, clicking anywhere else drops focus. While a box is focused every other key is taken over, so the chat hotkey, the book's own [keybinds](../../concepts/actions.md#running-actions-without-a-click) and other mods' hotkeys all stop firing until focus is dropped. Escape is the exception, leaving the box on the first press and closing the book on the second.

**Focus doesn't survive a page turn.** Turning a page, shutting to the cover or closing the book all drop it, since the box the reader was typing into may no longer be on screen.

**The text is per reading session.** Closing the book empties every input. Nothing is saved, so a search box starts blank each time the book is opened.

**No controller or touch keyboard yet.** The box is driven by the hardware keyboard. Gamepad and mobile players have no on-screen entry for it, so don't make a book's only route through one.

## Sprite fields

The texture must nine-slice: the border is a third of the shorter side. `HoverTextureSourceRectangle` gives the box a highlighted state under the cursor. The frame is optional, so a box with no `TexturePath` draws its text with nothing behind it, which suits an input sitting inside a [`Panel`](panel.md).

--8<-- "sprite.md"

## Text fields

Text is always drawn from the left and vertically centred, whatever `Alignment` says, since `Alignment` places the box itself. When the text outruns the box its start scrolls out of view, keeping the caret visible.

--8<-- "text-content.md"

## Common fields

`Scale` on an `Input` is the **frame** scale, sizing the border and the padding. `TextScale` sizes the text.

That leaves `Scale` with nothing proportional to grab on the vertical axis unless you give the box a `Height`. Without one, `Width` (under `Fixed` sizing) is multiplied by `Scale` while the height is just a line of text plus the frame, so raising `Scale` widens the box far faster than it deepens it. `Height` fixes that, and setting `TextScale` alongside `Scale` keeps the text in proportion with the box.

--8<-- "element-common.md"
