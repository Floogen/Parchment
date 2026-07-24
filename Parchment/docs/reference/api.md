# C# API

This page is for **SMAPI mod authors** who want to open a book from their own code. If you're using Content Patcher, see [Opening a book](../concepts/opening-books.md) instead.

## Getting the API

Copy this interface into your mod:

```csharp
public interface IParchmentApi
{
    /// <summary>Opens a book at a chapter-relative page number.</summary>
    bool TryOpenBook(string bookId, string chapterId, int page);

    /// <summary>Opens a book, optionally at a chapter and/or a page by its PageData.Id.</summary>
    bool TryOpenBook(string bookId, string chapterId = null, string pageId = null);
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

## Opening a book

```csharp
// The book's first page.
parchment?.TryOpenBook("YourMod_FieldGuide");

// The first page of a chapter.
parchment?.TryOpenBook("YourMod_FieldGuide", "appendix");

// A specific page, by its PageData.Id.
parchment?.TryOpenBook("YourMod_FieldGuide", "appendix", "mushrooms");

// A page by chapter-relative number (0-based).
parchment?.TryOpenBook("YourMod_FieldGuide", "appendix", 2);
```

### Parameters

| Parameter | Type | Meaning |
| --- | --- | --- |
| `bookId` | string | The book's `Id` from `Data/PeacefulEnd.Parchment/Books` (its `BookData.Id`), **not** the qualified item ID. |
| `chapterId` | string | A page's `ChapterId`, or `null` for the whole book. |
| `pageId` | string | A page's `Id` (its `PageData.Id`). Scoped to `chapterId` when you pass one. |
| `page` | int | A chapter-relative page number, 0-based. |

Both overloads return `true` when the book was found and opened and `false` when the book, chapter or page couldn't be resolved.