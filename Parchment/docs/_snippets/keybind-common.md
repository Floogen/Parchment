<!-- Included by pages at docs/reference/. The relative links below assume that depth. -->
| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Keybind` <span class="req">required</span> | `string` | — | The key running the actions. See [Writing a keybind](#writing-a-keybind). |
| `Condition` <span class="opt">optional</span> | `string` | — | A [game state query](../concepts/conditions.md) deciding whether the actions run. When omitted, they always run. Checked at the moment the key is pressed rather than polled. Understands [tokens](../concepts/conditions.md#tokens-in-conditions). |
| `Action` <span class="opt">optional</span> | `string` | — | A single [trigger action](../concepts/actions.md), the shorthand for a one-entry `Actions`. Understands [tokens](../concepts/actions.md#tokens). |
| `Actions` <span class="opt">optional</span> | list of `string` | — | [Trigger actions](../concepts/actions.md), run in order. Combined with `Action` rather than replacing it. Understands [tokens](../concepts/actions.md#tokens). |
| `Sound` <span class="opt">optional</span> | `string` | — | A cue played once when the bind fires. Unlike an element's `Sound` this defaults to silence, since a key press has no click to answer. |
| `SuppressDefault` <span class="opt">optional</span> | `bool` | `true` | Whether a match stops the key reaching the menu's own handling. Leave it on to override a key, turn it off to run alongside whatever the key already does. |

At least one of `Action` or `Actions` is required.

!!! note "A bind has no element to read from"
    A keybind belongs to the page or to the book rather than to an element, so `%Input%` on its own, `%Item%` and `%Tags%` have nothing to answer with and are left in place. `%Input:someId%`, `%Variable:someId%` and the grid tokens work here as they do anywhere.

### Writing a keybind

`Keybind` uses SMAPI's keybind syntax, the same as a mod's config file:

| Form | Meaning |
| --- | --- |
| `Escape` | A single key. |
| `LeftControl + S` | A combination, matching only while the other keys are held. |
| `Escape, Back` | Alternatives, matching when any one of them does. |

Controller buttons work by name in the same field, so `"Escape, ControllerB"` covers both inputs. The button names are on the wiki:

> **[Modding: Player guide - Key bindings](https://stardewvalleywiki.com/Modding:Player_Guide/Key_Bindings)**

!!! warning "Mouse buttons aren't supported here"
    A book already spends its clicks on elements and page corners. Put a `MouseRight` in a `Keybind` and it never fires. Use an element's `Action` for anything the reader points at.
