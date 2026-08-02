using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Parchment.Framework.API;
using Parchment.Framework.Managers;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Patches;
using Parchment.Framework.Patches.Objects;
using Parchment.Framework.UI.Menus;
using Parchment.Framework.UI.Rendering;
using Parchment.Framework.Utilities;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment
{
    public class Parchment : Mod
    {
        public const string CUSTOM_BOOK_ID = "(O)PeacefulEnd.Parchment_Book";
        public const string CUSTOM_BOOK_FIELD_ID = "PeacefulEnd.Parchment/CustomFields/BookId";

        public const string DEFAULT_BOOK_ASSET = "Assets/PeacefulEnd.Parchment/smallBook";
        public const string DEFAULT_BOOK_GRAYSCALE_ASSET = "Assets/PeacefulEnd.Parchment/smallBookGrayscale";
        public const string DEFAULT_PAGE_CURL_ASSET = "Assets/PeacefulEnd.Parchment/curlPage";

        public const string DEFAULT_NOTEBOOK_ASSET = "Assets/PeacefulEnd.Parchment/notebook";
        public const string DEFAULT_NOTEBOOK_GRAYSCALE_ASSET = "Assets/PeacefulEnd.Parchment/notebookGrayscale";
        public const string DEFAULT_NOTEBOOK_PAGE_CURL_ASSET = "Assets/PeacefulEnd.Parchment/curlPage2";

        public static bool isDebugMode = false;

        // Shared static helpers
        internal static IManifest manifest;
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

        // Managers
        internal static ActionManager actionManager;
        internal static BookManager bookManager;
        internal static ContentPatcherManager contentPatcherManager;
        internal static FlagManager flagManager;
        internal static InputManager inputManager;
        internal static QueryManager queryManager;
        internal static TileManager tileManager;
        internal static VariableManager variableManager;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            manifest = ModManifest;
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Create managers
            actionManager = new ActionManager(monitor, helper);
            bookManager = new BookManager(monitor, helper);
            contentPatcherManager = new ContentPatcherManager(monitor, helper);
            flagManager = new FlagManager(monitor, helper);
            inputManager = new InputManager(monitor, helper);
            queryManager = new QueryManager(monitor, helper);
            tileManager = new TileManager(monitor, helper);
            variableManager = new VariableManager(monitor, helper);

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
                new ObjectPatch(monitor, modHelper).Apply(harmony);
                new GamePatch(monitor, modHelper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.Saving += (sender, args) => variableManager.Save();
            helper.Events.GameLoop.ReturnedToTitle += (sender, args) => variableManager.Save();

            // Global variables can be set with no book open, so attempt variable save once a second
            helper.Events.GameLoop.OneSecondUpdateTicked += (sender, args) => variableManager.Save();

            // Register actions
            GameLocation.RegisterTileAction("PeacefulEnd.Parchment_OpenBook", MapActionHelper.HandleOpenBook);

            // Register commands
            helper.ConsoleCommands.Add("parchment_debug", "parchment_debug", (cmd, args) => { isDebugMode = !isDebugMode; });
            helper.ConsoleCommands.Add("parchment_open", "parchment_open <book_id> [page] [chapter]", OpenBook);
            helper.ConsoleCommands.Add("parchment_clearseen", "parchment_clearseen", (cmd, args) => { bookManager.ClearSeen(Game1.player); });
        }

        public override object GetApi(IModInfo mod)
        {
            return new ParchmentApi(mod);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(DEFAULT_BOOK_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/smallBook.png"), AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(DEFAULT_BOOK_GRAYSCALE_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/smallBookGrayscale.png"), AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(DEFAULT_PAGE_CURL_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/curlPage.png"), AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(DEFAULT_NOTEBOOK_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/notebook.png"), AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(DEFAULT_NOTEBOOK_GRAYSCALE_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/notebookGrayscale.png"), AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo(DEFAULT_NOTEBOOK_PAGE_CURL_ASSET))
            {
                e.LoadFrom(() => Helper.ModContent.Load<Texture2D>("Framework/Assets/curlPage2.png"), AssetLoadPriority.Low);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // A focused Input owns typed characters, which arrive through the keyboard dispatcher, but the chat hotkey and every other world bind are read from the polled key state instead and never pass through the menu
            // Suppressing here is the only place that reaches them. Escape is let through so it can still leave the box
            if (Game1.activeClickableMenu is BookMenu focusedBookMenu && focusedBookMenu.HasFocusedInput is true && e.Button.TryGetKeyboard(out Keys pressedKey) is true && pressedKey is not Keys.Escape)
            {
                Helper.Input.Suppress(e.Button);
                return;
            }

            // Only runs if debug mode is active
            if (isDebugMode is true && e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                // Consume the button press
                Helper.Input.Suppress(e.Button);

                Game1.activeClickableMenu = new BookMenu(bookManager.CreateBook("PeacefulEnd.Parchment.ExamplePack_Notebook"));
            }
        }

        public static void OpenBook(string command, string[] args)
        {
            if (ArgUtility.TryGet(args, 0, out string bookId, out string error) is false || (bookManager.CreateBook(bookId) is var book && book is null))
            {
                return;
            }

            var bookMenu = new BookMenu(book);
            if (ArgUtility.TryGetInt(args, 1, out int page, out error) is true)
            {
                bool passed = ArgUtility.TryGet(args, 2, out string chapter, out error) is true ? bookMenu.TryOpenAtChapterPage(chapter, page, out error) : bookMenu.TryOpenAtChapter(chapter, out error);
                if (passed is false)
                {
                    monitor.Log(error, LogLevel.Warn);
                }
            }

            Game1.activeClickableMenu = bookMenu;
        }
    }
}
