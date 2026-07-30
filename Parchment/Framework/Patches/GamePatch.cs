using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Parchment.Framework.Patches
{
    internal class GamePatch : PatchTemplate
    {
        private readonly System.Type _game = typeof(Game1);

        public GamePatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal override void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_game, nameof(Game1.tryToCheckAt), new[] { typeof(Vector2), typeof(Farmer) }), postfix: new HarmonyMethod(GetType(), nameof(TryToCheckAtPostfix)));
        }

        private static void TryToCheckAtPostfix(bool __result)
        {
            if (__result is false)
            {
                return;
            }

            Parchment.bookManager.CancelRequestedBook();
        }
    }
}
