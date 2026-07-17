using HarmonyLib;
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
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                var test = Helper.GameContent.Load<List<BookData>>(BookManager.BOOKS_DATA_PATH);
                _ = test;

                // Consume the button press
                Helper.Input.Suppress(e.Button);

                Game1.activeClickableMenu = new BookMenu(bookManager.CreateTestBook());
            }
        }
    }
}
