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

        // Reading history rides on the player rather than in a data asset, so the game saves it and each save file keeps its own
        public const string SEEN_PAGES_MOD_DATA_KEY = "PeacefulEnd.Parchment/SeenPages";
        public const string SEEN_CHAPTERS_MOD_DATA_KEY = "PeacefulEnd.Parchment/SeenChapters";

        // Chosen because a book, chapter or page ID can contain a dot but not this
        private const char SEEN_SEPARATOR = '|';

        public List<BookData> Books { get { return _books; } set { FilterBookData(value); } }
        private List<BookData> _books = new List<BookData>();

        // The last validation error per book ID for those that were dropped by FilterBookData
        private readonly Dictionary<string, string> _bookIdToValidationError = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Books registered through the C# API, keyed by the owning mod's unique ID then the book's ID. The builder is kept rather than the
        // built data, so every asset load produces a fresh graph and Content Patcher's edits can't accumulate on the registered original.
        private readonly Dictionary<string, Dictionary<string, BookBuilder>> _modIdToRegisteredBooks = new Dictionary<string, Dictionary<string, BookBuilder>>(StringComparer.OrdinalIgnoreCase);

        // Every builder handed out by CreateBook, keyed by the book ID it was created for. A book being assembled right now isn't in the asset yet,
        // so this is what lets its variable declarations answer before the terminal call that would put it there.
        private readonly Dictionary<string, BookBuilder> _bookIdToLiveBuilder = new Dictionary<string, BookBuilder>(StringComparer.OrdinalIgnoreCase);

        // Whether the books asset has been loaded at least once, so registrations made before then don't need to invalidate it
        private bool _hasLoadedBooks = false;
        private bool _hasPendingBookReload = false;

        // A requested book to be opened (if this fails, the book request is discarded)
        private string? _requestedBookId = null;

        // Parsed views of what each farmer has read, so a condition refresh reads a set rather than splitting a stored string dozens of times a second.
        // Every change is written back to modData on the spot, so these are a cache and never the record.
        private readonly Dictionary<long, HashSet<string>> _playerToSeenPages = new Dictionary<long, HashSet<string>>();
        private readonly Dictionary<long, HashSet<string>> _playerToSeenChapters = new Dictionary<long, HashSet<string>>();

        public ElementRegistry ElementRegistry { get; }
        public FontResolver FontResolver { get; }

        public BookManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            // Register default helpers
            ElementRegistry = new ElementRegistry(registerDefaults: true);
            FontResolver = new FontResolver();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetInvalidated;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
            _hasLoadedBooks = true;
        }

        // The cached history belongs to the save being left, and a farmer's ID is only unique within one
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            _playerToSeenPages.Clear();
            _playerToSeenChapters.Clear();
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOKS_DATA_PATH))
            {
                e.LoadFrom(CreateRegisteredBookList, AssetLoadPriority.Medium);
            }
        }

        private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            var books = e.NamesWithoutLocale.FirstOrDefault(a => a.IsEquivalentTo(BOOKS_DATA_PATH));
            if (books is not null)
            {
                // Reloading under a reader would throw away their page, their scroll and anything typed into an input.
                // Single player can't hit this, as an open menu pauses the clock Content Patcher updates on, but multiplayer keeps ticking.
                if (Game1.activeClickableMenu is BookMenu)
                {
                    _hasPendingBookReload = true;
                }
                else
                {
                    Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
                }
            }
            if (Game1.activeClickableMenu is BookMenu bookMenu)
            {
                bookMenu.Book.RefreshTextures(e.NamesWithoutLocale);
            }
        }

        /// <summary>Takes up a reload that was held back while a book was open. Called when the menu closes, since that is when the reader has nothing left to lose.</summary>
        public void ApplyPendingBookReload()
        {
            if (_hasPendingBookReload is false)
            {
                return;
            }

            _hasPendingBookReload = false;
            Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
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

        /// <summary>Records a builder against the book ID it was created for, so what it declares can be found before it's registered or opened.
        /// Keyed by book ID, so a mod rebuilding the same book replaces its earlier builder rather than stacking up another one.
        /// </summary>
        public void TrackLiveBuilder(BookBuilder builder)
        {
            if (string.IsNullOrWhiteSpace(builder.BookId) is true)
            {
                return;
            }

            // A book ID another mod already registered stays theirs, so a builder can't answer for a book it doesn't own
            if (TryGetOwningModId(builder.BookId, out string owningModId) is true && owningModId.EqualsIgnoreCase(builder.ModId) is false)
            {
                return;
            }

            _bookIdToLiveBuilder[builder.BookId] = builder;
        }

        /// <summary>The most recent builder created for a book ID, whether or not anything has been done with it yet.</summary>
        public bool TryGetLiveBuilder(string bookId, out BookBuilder builder)
        {
            return _bookIdToLiveBuilder.TryGetValue(bookId, out builder!);
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

            _bookIdToLiveBuilder.Remove(bookId);

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
            return GetSeen(who, _playerToSeenChapters, SEEN_CHAPTERS_MOD_DATA_KEY).Contains($"{bookId}.{chapter}");
        }

        public bool HasSeenPage(Farmer who, string bookId, string chapter, string pageId)
        {
            return GetSeen(who, _playerToSeenPages, SEEN_PAGES_MOD_DATA_KEY).Contains($"{bookId}.{chapter}.{pageId}");
        }

        public bool HasSeenChapterlessPage(Farmer who, string bookId, string pageId)
        {
            return HasSeenPage(who, bookId, string.Empty, pageId);
        }

        public void SetSeenChapter(Farmer who, string bookId, string chapter)
        {
            SetSeen(who, _playerToSeenChapters, SEEN_CHAPTERS_MOD_DATA_KEY, $"{bookId}.{chapter}");
        }

        public void SetSeenPage(Farmer who, string bookId, string chapter, string pageId)
        {
            SetSeen(who, _playerToSeenPages, SEEN_PAGES_MOD_DATA_KEY, $"{bookId}.{chapter}.{pageId}");
        }

        /// <summary>Forgets a single chapter, so the next reading counts as the first.</summary>
        public void ClearSeenChapter(Farmer who, string bookId, string chapter)
        {
            ClearSeen(who, _playerToSeenChapters, SEEN_CHAPTERS_MOD_DATA_KEY, entry => entry.EqualsIgnoreCase($"{bookId}.{chapter}"));
        }

        /// <summary>Forgets a single page.</summary>
        public void ClearSeenPage(Farmer who, string bookId, string chapter, string pageId)
        {
            ClearSeen(who, _playerToSeenPages, SEEN_PAGES_MOD_DATA_KEY, entry => entry.EqualsIgnoreCase($"{bookId}.{chapter}.{pageId}"));
        }

        /// <summary>Forgets everything a player has read, or everything from one book when a book ID is given.</summary>
        public void ClearSeen(Farmer who, string? bookId = null)
        {
            Func<string, bool> matches = bookId is null ? _ => true : entry => entry.StartsWith($"{bookId}.", StringComparison.OrdinalIgnoreCase);

            ClearSeen(who, _playerToSeenPages, SEEN_PAGES_MOD_DATA_KEY, matches);
            ClearSeen(who, _playerToSeenChapters, SEEN_CHAPTERS_MOD_DATA_KEY, matches);
        }

        // Reads a player's history out of their modData the first time it is asked for, then answers from the parsed set
        private static HashSet<string> GetSeen(Farmer who, Dictionary<long, HashSet<string>> cache, string modDataKey)
        {
            if (cache.TryGetValue(who.UniqueMultiplayerID, out HashSet<string>? cached) is true)
            {
                return cached;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (who.modData.TryGetValue(modDataKey, out string? stored) is true && string.IsNullOrEmpty(stored) is false)
            {
                foreach (string entry in stored.Split(SEEN_SEPARATOR, StringSplitOptions.RemoveEmptyEntries))
                {
                    seen.Add(entry);
                }
            }

            cache[who.UniqueMultiplayerID] = seen;

            return seen;
        }

        private static void SetSeen(Farmer who, Dictionary<long, HashSet<string>> cache, string modDataKey, string entry)
        {
            HashSet<string> seen = GetSeen(who, cache, modDataKey);

            // Nothing to write when the entry was already there, which is what keeps a reader lingering on a page off the save
            if (seen.Add(entry) is false)
            {
                return;
            }

            Store(who, seen, modDataKey);
        }

        private static void ClearSeen(Farmer who, Dictionary<long, HashSet<string>> cache, string modDataKey, Func<string, bool> matches)
        {
            HashSet<string> seen = GetSeen(who, cache, modDataKey);

            if (seen.RemoveWhere(entry => matches(entry)) is 0)
            {
                return;
            }

            Store(who, seen, modDataKey);
        }

        private static void Store(Farmer who, HashSet<string> seen, string modDataKey)
        {
            if (seen.Count is 0)
            {
                who.modData.Remove(modDataKey);
                return;
            }

            who.modData[modDataKey] = string.Join(SEEN_SEPARATOR, seen);
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
