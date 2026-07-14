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
            new PageData { Id = "cover", Type = PageType.Title, Title = "Camping Guide" },
            new PageData { Id = "intro", Type = PageType.Text, Title = "Chapter 1", Text = "Welcome to the wilderness! This chapter covers the basics of setting up camp, keeping warm, and not being eaten by anything larger than you are." },
            new PageData { Id = "tents", Type = PageType.Text, Title = "Tents", Text = "A good tent keeps the rain out and the warmth in. Pitch on flat ground, away from dead branches." },
            new PageData { Id = "tent-diagram", Type = PageType.Image, ImagePath = "Framework/Assets/testDiagram.png", ImageScale = 4f },
            new PageData { Id = "campfires", Type = PageType.Text, Title = "Campfires", Text = "Ring your fire with stones. Never leave it unattended. Marshmallows optional but recommended." },
        }
            };

            return new Book(bookData, owner: null);
        }
    }
}
