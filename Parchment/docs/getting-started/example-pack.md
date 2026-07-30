# The example pack

Parchment ships with a working Content Patcher pack, [`[CP] Parchment Example Pack`](https://github.com/Floogen/Parchment/tree/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack), which registers two books that between them cover most of the framework. Everything below is real, running JSON rather than a fragment, so it's the place to look when a snippet elsewhere in these docs leaves you wondering how it fits into a whole file.

Both books live in one file: [`parchment/books.json`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/parchment/books.json).

| File | What it does |
| --- | --- |
| [`manifest.json`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/manifest.json) | Declares `PeacefulEnd.Parchment.Core` as a required dependency. |
| [`content.json`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/content.json) | One `Load` patch for all of the pack's art, then an `Include` pulling in the books. |
| [`parchment/books.json`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/parchment/books.json) | The item that opens a book, both `BookData` entries and a commented-out [book indicator](../concepts/indicators.md) patch. |
| [`assets/`](https://github.com/Floogen/Parchment/tree/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets) | The pack's PNGs, each alongside the Aseprite file it came from. |

## Opening them

The Guide Book is attached to an item, `PeacefulEnd.Parchment_Book`, through the `PeacefulEnd.Parchment/CustomFields/BookId` custom field. The Notebook has no opener of its own, so the console is the way in:

```
parchment_open PeacefulEnd.Parchment.ExamplePack_Notebook
parchment_open PeacefulEnd.Parchment.ExamplePack_GuideBook
```

Pair that with `parchment_debug` to see every element's bounds. See [Opening a book](../concepts/opening-books.md) for the other routes in.

---

## The Notebook

`PeacefulEnd.Parchment.ExamplePack_Notebook` is the short one, and it's here for its **[appearance](../reference/book.md#appearance)**: it's the same page content in a different book.

```json title="parchment/books.json"
"Appearance": {
  "TexturePath": "Assets/PeacefulEnd.Parchment/notebook",
  "GrayscaleTexturePath": "Assets/PeacefulEnd.Parchment/notebookGrayscale",
  "FrameWidth": 219,
  "FrameHeight": 143,
  "OpenFrameCount": 6,
  "TurnFrameCount": 7,
  "TintColor": "165 42 42"
},
"PageCurl": {
  "TexturePath": "Assets/PeacefulEnd.Parchment/curlPage2",
  "PreviousPageOffset": { "X": 5, "Y": 105 },
  "NextPageOffset": { "X": 182, "Y": 105 }
}
```

The frame counts are the part worth copying. This sheet has six open frames and seven turn frames rather than the built-in book's four and six, and getting those wrong is what makes a book animate to the wrong frame or flicker on a page turn. The `PageCurl` offsets are tuned to this art too, since the corners sit in different places on a notebook than on a bound book.

Its three pages are a quick tour rather than a demonstration of anything in particular:

| Page | What's on it |
| --- | --- |
| 1 | A title drawn as an [`Image`](../reference/elements/image.md) with `TextArea` text over it, a `Heading` and a `Fixed`-width [`Panel`](../reference/elements/panel.md). |
| 2 | A [`Button`](../reference/elements/button.md) running one `Action` and two more in `Actions`, with a hover swap and a `Sound`. |
| 3 | An animated sprite using both `Frames` and `HoverFrames`, pivoting around a centred `Origin`. |

Each page also places a [`PageNumber`](../reference/elements/page-number.md) in its `Background`, alternating between the left and right corners so the numbers land on the outside edge.

---

## The Guide Book

`PeacefulEnd.Parchment.ExamplePack_GuideBook` is the more robust example. Its `Appearance` sets nothing but a colour:

```json title="parchment/books.json"
"Appearance": {
  "TintColor": "165 42 42"
}
```

That's the built-in book art recoloured through its greyscale layer, which is all a book needs.

### Book-wide layers

`Underlay` and `Overlay` sit behind and in front of the book itself on every page, and the Guide Book uses both.

Two Junimos peek out from behind the book, one on each side, each conditioned on the book state and the page index so they only appear mid-turn:

```json title="parchment/books.json"
"Condition": "PeacefulEnd.Parchment_CurrentBookState Turning, PeacefulEnd.Parchment_CurrentPageIndex 0"
```

Two bookmarks hang off the edges as `Overlay` elements, one jumping to the first page and one to the last, each hidden when it would be pointless:

```json title="parchment/books.json"
{
  "Type": "Image",
  "TexturePath": "Assets/PeacefulEnd.Parchment/bookmark1",
  "TextureSourceRectangle": { "X": 0, "Y": 0, "Width": 24, "Height": 17 },
  "HoverTextureSourceRectangle": { "X": 0, "Y": 17, "Width": 24, "Height": 17 },
  "TintColor": "255 0 0",
  "SpriteEffects": "FlipHorizontally",
  "Position": { "X": -64, "Y": 192 },
  "Scale": 4,
  "Action": "PeacefulEnd.Parchment_FirstPage",
  "Condition": "PeacefulEnd.Parchment_CurrentBookState Ready, !PeacefulEnd.Parchment_CurrentPageIndex 0"
}
```

One sprite, one hover frame, one action and a condition. That's the whole bookmark. `SpriteEffects` mirrors the same art for the left-hand side rather than needing a second drawing.

### The pages

| Page | Chapter | What it demonstrates |
| --- | --- | --- |
| `cover` | `chapter-1` | A banner title, a `Fixed` panel and a button carrying several [trigger actions](../concepts/actions.md) at once. |
| `info` | `chapter-1` | A `ShrinkToFit` [`Banner`](../reference/elements/banner.md), a `Paragraph` nudged with `MarginLeft` and a `Fill` panel with a set height whose children include a hover `Description`. |
| `test` | `chapter-1` | Alignment and margins on headings, both textured and textureless [`Divider`](../reference/elements/divider.md)s, a custom `PageNumber` `Format` and an `OnView` trigger that pays out once. |
| `huh` | `chapter-1` | `Background` elements: art drawn under the page content, plus background text carrying its own tooltip. |
| `animated` | `chapter-1` | Three animations side by side, including per-frame `Scale` and a `HoverFrames` swap. See [Frames](../reference/elements/image.md#frames). |
| `items` | `chapter-1` | [`ItemId`](../reference/elements/image.md) icons, one taking its tooltip from the item and one overriding it. |
| `last?` | `chapter-1` | A button running `PeacefulEnd.Parchment_JumpToChapter`. |
| `hopped` | `chapter-2` | A chapter-scoped page number, which restarts its count inside the chapter. |
| `last!` | `chapter-2` | A book-scoped page number on the same spread, and a button back to the start. |

!!! tip "The chapter split is the interesting bit"
    `chapter-1` and `chapter-2` are two [chapters](../reference/page.md#chapters) in one book, and the only way between them is the buttons on `last?` and `last!`. Turning pages won't cross the boundary, which is what makes a table of contents or an appendix possible.

The `OnView` block on `test` is worth reading in full, since it's the pattern for a page that does something the first time it's seen:

```json title="parchment/books.json"
"OnView": [
  {
    "Condition": "!PLAYER_HAS_MAIL Current PeacefulEnd.Parchment_ExampleMailIdTest Any",
    "Actions": [ "AddMoney 500", "AddMail Current PeacefulEnd.Parchment_ExampleMailIdTest All" ]
  }
]
```

The mail flag is doing the remembering. The actions add it, and the condition checks for it, so the payout happens once however many times the page is turned to.

---

## Where the art lives

The book frames come from the core mod, since they're the built-in art every book falls back to. Everything drawn on a page comes from the content pack.

### Book and page-curl art

In [`Parchment/Framework/Assets/`](https://github.com/Floogen/Parchment/tree/development/Parchment/Framework/Assets):

| `TexturePath` | File | Used by |
| --- | --- | --- |
| `Assets/PeacefulEnd.Parchment/smallBook` | [`smallBook.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Framework/Assets/smallBook.png) | The Guide Book, as the default `Appearance.TexturePath`. |
| `Assets/PeacefulEnd.Parchment/smallBookGrayscale` | [`smallBookGrayscale.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Framework/Assets/smallBookGrayscale.png) | The layer the Guide Book's `TintColor` is multiplied into. |
| `Assets/PeacefulEnd.Parchment/curlPage` | [`curlPage.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Framework/Assets/curlPage.png) | The Guide Book's page corners, by default. |
| `Assets/PeacefulEnd.Parchment/notebook` | [`notebook.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Framework/Assets/notebook.png) | The Notebook's `Appearance.TexturePath`. |
| `Assets/PeacefulEnd.Parchment/notebookGrayscale` | [`notebookGrayscale.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Framework/Assets/notebookGrayscale.png) | The Notebook's `GrayscaleTexturePath`. |
| `Assets/PeacefulEnd.Parchment/curlPage2` | [`curlPage2.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Framework/Assets/curlPage2.png) | The Notebook's `PageCurl.TexturePath`. |

!!! note "These are game assets, not files in the pack"
    The example pack refers to `Assets/PeacefulEnd.Parchment/notebook` without loading anything, because Parchment loads it. Your own book can use the same names, and another mod retexturing them changes both. See [Loading your art](first-book.md#loading-your-art).

### Page art

In [`assets/`](https://github.com/Floogen/Parchment/tree/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets), loaded by the pack's one `Load` patch:

| `TexturePath` | File | Drawn as |
| --- | --- | --- |
| `Assets/PeacefulEnd.Parchment/bannerTitle1` | [`bannerTitle1.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/bannerTitle1.png) | The stretching `Banner` with its `CapWidth` ends. |
| `Assets/PeacefulEnd.Parchment/bannerTitle2` | [`bannerTitle2.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/bannerTitle2.png) | The fixed title plate on both covers, with its text in a `TextArea`. |
| `Assets/PeacefulEnd.Parchment/button1` | [`button1.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/button1.png) | Every `Button`, with the hover state on the row below. |
| `Assets/PeacefulEnd.Parchment/panelFrame2` | [`panelFrame2.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/panelFrame2.png) | Every `Panel`, sliced nine ways from a 24×24 source. |
| `Assets/PeacefulEnd.Parchment/divider1` | [`divider1.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/divider1.png) | The lower rule on `test`. |
| `Assets/PeacefulEnd.Parchment/divider2` | [`divider2.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/divider2.png) | The upper rule on `test`. |
| `Assets/PeacefulEnd.Parchment/bookmark1` | [`bookmark1.PNG`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/bookmark1.PNG) | Both `Overlay` bookmarks, mirrored and tinted from one sprite. |
| `Assets/PeacefulEnd.Parchment/itemBorder1` | [`itemBorder1.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/itemBorder1.png) | The slot behind the item icon on `items`, placed in the `Background`. |
| `Assets/PeacefulEnd.Parchment/backgroundNoise1` | [`backgroundNoise1.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/backgroundNoise1.png) | The texture behind the panel on `huh`. |
| `Assets/PeacefulEnd.Parchment/exampleBookIcon` | [`exampleBookIcon.png`](https://github.com/Floogen/Parchment/blob/development/Parchment/Content%20Packs/%5BCP%5D%20Parchment%20Example%20Pack/assets/exampleBookIcon.png) | The item's inventory sprite, through `Data/Objects`. |

The pack loads a few more sheets than it draws (`panelFrame1`, `imageBackground1` and `imageBackground2`), which are there to swap in while you're experimenting.

Vanilla art turns up as well, needing no `Load` patch of its own. `LooseSprites/GemBird` and `LooseSprites/Cursors2` animate on `animated`, `Characters/Junimo` appears both there and as the two `Underlay` peekers, and `LooseSprites/Cursors_1_6` is the sprite drawn on `test` and again inside the panel on `huh`.

!!! tip "Any loaded texture is fair game"
    A `TexturePath` is a game asset name, not a path inside your pack, so vanilla sheets need no `Load` patch and another mod's assets work too as long as that mod is installed. Reaching into a mod you don't depend on is how you end up with an element that logs a warning and draws nothing on someone else's setup.

---

## Using it as a starting point

Copy the folder into `Mods`, rename it, and change the `UniqueID` in the manifest along with both book IDs. From there the fastest loop is to edit `books.json`, run `patch reload <your mod id>` in the console and reopen the book. Parchment reloads books from the asset, so a page usually redraws without restarting the game.
