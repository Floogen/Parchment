using Parchment.Framework.Models;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Rewrites the placeholders an author can put in a trigger action before it runs.
    /// This happens at dispatch rather than on the model, as element data is the shared cached asset instance and a rewrite there would stick across asset reloads.
    /// </summary>
    public static class ActionTokenHelper
    {
        /// <summary>Resolves every token in an action, with each value quoted so a substitution containing spaces stays one argument. See <see cref="TokenHelper"/> for the vocabulary.</summary>
        /// <param name="element">The element the action belongs to. Null for an action that belongs to no element, such as a page's OnView trigger.</param>
        public static string Resolve(string action, Element? element)
        {
            return TokenHelper.Resolve(action, element, quoteValues: true);
        }
    }
}
