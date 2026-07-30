# Building books in C#

A SMAPI mod can build a book in code instead of shipping it as a content pack. This page covers the builder; for fetching the API and opening books that already exist, see [C# API](api.md).

Reach for this when a book's contents depend on something a content pack can't see: a quest log that reflects what the player has done, a bestiary that fills in as creatures are found, a reference generated from another mod's data.

If your book is the same every time, a content pack is still the better home. It's translatable, other authors can patch it and you don't need a C# mod at all.

## A complete example

```csharp
IBookBuilder book = parchment.CreateBook("you.CampingGuide");
book.Sprite("you.CampingGuide/book");

IPageBuilder cover = book.AddPage("cover");
cover.AddTitle("Camping Guide").Alignment("Center");
cover.AddImage("you.CampingGuide/cover").Alignment("Center").Scale(3);
cover.AddButton("Begin", "PeacefulEnd.Parchment_NextPage").Alignment("Center");

IPageBuilder tents = book.AddPage("tents", "chapter-tents");
tents.AddHeading("Tents");
tents.AddParagraph("A starter tent sleeps one, and packs down to nothing.");
tents.AddItemImage("(O)24").Alignment("Center");

if (book.TryRegister(out string error) is false)
{
    Monitor.Log($"Couldn't register the camping guide, because {error}.", LogLevel.Warn);
}
```

Hold each page in a local rather than trying to write the whole book as one chain. Every `Add` method returns the *new* element's builder so you can configure it, which means chaining walks away from the page rather than back to it.

## Registering or opening

Two ways to finish a book, and they behave very differently.

| | `TryRegister` | `TryOpen` |
| --- | --- | --- |
| Where the book goes | Into `Data/PeacefulEnd.Parchment/Books` | Nowhere, it opens immediately |
| Opened by ID later | Yes, by item, tile action or `TryOpenBook` | No |
| Patchable by Content Patcher | Yes | No |
| Rebuilt | On every asset load | Every time you call it |
| Good for | A book that exists in the world | A book assembled fresh for this reading |

!!! tip "Registered books stay patchable"
    Registrations are added to the books asset *before* content packs are applied, so Content Patcher can edit, translate or replace anything you register. Other authors can extend your book without you exposing an API of your own.

Registering the same book ID again replaces your earlier registration, so re-registering is how you update a book. Call it from `GameLaunched` for a book that always exists, or later (after a save loads, say) for one that depends on the save.

```csharp
parchment.TryUnregisterBook("you.CampingGuide", out string error);
```

You can only remove books your own mod registered. Books from content packs, and from other mods, are left alone.

## The book builder

| Method | What it does |
| --- | --- |
| `Set(field, value)` | Sets any [book field](book.md) by name. Dotted paths reach nested groups, such as `"Appearance.Scale"`. |
| `Sprite(path)` | The sprite for the book item. |
| `AddPage(pageId)` | Adds a page, in reading order. |
| `AddPage(pageId, chapterId)` | Adds a page belonging to a chapter. Pages sharing a chapter must be added together. |
| `AddUnderlay(type)` | Adds an element drawn behind the book sprite. |
| `AddOverlay(type)` | Adds an element drawn in front of everything. |
| `OnKeyPress(keybind, action)` | Runs a trigger action when the key is pressed on any page of the book. A page binding the same key takes it over. |
| `OnKeyPress(keybind, action, condition)` | The same, gated by a [game state query](../concepts/conditions.md). |
| `TryRegister(out error)` | Validates and registers the book. |
| `TryOpen(out error)` | Validates and opens the book without registering it. |

## The page builder

| Method | What it does |
| --- | --- |
| `Set(field, value)` | Sets any [page field](page.md) by name. |
| `Add(type)` | Adds an element to the page's stacked content, by [type name](elements/index.md). |
| `AddBackground(type)` | Adds an element behind the page's content, placed by `Position`. |
| `AddForeground(type)` | Adds an element over the page's content, placed by `Position`. |
| `AddTitle(text)` | Shorthand for `Add("Title").Text(text)`. |
| `AddHeading(text)` | Shorthand for `Add("Heading").Text(text)`. |
| `AddParagraph(text)` | Shorthand for `Add("Paragraph").Text(text)`. |
| `AddBanner(text)` | Shorthand for `Add("Banner").Text(text)`. |
| `AddDivider()` | Shorthand for `Add("Divider")`. |
| `AddPanel()` | Shorthand for `Add("Panel")`. |
| `AddPageNumber()` | The page's own [number](elements/page-number.md), filled in from its position. |
| `AddImage(texturePath)` | Shorthand for `Add("Image").Texture(texturePath)`. |
| `AddItemImage(itemId)` | An image drawn from an item's icon, using a qualified ID such as `"(O)24"`. |
| `AddButton(text, action)` | A button running a [trigger action](../concepts/actions.md) when clicked. |
| `OnView(action)` | Runs a trigger action each time the page becomes visible. |
| `OnView(action, condition)` | The same, gated by a [game state query](../concepts/conditions.md). |
| `OnKeyPress(keybind, action)` | Runs a trigger action when the key is pressed while the page is visible, taking the key over from the menu and from the book's own binds. |
| `OnKeyPress(keybind, action, condition)` | The same, gated by a [game state query](../concepts/conditions.md). |

## The element builder

Most methods are named after the field they set, so anything you've written in a content pack carries over. The handful that aren't are the ones doing more than an assignment, such as `Margin`, `Spacing` and `AddFrame`.

| Method | Sets |
| --- | --- |
| `Set(field, value)` | Any [element field](elements/index.md) by name. |
| `WithId(id)` | `Id` |
| `Text(text)` | `Text` |
| `Alignment(alignment)` | `Alignment`, one of `"Left"`, `"Center"`, `"Right"` |
| `VerticalAlignment(alignment)` | `VerticalAlignment`, one of `"Top"`, `"Center"`, `"Bottom"`. Only used on a placed element |
| `TextAlignment(alignment)` | `TextAlignment`, one of `"Left"`, `"Center"`, `"Right"` |
| `Font(fontType)` | `FontType`, one of `"Dialogue"`, `"Small"`, `"Tiny"`, `"SpriteText"` |
| `TextColor(color)` | `TextColor` |
| `TextScale(scale)` | `TextScale` |
| `Scale(scale)` | `Scale` |
| `Rotation(rotation)` | `Rotation` |
| `Origin(x, y)` | `Origin` |
| `Position(x, y)` | `Position` |
| `Texture(path)` | `TexturePath` |
| `TextureSource(x, y, width, height)` | `TextureSourceRectangle` |
| `HoverTextureSource(x, y, width, height)` | `HoverTextureSourceRectangle` |
| `Tint(color)` | `TintColor` |
| `Item(itemId)` | `ItemId` |
| `Action(action)` / `Action(action, sound)` | A click [action](../concepts/actions.md). Call it more than once to build a list. |
| `HoverAction(action)` | A hover action. Call it more than once to build a list. |
| `Sound(sound)` | `Sound` |
| `Condition(condition)` | `Condition` |
| `Sizing(mode)` | `Sizing`, one of `"Fill"`, `"ShrinkToFit"`, `"Fixed"` |
| `Width(width)` / `Height(height)` | `Width` and `Height`. `Width` is taken by a [`Panel`](elements/panel.md), [`Divider`](elements/divider.md) or [`Banner`](elements/banner.md) with a `Fixed` `Sizing`, and by a [`Paragraph`](elements/paragraph.md) on its own. `Height` is a Panel's. |
| `Padding(padding)` | `Padding` |
| `Scope(scope)` | A PageNumber's `Scope`, either `"Book"` or `"Chapter"` |
| `Format(format)` | A PageNumber's `Format`, such as `"Page {0}"` |
| `Spacing(spacingAfter)` | `SpacingAfter` |
| `Margin(left, right)` | `MarginLeft` and `MarginRight` |
| `Tooltip(displayName, description)` | `DisplayName` and `Description` |
| `AddFrame(x, y, duration, scale, condition)` | An [animation frame](elements/image.md#frames) on an Image. Every argument after `y` is optional. |
| `AddFrameInPlace(duration, scale, condition)` | An animation frame that keeps whatever the element already draws. Every argument is optional. |
| `AddHoverFrame(x, y, duration, scale, condition)` | A [hover frame](elements/image.md#hover-frames), played while the cursor is over the element. |
| `AddHoverFrameInPlace(duration, scale, condition)` | A hover frame that keeps whatever the element already draws. |
| `AddChild(type)` | A child element on a container such as a Panel |
| `AddBackground(type)` | An element behind a container's children, placed by `Position` within its content area |
| `AddForeground(type)` | An element over a container's children, placed the same way |

Not every method applies to every element type. `Padding` on a `Heading` isn't valid, and asking for it fails at registration with a message naming the fields that type does accept.

## Setting anything else

The methods above cover the common fields. `Set` covers the rest, using the same field names the JSON uses:

```csharp
book.Set("Format", "1.4.0");
book.Set("Appearance.Scale", 4);
book.Set("Layout.MarginTop", 40);

cover.AddImage("you.CampingGuide/tent").Set("Rotation", 0.2f).Set("SpriteEffects", "FlipHorizontally");
```

Names are matched ignoring case, and enums are given as strings. A dotted path walks into a nested group, which is how the book's `Appearance`, `Layout`, `PageCurl` and `Animation` groups are reached.

Because `Set` reads the field names off the model, anything the JSON schema gains works here immediately, without waiting for a matching builder method.

## Running several actions

`Action` and `HoverAction` accumulate. Call either one repeatedly and you get a list run in order, so nothing changes about how you write the single-action case:

```csharp
page.AddButton("Take the map", "PeacefulEnd.Parchment_NextPage");

page.Add("Button")
    .Text("Accept the quest")
    .Texture("you.CampingGuide/button")
    .Action("AddQuest 101")
    .Action("AddMail Current you.QuestAccepted")
    .Action("PeacefulEnd.Parchment_CloseBook")
    .Sound("questcomplete");
```

Building the list in a loop works the same way, which is the case a content pack can't cover:

```csharp
IElementBuilder claim = page.AddButton("Claim rewards", "PeacefulEnd.Parchment_CloseBook");

foreach (string itemId in unclaimedRewards)
{
    claim.Action($"AddItem {itemId}");
}
```

The list runs to the end regardless of what happens partway, and there's no per-action condition. Since you're in C#, decide in code whether to add an action at all, and keep `If <query> ## <action>` for state that changes while the book is open.

`Sound` is separate because it plays once however many actions run. `Action(action, sound)` sets both in one call, for the common single-action button.

## Animating in code

Frames are added to an Image after its source rectangle, which is what gives them their size:

```csharp
IElementBuilder junimo = page.AddImage("Characters/Junimo").TextureSource(48, 0, 16, 16).Scale(4).Alignment("Center");

junimo.AddFrame(48, 0, 400);
junimo.AddFrame(64, 0, 400);
junimo.AddFrame(80, 0, 400, 1.1f);
```

Every argument after `y` is optional, so `AddFrame(48, 0)` is a frame at the element's own duration and scale. A `duration` of 0 means the same thing as omitting it: the element's `FrameDuration` applies.

`AddHoverFrame` builds the separate list played while the cursor is over the element. Leaving it empty means the idle animation just keeps running:

```csharp
junimo.AddHoverFrame(48, 0, 120, 1.15f);
junimo.AddHoverFrame(64, 0, 120);
```

`AddFrameInPlace` and `AddHoverFrameInPlace` are the same thing without a coordinate, for a frame that keeps whatever the element already draws and varies only its timing, scale or condition. That's what animates an [item icon](elements/image.md#animating-an-item), which has no source rectangle of your own to point at:

```csharp
IElementBuilder parsnip = page.AddItemImage("(O)24").Scale(4).Origin(8f, 8f).Alignment("Center");

parsnip.AddFrameInPlace(900);
parsnip.AddFrameInPlace(150, 1.1f);
parsnip.AddFrameInPlace(250);
```

A `condition` on a frame is a [game state query](../concepts/conditions.md), skipped rather than paused on when it fails. Since you're building in code you can often decide in C# whether to add the frame at all, which is clearer than a query. Reserve the condition for state that changes while the book is open:

```csharp
if (isNighttime is true)
{
    junimo.AddFrame(96, 0, 200);                              // decided now, at build time
}

junimo.AddFrame(112, 0, 200, 1f, "WEATHER Here Rain");       // re-checked while the book is open
```

## When something's wrong

Both `TryRegister` and `TryOpen` return `false` with an error rather than throwing, and the book is validated exactly the way a content pack's is.

```csharp
if (book.TryRegister(out string error) is false)
{
    Monitor.Log($"Couldn't register the camping guide, because {error}.", LogLevel.Warn);
}
```

Typical messages:

| Error | Cause |
| --- | --- |
| there's no element type named "Headding" | A typo in `Add`. |
| [Heading] there's no field named "Padding" on HeadingElementData. It accepts: ... | A field that type doesn't have. The list tells you what it does have. |
| [Image] "Alignment" expects one of Left, Center, Right but got "Centre" | A bad enum value. |
| "Pages" must contain at least one page | The usual book validation, same as content packs get. |
| there's more than one page with the ID "cover" | Duplicate page IDs within the book. |

## Rules and limits

| Rule | Behaviour |
| --- | --- |
| Prefix your book IDs with your mod's unique ID | Unprefixed IDs are accepted but logged as a warning. |
| Two mods, one book ID | The second mod is rejected with an error naming the first. |
| Same mod, same book ID | Your earlier registration is replaced. |
| Registering after launch | Supported. The books asset is rebuilt and Content Patcher edits are reapplied. |

Two things the builder can't do:

- **Content Patcher tokens.** `{{Season}}`, `{{i18n:key}}` and the rest are a Content Patcher feature, not a Parchment one. Substitute values yourself before building, or keep the book in a content pack.
- **Translations.** There's no `i18n` layer here. Pull strings from your own `Helper.Translation` as you build.
