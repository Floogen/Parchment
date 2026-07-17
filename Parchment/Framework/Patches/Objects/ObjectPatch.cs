using HarmonyLib;
using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Patches.Objects
{
    internal class ObjectPatch : PatchTemplate
    {
        private readonly System.Type _object = typeof(Object);

        public ObjectPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Object.performUseAction), null), postfix: new HarmonyMethod(GetType(), nameof(PerformUseActionPostfix)));
        }

        private static void PerformUseActionPostfix(Object __instance, ref bool __result)
        {
            if (Parchment.bookManager.TryGetBookId(__instance.QualifiedItemId, out var bookId) is false || string.IsNullOrEmpty(bookId))
            {
                return;
            }

            var book = Parchment.bookManager.CreateBook(bookId);
            if (book is null)
            {
                return;
            }
            __result = false;

            Game1.activeClickableMenu = new BookMenu(book);
        }
    }
}