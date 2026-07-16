using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Menus;
using Parchment.Framework.UI.Rendering;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Tools;
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

        public List<BookData> Books { get { return _books; } set { FilterBookData(value); } }
        private List<BookData> _books = new List<BookData>();

        private ElementRegistry _elementRegistery;
        private FontResolver _fontResolver;

        public BookManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            // Register default helpers
            _elementRegistery = new ElementRegistry(registerDefaults: true);
            _fontResolver = new FontResolver();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += OnAssetInvalidated;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOKS_DATA_PATH))
            {
                e.LoadFrom(() => Books, AssetLoadPriority.Medium);
            }
        }

        private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            var books = e.NamesWithoutLocale.FirstOrDefault(a => a.IsEquivalentTo(BOOKS_DATA_PATH));
            if (books is not null)
            {
                Books = helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
            }

            if (Game1.activeClickableMenu is BookMenu bookMenu)
            {
                bookMenu.Book.RefreshTextures(e.NamesWithoutLocale);
            }
        }

        private void FilterBookData(List<BookData> bookData)
        {
            /// TODO: Finish implementing the various IsValid checks
            _books = bookData;
            return;
            foreach (var book in bookData)
            {
                var isValidData = book.IsValid();
                if (isValidData.Result is false)
                {
                    monitor.LogOnce($"Skipping invalid BookData with name \"{book.Id}\": {isValidData.Error}", LogLevel.Warn);
                }
            }

            _books = bookData.Where(c => c.IsValid().Result is true).ToList();
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
            return new Book(bookData, _elementRegistery, _fontResolver);
        }

        public Book CreateTestBook()
        {
            BookData bookData = new BookData
            {
                Format = "1.0.0",
                Id = "Parchment.Test_CampingGuide",
                Title = "Camping Guide",
                Description = "A test book for exercising the BookMenu.",
                Pages = new List<PageData>
                {
                    new PageData {
                        Id = "cover", Elements = new List<ElementData>() {
                            new TitleElementData() { Text = "Example Guide", Alignment = AlignmentType.Center },
                            new HeadingElementData() { Text = "By ...", Alignment = AlignmentType.Center },
                            new PanelElementData { TexturePath = "Assets/PeacefulEnd.Parchment/panelFrame2",
                                TextureSourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 24), Padding = 0, Width = 32, Sizing = SizingMode.Fixed, Alignment = AlignmentType.Center, Scale = 4,
                                Children = new List<ElementData>() {

                            new ParagraphElementData() { Text = "This is a fixed panel. Probably. Maybe." },
                                }
                            }
                        }
                    },
                    new PageData {
                        Id = "info", Elements = new List<ElementData>()
                        {
                            new BannerElementData() { Text = "Test Text", FontType = FontType.Small, CapWidth = 19, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle1", Alignment = AlignmentType.Center, Sizing = SizingMode.ShrinkToFit, Scale = 5 },
                            new ParagraphElementData() { Text = "Wow wow wooooooooooooooooooooooooooooooooooooooooooooooooow", MarginLeft = 16 },
                            new PanelElementData { TexturePath = "Assets/PeacefulEnd.Parchment/panelFrame2", Height = 48,
                                TextureSourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 24), Padding = 0, Width = 32, Sizing = SizingMode.Fill, Alignment = AlignmentType.Center, Scale = 4,
                                Children = new List<ElementData>() {
                                    new ParagraphElementData() { Text = "This is a fill panel with a set height of 48", Alignment = AlignmentType.Center, SpacingAfter = 32 },
                                    new ParagraphElementData() { Text = "Much detail", Alignment = AlignmentType.Center },
                                }
                            }
                        }
                    },
                    new PageData {
                        Id = "test", Elements = new List<ElementData>()
                        {
                            new HeadingElementData() { Text = "Next Page?" },
                            new ImageElementData { TexturePath = "Data/PeacefulEnd_Campgrounds/Campgrounds/Textures/StarterTent", TextureSourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 5, 48, 59), Scale = 2, Alignment = AlignmentType.Center }

                        }
                    }
                }
            };

            return new Book(bookData, _elementRegistery, _fontResolver);
        }
    }
}
