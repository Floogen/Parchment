using Parchment.Framework.Models;
using StardewValley;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Checks an authored condition, resolving its placeholders first. See <see cref="TokenHelper"/> for the vocabulary.
    /// Values are quoted, as a condition splits its arguments on spaces the same way a trigger action does, so a substitution holding a space would otherwise become several arguments.
    /// Unlike an action the game's own [Token] forms are resolved here as well, since a condition goes to <see cref="GameStateQuery"/> rather than to the game's own parser.
    /// </summary>
    public static class ConditionHelper
    {
        /// <summary>Whether a condition passes. An empty condition passes, so a caller can hand one over without checking for one first.</summary>
        /// <param name="condition">The authored condition, which may hold tokens.</param>
        /// <param name="element">The element the condition belongs to, used by the tokens that read from it. Null for a condition that belongs to no element, such as a page's OnView trigger.</param>
        public static bool Check(string? condition, Element? element = null)
        {
            if (string.IsNullOrWhiteSpace(condition) is true)
            {
                return true;
            }

            return GameStateQuery.CheckConditions(Resolve(condition, element));
        }

        /// <summary>A condition with its tokens resolved, as it will be handed to the game. Kept separate from <see cref="Check"/> so the resolved form can be logged or tested on its own.</summary>
        public static string Resolve(string condition, Element? element = null)
        {
            return TokenHelper.Resolve(condition, element, quoteValues: true);
        }
    }
}
