# C# API

This page is for **SMAPI mod authors** who want to open a book from their own code. If you're using Content Patcher, see [Opening a book](../concepts/opening-books.md) instead.

## Getting the API

Copy this interface into your mod:

```csharp
public interface IParchmentApi
{
    /// <summary>Opens a book, optionally at a chapter.</summary>
    bool TryOpenBook(string bookId, string chapterId = null);

    /// <summary>Opens a book at a page number.</summary>
    bool TryOpenBookAtPage(string bookId, string chapterId, int page);

    /// <summary>Opens a book at a page's PageData.Id.</summary>
    bool TryOpenBookAtPageId(string bookId, string chapterId, string pageId);
}
```

Then fetch it once both mods have loaded in `GameLaunched`:

```csharp
private IParchmentApi parchment;

public override void Entry(IModHelper helper)
{
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
}

private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
{
    parchment = Helper.ModRegistry.GetApi<IParchmentApi>("PeacefulEnd.Parchment.Core");
}
```

!!! warning "Use the full unique ID"
    Parchment's `UniqueID` is `PeacefulEnd.Parchment.Core`. `GetApi` returns `null` when the ID doesn't match, and a `null` API is easy to mistake for Parchment not being installed. The same ID goes in your manifest's `Dependencies`.

## Opening a book

```csharp
// The book's first page.
parchment?.TryOpenBook("YourMod_FieldGuide");

// The first page of a chapter.
parchment?.TryOpenBook("YourMod_FieldGuide", "appendix");

// A specific page, by its PageData.Id.
parchment?.TryOpenBookAtPageId("YourMod_FieldGuide", "appendix", "mushrooms");

// A specific page, searching every chapter for the ID.
parchment?.TryOpenBookAtPageId("YourMod_FieldGuide", null, "mushrooms");

// A chapter-relative page number (0-based).
parchment?.TryOpenBookAtPage("YourMod_FieldGuide", "appendix", 2);

// A page number counted across the whole book (0-based).
parchment?.TryOpenBookAtPage("YourMod_FieldGuide", null, 5);
```

### Parameters

| Parameter | Type | Meaning |
| --- | --- | --- |
| `bookId` | string | The book's `Id` from `Data/PeacefulEnd.Parchment/Books` (its `BookData.Id`), **not** the qualified item ID. |
| `chapterId` | string | A page's `ChapterId`. Pass `null` to work across the whole book. |
| `pageId` | string | A page's `Id` (its `PageData.Id`). Scoped to `chapterId` when you pass one. |
| `page` | int | A 0-based page number, relative to `chapterId` when you pass one and to the whole book otherwise. |

Every method returns `true` when the book was found and opened and `false` when the book, chapter or page couldn't be resolved. Failures are logged with the reason, including when a book was dropped during loading because its data was invalid.
