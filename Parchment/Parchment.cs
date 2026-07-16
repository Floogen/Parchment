using HarmonyLib;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Menus;
using Parchment.Framework.UI.Rendering;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment
{
    public class Parchment : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static ElementRegistry elementRegistery;

        public const string BOOKS_DATA_PATH = "Data/PeacefulEnd.Parchment/Books";

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Create ElementRegistery
            elementRegistery = new ElementRegistry(registerDefaults: true);

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += Content_AssetsInvalidated; ;
        }

        private void Content_AssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            var campData = e.NamesWithoutLocale.FirstOrDefault(a => a.IsEquivalentTo(BOOKS_DATA_PATH));
            if (campData is not null)
            {
                var test = Helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
                _ = test;
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(BOOKS_DATA_PATH))
            {
                var testBook = CreateTestBook();
                e.LoadFrom(() => new List<BookData>() { testBook.Data }, AssetLoadPriority.Medium);
            }
        }

        private void GameLoop_GameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
        }

        private void OnButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                var test = Helper.GameContent.Load<List<BookData>>(BOOKS_DATA_PATH);
                _ = test;

                // Consume the button press
                Helper.Input.Suppress(e.Button);

                Game1.activeClickableMenu = new BookMenu(CreateTestBook());
            }
        }

        private Book CreateTestBook()
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
                            new HeadingElementData() { Text = "By ...", Alignment = AlignmentType.Center }
                        }
                    },
                    new PageData {
                        Id = "info", Elements = new List<ElementData>()
                        { 
                            new HeadingElementData() { Text = "Test Text" }, 
                            new ParagraphElementData() { Text = "Wow wow wooooooooooooooooooooooooooooooooooooooooooooooooow" }
                        }
                    },
                    new PageData {
                        Id = "test", Elements = new List<ElementData>()
                        {
                            new HeadingElementData() { Text = "Next Page?" },
                            new PanelElementData { TexturePath = "Assets/PeacefulEnd.Parchment/panelFrame2",
                                TextureSourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 24, 24), Width = 256, Alignment = AlignmentType.Center,
                                Children = new List<ElementData>() {
                                    new ImageElementData { TexturePath = "Data/PeacefulEnd_Campgrounds/Campgrounds/Textures/StarterTent", TextureSourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 48, 80), Scale = 2, Alignment = AlignmentType.Center } 
                                } 
                            }
                        }
                    }
                }
            };

            return new Book(bookData, elementRegistery, owner: null);
        }
    }
}
