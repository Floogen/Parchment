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
                    new PageData { Id = "cover", Elements = new List<PageElementData>() { new PageElementData() { Type = PageElementType.Title, Text = "Camping Guide", Alignment = AlignmentType.Center } } },
                    new PageData { Id = "info", Elements = new List<PageElementData>() { new PageElementData() { Type = PageElementType.Header, Text = "Test Text" }, new PageElementData() { Type = PageElementType.Paragraph, Text = "Wow wow" } } },
                    new PageData { Id = "test", Elements = new List<PageElementData>() { new PageElementData() { Type = PageElementType.Header, Text = "Next Page?" }, new PageElementData { Type = PageElementType.Image, ImagePath = "Data/PeacefulEnd_Campgrounds/Campgrounds/Textures/StarterTent", ImageSourceRectangle = new Microsoft.Xna.Framework.Rectangle(0, 0, 48, 80) , Alignment = AlignmentType.Center } } }
                }
            };

            return new Book(bookData, owner: null);
        }
    }
}
