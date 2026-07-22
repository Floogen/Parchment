using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Managers;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Patches.Objects;
using Parchment.Framework.UI.Menus;
using Parchment.Framework.UI.Rendering;
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

        public static bool isDebugMode = false;

        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

        // Managers
        internal static ActionManager actionManager;
        internal static BookManager bookManager;
        internal static QueryManager queryManager;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Create managers
            actionManager = new ActionManager(monitor, helper);
            bookManager = new BookManager(monitor, helper);
            queryManager = new QueryManager(monitor, helper);

            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply patches
                new ObjectPatch(monitor, modHelper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Hook into the required events
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // Register commands
            helper.ConsoleCommands.Add("parchment_debug", "parchment_debug", (cmd, args) => { isDebugMode = !isDebugMode; });
            helper.ConsoleCommands.Add("parchment_open", "parchment_open <book_id> [page] [chapter]", OpenBook);
            helper.ConsoleCommands.Add("parchment_clearseen", "parchment_clearseen", (cmd, args) => { bookManager.ClearSeen(Game1.player); });
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
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Only runs if debug mode is active
            if (isDebugMode is true && e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                // Consume the button press
                Helper.Input.Suppress(e.Button);

                Game1.activeClickableMenu = new BookMenu(bookManager.CreateTestBook());
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
