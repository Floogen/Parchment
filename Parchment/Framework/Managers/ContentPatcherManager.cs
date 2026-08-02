using Parchment.Framework.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Managers
{
    public class ContentPatcherManager : BaseManager
    {
        public const string CONTENT_PATCHER_ID = "Pathoschild.ContentPatcher";
        public const string VARIABLES_TOKEN = "Variables";

        public ContentPatcherManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (helper.ModRegistry.IsLoaded(CONTENT_PATCHER_ID) is false)
            {
                return;
            }

            if (helper.ModRegistry.GetApi<IContentPatcherApi>(CONTENT_PATCHER_ID) is not IContentPatcherApi api)
            {
                monitor.Log($"Content Patcher is installed but its API couldn't be reached, so the {VARIABLES_TOKEN} token won't be available.", LogLevel.Warn);
                return;
            }

            api.RegisterToken(Parchment.manifest, VARIABLES_TOKEN, GetVariableValues);
        }

        /// <summary>Every declared variable, as a list of "bookId/variableId=value" entries a pack matches against in a When block.
        /// Content Patcher asks for this on each of its context updates and caches the answer, so a variable changed inside a book reaches patches at the next update rather than on the click.
        /// </summary>
        private IEnumerable<string>? GetVariableValues()
        {
            List<string> values = Parchment.variableManager.GetAllValues(Context.IsWorldReady is true ? Game1.player : null).ToList();

            // Null tells Content Patcher the token isn't available yet (used before books are loaded)
            return values.Count is 0 ? null : values;
        }
    }
}
