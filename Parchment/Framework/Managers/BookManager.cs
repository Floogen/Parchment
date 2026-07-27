using Microsoft.Xna.Framework;
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
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetInvalidated;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);

            _playerToSeenPages = helper.GameContent.Load<Dictionary<string, List<string>>>(SEEN_PAGES_DATA_PATH);
            _playerToSeenChapters = helper.GameContent.Load<Dictionary<string, List<string>>>(SEEN_CHAPTERS_DATA_PATH);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOKS_DATA_PATH))
            {
                e.LoadFrom(() => Books, AssetLoadPriority.Medium);
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
