using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Elements;
using StardewModdingAPI;
using System.Text.RegularExpressions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Rewrites the placeholders an author can put in a trigger action before it runs.
    /// This happens at dispatch rather than on the model, as element data is the shared cached asset instance and a rewrite there would stick across asset reloads.
    /// </summary>
    public static class ActionTokenHelper
    {
        private static readonly Regex _inputTokenPattern = new Regex("%Input(?::(?<inputId>[^%]+))?%", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex _itemTokenPattern = new Regex("%Item%", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Replaces every %Input% and %Input:inputId% token with the text currently in that input, quoted so a typed phrase stays one argument.</summary>
        /// <param name="element">The element the action belongs to, used to resolve the bare %Input% token. Null for an action that belongs to no element, such as a page's OnView trigger.</param>
        public static string Resolve(string action, Element? element)
        {
            if (string.IsNullOrEmpty(action) || action.Contains('%') is false)
            {
                return action;
            }

            string resolvedAction = _inputTokenPattern.Replace(action, match => ResolveInputToken(match, action, element));

            return _itemTokenPattern.Replace(resolvedAction, match => ResolveItemToken(match, action, element));
        }

        /// <summary>Replaces %Item% with the qualified ID of the item the element is showing, which is how one template's action reaches whichever result its cell landed on.</summary>
        private static string ResolveItemToken(Match match, string action, Element? element)
        {
            if (element is null || string.IsNullOrWhiteSpace(element.AssignedItemId))
            {
                Parchment.monitor.LogOnce($"The action '{action}' uses %Item%, which only works inside a Grid's result cell.", LogLevel.Warn);
                return match.Value;
            }

            return Quote(element.AssignedItemId);
        }

        private static string ResolveInputToken(Match match, string action, Element? element)
        {
            string inputId = match.Groups["inputId"].Value;

            if (string.IsNullOrWhiteSpace(inputId) is true)
            {
                if (element?.Data is not InputElementData inputData || string.IsNullOrWhiteSpace(inputData.InputId))
                {
                    Parchment.monitor.LogOnce($"The action '{action}' uses a bare {match.Value} token, which only works on an Input element. Name an input with %Input:yourInputId% instead.", LogLevel.Warn);
                    return match.Value;
                }

                inputId = inputData.InputId;
            }

            // An unknown ID is left in place rather than substituted empty, so a typo fails loudly at the action's own argument parsing
            if (Parchment.inputManager.IsKnown(inputId) is false)
            {
                Parchment.monitor.LogOnce($"The action '{action}' refers to the input '{inputId}', which no element on this book has laid out.", LogLevel.Warn);
                return match.Value;
            }

            return Quote(Parchment.inputManager.GetText(inputId));
        }

        /// <summary>Wraps a value so it survives as a single argument. Quotes are dropped rather than escaped, as trigger action parsing has no escape form for them.</summary>
        private static string Quote(string value)
        {
            return string.Concat("\"", value.Replace("\"", string.Empty), "\"");
        }
    }
}
