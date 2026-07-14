using HarmonyLib;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Parchment
{
    public class Parchment : Mod
    {
        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

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
        }

        private void OnButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (e.Button is SButton.O && Context.IsPlayerFree && Game1.activeClickableMenu is null)
            {
                // Consume the button press
                Helper.Input.Suppress(e.Button);

                var testPages = new List<PageEntry>
                {
                    new PageEntry(new PageData { Id = "cover", Type = PageType.Title, Title = "Camping Guide" }, owner: null),
                    new PageEntry(new PageData { Id = "intro", Type = PageType.Text, Title = "Chapter 1", Text = "Hello" }, owner: null),
                    new PageEntry(new PageData { Id = "tent", Type = PageType.Text, Text = "0123" }, owner: null),
                };
                Game1.activeClickableMenu = new BookMenu(testPages);
            }
        }
    }
}
