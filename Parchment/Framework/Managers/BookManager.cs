using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
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

        public List<BookData> Books { get { return _books; } set { FilterBookData(value); } }
        private List<BookData> _books = new List<BookData>();

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

        public Book CreateTestBook()
        {
            BookData bookData = new BookData
            {
                Format = "1.0.0",
                Id = "Parchment.Test_CampingGuide",
                Title = "Camping Guide",
                Description = "A test book for exercising the BookMenu.",
                TintColor = "165 42 42",
                Underlay = new List<ElementData>()
                {
                    new ImageElementData() { TexturePath = "Characters/Junimo", TextureSourceRectangle = new Rectangle(48, 0, 16, 16), Scale = 4,
                        Position = new Point(128, 82),
                        TintColor = Color.YellowGreen.ToSpaceSeparated(),
                        Frames = new List<AnimationFrameData>(){
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(48, 0, 16, 16) },
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(64, 0, 16, 16) },
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(96, 0, 16, 16) },
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(112, 0, 16, 16) }
                        },
                        Condition = "PeacefulEnd.Parchment_CurrentBookState Turning, PeacefulEnd.Parchment_CurrentPageNumber 0"
                    },
                    new ImageElementData() { TexturePath = "Characters/Junimo", TextureSourceRectangle = new Rectangle(48, 0, 16, 16), Scale = 4,
                        Position = new Point(780, 82),
                        TintColor = Color.PaleVioletRed.ToSpaceSeparated(),
                        Frames = new List<AnimationFrameData>(){
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(48, 0, 16, 16) },
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(64, 0, 16, 16) },
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(96, 0, 16, 16) },
                            new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(112, 0, 16, 16) }
                        },
                        Condition = "PeacefulEnd.Parchment_CurrentBookState Turning, PeacefulEnd.Parchment_CurrentPageNumber 2"
                    }
                },
                Overlay = new List<ElementData>()
                {
                    new ImageElementData
                    {
                        TexturePath = "Assets/PeacefulEnd.Parchment/bookmark1",
                        TextureSourceRectangle = new Rectangle(0, 0, 24, 17),
                        HoverTextureSourceRectangle = new Rectangle(0, 17, 24, 17),
                        TintColor = Color.Red.ToSpaceSeparated(),
                        SpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally,
                        Position = new Point(-64, 192),
                        Scale = 4,
                        Action = ActionManager.FIRST_PAGE,
                        Condition = "!PeacefulEnd.Parchment_CurrentPageNumber 0"
                    },
                    new ImageElementData
                    {
                        TexturePath = "Assets/PeacefulEnd.Parchment/bookmark1",
                        TextureSourceRectangle = new Rectangle(0, 0, 20, 17),
                        TintColor = Color.Blue.ToSpaceSeparated(),
                        HoverTextureSourceRectangle = new Rectangle(0, 17, 24, 17),
                        Position = new Point(1064, 256),
                        Scale = 4,
                        Action = ActionManager.LAST_PAGE,
                        Condition = "!PeacefulEnd.Parchment_IsLastPage"
                    }
                },
                Pages = new List<PageData>
                {
                    new PageData {
                        Id = "cover",
                        ChapterId = "chapter-1",
                        Elements = new List<ElementData>() {
                            new ImageElementData() { Text = "Example Guide", FontType = FontType.SpriteText, TextArea = new Rectangle(15, 5, 65, 10), Alignment = AlignmentType.Center, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle2", Scale = 5 },
                            new HeadingElementData() { Text = "By ...", Alignment = AlignmentType.Center },
                            new PanelElementData { TexturePath = "Assets/PeacefulEnd.Parchment/panelFrame2",
                                TextureSourceRectangle = new Rectangle(0, 0, 24, 24), Padding = 0, Width = 32, Sizing = SizingMode.Fixed, Alignment = AlignmentType.Center, Scale = 4,
                                Children = new List<ElementData>() {
                                    new ParagraphElementData() { Text = "This is a fixed panel. Probably. Maybe." },
                                },
                                SpacingAfter = 4
                            },
                            new ButtonElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/button1",
                                TextureSourceRectangle = new Rectangle(0, 0, 18, 18),
                                HoverTextureSourceRectangle = new Rectangle(0, 18, 18, 18),
                                Text = "Click me!",
                                FontType = FontType.Small,
                                TextScale = 1,
                                Padding = 0,
                                Sizing = SizingMode.ShrinkToFit,
                                Scale = 2,
                                Alignment = AlignmentType.Center,
                                Action = ActionManager.LAST_PAGE,
                                Sound = "bigSelect"
                            }
                        }
                    },
                    new PageData {
                        Id = "info",
                        ChapterId = "chapter-1",
                        Elements = new List<ElementData>()
                        {
                            new BannerElementData() { Text = "Test Text", FontType = FontType.Small, CapWidth = 19, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle1", Alignment = AlignmentType.Center, Sizing = SizingMode.ShrinkToFit, Scale = 5 },
                            new ParagraphElementData() { Text = "Wow wow wooooooooooooooooooooooooooooooooooooooooooooooooow", MarginLeft = 16 },
                            new PanelElementData { TexturePath = "Assets/PeacefulEnd.Parchment/panelFrame2", Height = 48,
                                TextureSourceRectangle = new Rectangle(0, 0, 24, 24), Padding = 0, Width = 32, Sizing = SizingMode.Fill, Alignment = AlignmentType.Center, Scale = 4, SpacingAfter = 4,
                                Children = new List<ElementData>() {
                                    new ParagraphElementData() { Text = "This is a fill panel with a set height of 48px", Alignment = AlignmentType.Center, SpacingAfter = 32 },
                                    new ParagraphElementData() { Text = "Much detail", Alignment = AlignmentType.Center, SpacingAfter = 32 },
                                    new ParagraphElementData() { Text = "Hover me!", Alignment = AlignmentType.Center, Description = "This is a hover description." },
                                }
                            },
                            new BannerElementData() { Text = "---Size & Color---", TintColor = "255 0 0 100", FontType = FontType.Small, CapWidth = 19, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle1", Alignment = AlignmentType.Center, Sizing = SizingMode.ShrinkToFit, Scale = 5 },
                        }
                    },
                    new PageData {
                        Id = "test",
                        ChapterId = "chapter-1",
                        Elements = new List<ElementData>()
                        {
                            new HeadingElementData() { Text = "Manually offset text?", MarginLeft = 32 },
                            new HeadingElementData() { Text = "No offset here!" },
                            new HeadingElementData() { Text = "Right aligned", Alignment = AlignmentType.Right },
                            new ImageElementData { TexturePath = "Data/PeacefulEnd_Campgrounds/Campgrounds/Textures/StarterTent", TintColor = (Color.Black * 0.35f).ToSpaceSeparated(), TextureSourceRectangle = new Rectangle(0, 5, 48, 59), Scale = 2, Alignment = AlignmentType.Center },
                            new HeadingElementData() { Text = "This is a tent (probably)", FontType = FontType.Small, Alignment = AlignmentType.Center, SpacingAfter = 12 },
                            new DividerElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/divider2", Sizing = SizingMode.ShrinkToFit, Alignment = AlignmentType.Center, Scale = 2 },
                            new ParagraphElementData() { Text = "A divider above and below!", Alignment = AlignmentType.Center },
                            new DividerElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/divider1", Sizing = SizingMode.ShrinkToFit, Alignment = AlignmentType.Center, Scale = 2, SpacingAfter = 20 },
                            new ParagraphElementData() { Text = "Textureless divider", Alignment = AlignmentType.Center, SpacingAfter = 4 },
                            new DividerElementData() { TintColor = "255 0 0", Sizing = SizingMode.Fixed, Width = 64, Alignment = AlignmentType.Center, Scale = 4 },
                        }
                    },
                    new PageData {
                        Id = "huh",
                        ChapterId = "chapter-1",
                        Elements = new List<ElementData>()
                        {
                            new PanelElementData { TexturePath = "Assets/PeacefulEnd.Parchment/panelFrame2", Height = 48,
                                TextureSourceRectangle = new Rectangle(0, 0, 24, 24), Padding = 0, Width = 32, Sizing = SizingMode.Fill, Alignment = AlignmentType.Center, Scale = 4, SpacingAfter = 4,
                                Children = new List<ElementData>() {
                                    new ImageElementData { TexturePath = "Data/PeacefulEnd_Campgrounds/Campgrounds/Textures/StarterTent", TextureSourceRectangle = new Rectangle(0, 5, 48, 59), Scale = 2, Alignment = AlignmentType.Center },
                                    new HeadingElementData() { Text = "This is a tent (in a panel)", FontType = FontType.Small, Alignment = AlignmentType.Center },
                                }
                            }
                        },
                        Background = new List<ElementData>()
                        {
                            new ImageElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/backgroundNoise1", Position = new Point(48, 272), Scale = 6 },
                            new HeadingElementData() { Text = "This is drawn in the background\n(ontop of another background)", FontType = FontType.Small, Position = new Point(64, 336), Scale = 1 }
                        }
                    },
                    new PageData {
                        Id = "animated",
                        ChapterId = "chapter-1",
                        Elements = new List<ElementData>()
                        {
                            new ImageElementData() { TexturePath = "LooseSprites/GemBird", TextureSourceRectangle = new Rectangle(0, 0, 32, 32), Scale = 3, Alignment = AlignmentType.Center,
                                Frames = new List<AnimationFrameData>(){
                                    new AnimationFrameData() { Duration = 1000, SourceRectangle = new Rectangle(0, 0, 32, 32) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(32, 0, 32, 32) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(64, 0, 32, 32) },
                                    new AnimationFrameData() { Duration = 250, SourceRectangle = new Rectangle(96, 0, 32, 32) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(64, 0, 32, 32) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(32, 0, 32, 32) },
                                } 
                            },
                            new HeadingElementData() { Text = "! Animated sprites !", FontType = FontType.Small, Scale = 1, Alignment = AlignmentType.Center, SpacingAfter = 32 },
                            new ImageElementData() { TexturePath = "LooseSprites/Cursors2", TextureSourceRectangle = new Rectangle(192, 62, 32, 32), Scale = 3, Alignment = AlignmentType.Center,
                                Frames = new List<AnimationFrameData>(){
                                    new AnimationFrameData() { Duration = 1000, SourceRectangle = new Rectangle(192, 62, 32, 32) },
                                    new AnimationFrameData() { Duration = 250, SourceRectangle = new Rectangle(224, 62, 32, 32) },
                                    new AnimationFrameData() { Duration = 500, SourceRectangle = new Rectangle(192, 62, 32, 32) },
                                },
                                SpacingAfter = 16
                            },
                            new ImageElementData() { TexturePath = "Characters/Junimo", TextureSourceRectangle = new Rectangle(48, 0, 16, 16), Scale = 8, Alignment = AlignmentType.Center, 
                                TintColor = Color.LawnGreen.ToSpaceSeparated(),
                                Frames = new List<AnimationFrameData>(){
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(48, 0, 16, 16) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(64, 0, 16, 16) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(96, 0, 16, 16) },
                                    new AnimationFrameData() { Duration = 100, SourceRectangle = new Rectangle(112, 0, 16, 16) }
                                },
                                Description = "Junimo!",
                                SpacingAfter = 32
                            }
                        }
                    },
                    new PageData {
                        Id = "items",
                        ChapterId = "chapter-1",
                        Background = new List<ElementData>()
                        {
                            new ImageElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/itemBorder1", Scale = 4, Position = new Point(160, 40) },
                        },
                        Elements = new List<ElementData>()
                        {
                            new HeadingElementData() { Text = "Automatic Item Handling!", Alignment = AlignmentType.Center, SpacingAfter = 18 },
                            new ImageElementData() { ItemId = "(O)24", SpacingAfter = 16, Scale = 4, Alignment = AlignmentType.Center },
                            new HeadingElementData() { Text = "With automatic or custom hover text...", Alignment = AlignmentType.Center, SpacingAfter = 64 },
                            new ImageElementData() { ItemId = "(O)24", DisplayName = "", Description = "This is probably a parsnip?", Scale = 4, Alignment = AlignmentType.Center },
                        }
                    },
                    new PageData {
                        Id = "last?",
                        ChapterId = "chapter-1",
                        Elements = new List<ElementData>()
                        {
                            new BannerElementData() { Text = "Is this the end?", FontType = FontType.Small, CapWidth = 19, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle1", Alignment = AlignmentType.Center, Sizing = SizingMode.ShrinkToFit, Scale = 5 },
                            new ButtonElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/button1",
                                TextureSourceRectangle = new Rectangle(0, 0, 18, 18),
                                HoverTextureSourceRectangle = new Rectangle(0, 18, 18, 18),
                                Text = "Click here!",
                                FontType = FontType.Small,
                                TextScale = 1,
                                Padding = 2,
                                Sizing = SizingMode.ShrinkToFit,
                                Scale = 2,
                                Alignment = AlignmentType.Center,
                                Action = $"{ActionManager.JUMP_TO_CHAPTER} chapter-2",
                                Sound = "bigSelect"
                            }
                        }
                    },

                    // Start chapter 2
                    new PageData {
                        Id = "hopped",
                        ChapterId = "chapter-2",
                        Elements = new List<ElementData>()
                        {
                            new BannerElementData() { Text = "Chapter 2", FontType = FontType.Small, CapWidth = 19, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle1", Alignment = AlignmentType.Center, Sizing = SizingMode.ShrinkToFit, Scale = 5 },
                        }
                    },
                    new PageData {
                        Id = "last!",
                        ChapterId = "chapter-2",
                        Elements = new List<ElementData>()
                        {
                            new BannerElementData() { Text = "Can't go back!", FontType = FontType.Small, CapWidth = 19, TexturePath = "Assets/PeacefulEnd.Parchment/bannerTitle1", Alignment = AlignmentType.Center, Sizing = SizingMode.ShrinkToFit, Scale = 5 },
                            new HeadingElementData() { Text = "Use the button below to go back to chapter 1.", Alignment = AlignmentType.Center },
                            new ButtonElementData() { TexturePath = "Assets/PeacefulEnd.Parchment/button1",
                                TextureSourceRectangle = new Rectangle(0, 0, 18, 18),
                                HoverTextureSourceRectangle = new Rectangle(0, 18, 18, 18),
                                Text = "To the start!",
                                FontType = FontType.Small,
                                TextScale = 1,
                                Padding = 2,
                                Sizing = SizingMode.ShrinkToFit,
                                Scale = 2,
                                Alignment = AlignmentType.Center,
                                Action = ActionManager.GO_TO_START,
                                Sound = "bigSelect"
                            }
                        }
                    }
                }
            };

            return new Book(bookData, ElementRegistry, FontResolver);
        }
    }
}
