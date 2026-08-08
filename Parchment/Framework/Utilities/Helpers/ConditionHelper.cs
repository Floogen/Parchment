using Parchment.Framework.Models;
using StardewValley;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Checks an authored condition, resolving its placeholders first. See <see cref="TokenHelper"/> for the vocabulary.
    /// Values are quoted, as a condition splits its arguments on spaces the same way a trigger action does, so a substitution holding a space would otherwise become several arguments.
    /// The game's own [Token] forms are resolved here too, each one on its own so its result can be quoted the same way, since a condition goes to <see cref="GameStateQuery"/> rather than to the game's own parser.
    /// </summary>
    public static class ConditionHelper
    {
        /// <summary>Whether a condition passes. An empty condition passes, so a caller can hand one over without checking for one first.</summary>
        /// <param name="condition">The authored condition, which may hold tokens.</param>
        /// <param name="element">The element the condition belongs to, used by the tokens that read from it. Null for a condition that belongs to no element, such as a page's OnView trigger or a keybind.</param>
        /// <param name="parseTokenizableStrings">Whether the game's [Token] forms are resolved, for a condition whose owner is not an element. Null takes the element's answer.</param>
        public static bool Check(string? condition, Element? element = null, bool? parseTokenizableStrings = null)
        {
            if (string.IsNullOrWhiteSpace(condition) is true)
            {
                return true;
            }

            return GameStateQuery.CheckConditions(Resolve(condition, element, parseTokenizableStrings));
        }

        /// <summary>A condition with its tokens resolved, as it will be handed to the game. Kept separate from <see cref="Check"/> so the resolved form can be logged or tested on its own.</summary>
        public static string Resolve(string condition, Element? element = null, bool? parseTokenizableStrings = null)
        {
            return TokenHelper.Resolve(condition, element, quoteValues: true, parseTokenizableStrings);
        }
    }
}
