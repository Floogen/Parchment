using Parchment.Framework.Models;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Rewrites the placeholders an author can put in a trigger action before it runs.
    /// This happens at dispatch rather than on the model, as element data is the shared cached asset instance and a rewrite there would stick across asset reloads.
    /// </summary>
    public static class ActionTokenHelper
    {
        /// <summary>Resolves every token in an action, with each value quoted so a substitution containing spaces stays one argument. See <see cref="TokenHelper"/> for the vocabulary.
        /// The game's own [Token] forms resolve here as well. Few trigger actions parse their own arguments and none of Parchment's do, so a form left for the game would arrive at the action as literal text.
        /// Quoting is what makes resolving them safe: each one is read on its own and its result quoted, so a farm name of two words stays one argument rather than becoming two.
        /// A token that picks at random picks afresh on every run, unlike in text and conditions where the answer is held for the day. Nothing here is resolved twice, so there is no relayout to protect against.
        /// </summary>
        /// <param name="element">The element the action belongs to. Null for an action that belongs to no element, such as a page's OnView trigger or a keybind.</param>
        /// <param name="parseTokenizableStrings">Whether the game's [Token] forms are resolved, for an action whose owner is not an element. Null takes the element's answer.</param>
        public static string Resolve(string action, Element? element, bool? parseTokenizableStrings = null)
        {
            return TokenHelper.Resolve(action, element, quoteValues: true, parseTokenizableStrings, pinRandom: false);
        }
    }
}
