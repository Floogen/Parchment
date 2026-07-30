using Microsoft.Xna.Framework;
using Parchment.Framework.API.Builders;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Books;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Menus;
using Parchment.Framework.UI.Rendering;
using Parchment.Framework.Utilities.Extensions;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Managers
{
    internal class BookManager : BaseManager
    {
        public const string BOOKS_DATA_PATH = "Data/PeacefulEnd.Parchment/Books";
        public const string SEEN_PAGES_DATA_PATH = "Data/PeacefulEnd.Parchment/SeenPages";
        public const string SEEN_CHAPTERS_DATA_PATH = "Data/PeacefulEnd.Parchment/SeenChapters";

        public List<BookData> Books { get { return _books; } set { FilterBookData(value); } }
        private List<BookData> _books = new List<BookData>();

        // The last validation error per book ID for those that were dropped by FilterBookData
        private readonly Dictionary<string, string> _bookIdToValidationError = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Books registered through the C# API, keyed by the owning mod's unique ID then the book's ID. The builder is kept rather than the
        // built data, so every asset load produces a fresh graph and Content Patcher's edits can't accumulate on the registered original.
        private readonly Dictionary<string, Dictionary<string, BookBuilder>> _modIdToRegisteredBooks = new Dictionary<string, Dictionary<string, BookBuilder>>(StringComparer.OrdinalIgnoreCase);

        // Whether the books asset has been loaded at least once, so registrations made before then don't need to invalidate it
        private bool _hasLoadedBooks = false;

        // A requested book to be opened (if this fails, the book request is discarded)
        private string? _requestedBookId = null;

        private Dictionary<string, List<string>> _playerToSeenPages { get; set; } = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> _playerToSeenChapters { get; set; } = new Dictionary<string, List<string>>();

        public ElementRegistry ElementRegistry { get; }
        public FontResolver FontResolver { get; }

        public BookManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            // Register default helpers
            ElementRegistry = new ElementRegistry(registerDefaults: true);
            FontResolver = new FontResolver();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetInvalidated;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
            _hasLoadedBooks = true;

            _playerToSeenPages = helper.GameContent.Load<Dictionary<string, List<string>>>(SEEN_PAGES_DATA_PATH);
            _playerToSeenChapters = helper.GameContent.Load<Dictionary<string, List<string>>>(SEEN_CHAPTERS_DATA_PATH);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOKS_DATA_PATH))
            {
                e.LoadFrom(CreateRegisteredBookList, AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(SEEN_PAGES_DATA_PATH))
            {
                e.LoadFrom(() => _playerToSeenPages, AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(SEEN_CHAPTERS_DATA_PATH))
            {
                e.LoadFrom(() => _playerToSeenChapters, AssetLoadPriority.Medium);
            }
        }

        private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            var books = e.NamesWithoutLocale.FirstOrDefault(a => a.IsEquivalentTo(BOOKS_DATA_PATH));
            if (books is not null)
            {
                Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
            }
            var seenPages = e.NamesWithoutLocale.FirstOrDefault(a => a.IsEquivalentTo(SEEN_PAGES_DATA_PATH));
            if (seenPages is not null)
            {
                _playerToSeenPages = helper.GameContent.Load<Dictionary<string, List<string>>>(SEEN_PAGES_DATA_PATH);
            }
            var seenChapters = e.NamesWithoutLocale.FirstOrDefault(a => a.IsEquivalentTo(SEEN_CHAPTERS_DATA_PATH));
            if (seenChapters is not null)
            {
                _playerToSeenChapters = helper.GameContent.Load<Dictionary<string, List<string>>>(SEEN_CHAPTERS_DATA_PATH);
            }

            if (Game1.activeClickableMenu is BookMenu bookMenu)
            {
                bookMenu.Book.RefreshTextures(e.NamesWithoutLocale);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_requestedBookId is null)
            {
                return;
            }

            // Taken and cleared up front, so a request that can't open now is spent either way
            string bookId = _requestedBookId;
            _requestedBookId = null;

            if (CanOpenRequestedBook() is false)
            {
                return;
            }

            if (CreateBook(bookId) is not Book book)
            {
                return;
            }

            Game1.activeClickableMenu = new BookMenu(book);
        }

        public void RequestOpenBook(string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId) is true)
            {
                return;
            }

            _requestedBookId = bookId;
        }

        public void CancelRequestedBook()
        {
            _requestedBookId = null;
        }

        private static bool CanOpenRequestedBook()
        {
            if (Context.IsWorldReady is false || Game1.currentLocation is null)
            {
                return false;
            }

            // Check if a menu is currently open (bail if so)
            if (Game1.activeClickableMenu is not null)
            {
                return false;
            }

            // Check if currently warping (bail if so)
            if (Game1.locationRequest is not null || Game1.fadeToBlack is true)
            {
                return false;
            }

            // Check if event or anything else is going on (...bail if so)
            if (Game1.eventUp is true || Game1.currentMinigame is not null || Game1.farmEvent is not null)
            {
                return false;
            }

            return true;
        }

        private void FilterBookData(List<BookData> bookData)
        {
            var validBooks = new List<BookData>();
            _bookIdToValidationError.Clear();

            foreach (var book in bookData)
            {
                var isValidData = book.IsValid();
                if (isValidData.Result is false)
                {
                    _bookIdToValidationError[book.Id ?? string.Empty] = isValidData.Error;
                    monitor.LogOnce($"Skipping invalid BookData with name \"{book.Id}\": {isValidData.Error}", LogLevel.Warn);
                    continue;
                }

                validBooks.Add(book);
            }

            _books = validBooks;
        }

        /// <summary>Gets why a book was rejected during validation, so callers can be told rather than only the log.</summary>
        public bool TryGetValidationError(string bookId, out string error)
        {
            return _bookIdToValidationError.TryGetValue(bookId ?? string.Empty, out error);
        }

        /// <summary>Registers a book built through the C# API on behalf of a mod, replacing that mod's previous registration for the same book ID.</summary>
        /// <param name="modId">The unique ID of the mod registering the book.</param>
        /// <param name="builder">The book's builder, which is kept so the book can be rebuilt on each asset load.</param>
        /// <param name="error">Why the book was rejected, when this returns false.</param>
        public bool TryRegisterBook(string modId, BookBuilder builder, out string error)
        {
            if (string.IsNullOrWhiteSpace(modId) is true)
            {
                error = "no owning mod ID was given";
                return false;
            }

            if (builder.TryBuildValidated(out BookData bookData, out error) is false)
            {
                monitor.Log($"{modId} failed to register the book \"{builder.BookId}\", because {error}", LogLevel.Warn);
                return false;
            }

            if (TryGetOwningModId(bookData.Id, out string owningModId) is true && owningModId.EqualsIgnoreCase(modId) is false)
            {
                error = $"the book ID \"{bookData.Id}\" is already registered by {owningModId}";
                monitor.Log($"{modId} failed to register a book, because {error}.", LogLevel.Warn);
                return false;
            }

            if (bookData.Id.StartsWith(modId, StringComparison.OrdinalIgnoreCase) is false)
            {
                monitor.LogOnce($"{modId} registered the book \"{bookData.Id}\", whose ID isn't prefixed with its mod ID. Prefixed IDs keep books from colliding between mods.", LogLevel.Warn);
            }

            if (_modIdToRegisteredBooks.ContainsKey(modId) is false)
            {
                _modIdToRegisteredBooks[modId] = new Dictionary<string, BookBuilder>(StringComparer.OrdinalIgnoreCase);
            }
            _modIdToRegisteredBooks[modId][bookData.Id] = builder;

            RefreshBooksAsset();

            return true;
        }

        /// <summary>Removes a book previously registered by the given mod. A mod can only remove its own books.</summary>
        public bool TryUnregisterBook(string modId, string bookId, out string error)
        {
            if (string.IsNullOrWhiteSpace(bookId) is true)
            {
                error = "no book ID was given";
                return false;
            }

            if (_modIdToRegisteredBooks.TryGetValue(modId, out var registeredBooks) is false || registeredBooks.Remove(bookId) is false)
            {
                error = $"{modId} hasn't registered a book with the ID \"{bookId}\"";
                return false;
            }

            RefreshBooksAsset();
            error = string.Empty;

            return true;
        }

        /// <summary>Gets whether a book with the given ID is currently loaded, whether it came from a content pack or the C# API.</summary>
        public bool HasBook(string bookId)
        {
            if (string.IsNullOrWhiteSpace(bookId) is true)
            {
                return false;
            }

            return Books.Any(book => book.Id.EqualsIgnoreCase(bookId));
        }

        // Builds the base books asset from the C# registrations. Content Patcher edits are applied on top of this, so registered books
        // stay patchable by content packs.
        private List<BookData> CreateRegisteredBookList()
        {
            var books = new List<BookData>();

            foreach (var modEntry in _modIdToRegisteredBooks)
            {
                foreach (var bookEntry in modEntry.Value)
                {
                    if (bookEntry.Value.TryBuildValidated(out BookData bookData, out string error) is false)
                    {
                        monitor.LogOnce($"Skipping the book \"{bookEntry.Key}\" registered by {modEntry.Key}, because {error}", LogLevel.Warn);
                        continue;
                    }

                    books.Add(bookData);
                }
            }

            return books;
        }

        // Finds which mod owns a registered book ID, if any
        private bool TryGetOwningModId(string bookId, out string modId)
        {
            foreach (var modEntry in _modIdToRegisteredBooks)
            {
                if (modEntry.Value.ContainsKey(bookId) is true)
                {
                    modId = modEntry.Key;
                    return true;
                }
            }

            modId = string.Empty;

            return false;
        }

        // Rebuilds the books asset so registrations made after launch take effect, which also reapplies any Content Patcher edits
        private void RefreshBooksAsset()
        {
            if (_hasLoadedBooks is false)
            {
                return;
            }

            helper.GameContent.InvalidateCache(BOOKS_DATA_PATH);
        }

        public bool HasSeenChapter(Farmer who, string bookId, string chapter)
        {
            if (_playerToSeenChapters.TryGetValue(who.Name, out var seenChapters) is false || seenChapters is null)
            {
                return false;
            }

            return seenChapters.Any(c => c.EqualsIgnoreCase($"{bookId}.{chapter}"));
        }

        public bool HasSeenPage(Farmer who, string bookId, string chapter, string pageId)
        {
            if (_playerToSeenPages.TryGetValue(who.Name, out var seenPages) is false || seenPages is null)
            {
                return false;
            }

            return seenPages.Any(c => c.EqualsIgnoreCase($"{bookId}.{chapter}.{pageId}"));
        }

        public bool HasSeenChapterlessPage(Farmer who, string bookId, string pageId)
        {
            return HasSeenPage(who, bookId, string.Empty, pageId);
        }

        public void SetSeenChapter(Farmer who, string bookId, string chapter)
        {
            if (_playerToSeenChapters.ContainsKey(who.Name) is false)
            {
                _playerToSeenChapters[who.Name] = new List<string>();
            }

            _playerToSeenChapters[who.Name].Add($"{bookId}.{chapter}");
        }

        public void SetSeenPage(Farmer who, string bookId, string chapter, string pageId)
        {
            if (_playerToSeenPages.ContainsKey(who.Name) is false)
            {
                _playerToSeenPages[who.Name] = new List<string>();
            }

            _playerToSeenPages[who.Name].Add($"{bookId}.{chapter}.{pageId}");
        }

        public void ClearSeen(Farmer who)
        {
            _playerToSeenPages[who.Name] = new List<string>();
            _playerToSeenChapters[who.Name] = new List<string>();
        }

        public bool TryGetBookId(string qualifiedItemId, out string? bookId)
        {
            bookId = null;

            ParsedItemData itemData = ItemRegistry.GetData(qualifiedItemId);
            if (itemData?.RawData is not ObjectData objectData)
            {
                return false;
            }

            if (objectData.CustomFields is null)
            {
                return false;
            }

            return objectData.CustomFields.TryGetValue(Parchment.CUSTOM_BOOK_FIELD_ID, out bookId);
        }

        public Book? CreateBook(string bookDataId)
        {
            var bookData = Books.FirstOrDefault(b => b.Id.EqualsIgnoreCase(bookDataId));
            if (bookData is null)
            {
                return null;
            }

            return CreateBook(bookData);
        }

        public Book? CreateBook(BookData bookData)
        {
            return new Book(bookData, ElementRegistry, FontResolver);
        }
    }
}
