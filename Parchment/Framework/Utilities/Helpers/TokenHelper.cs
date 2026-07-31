using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Text.RegularExpressions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Resolves the placeholders an author can write into an element's text or into a trigger action.
    /// Both use one vocabulary so a token means the same thing wherever it appears, and differ only in whether the value is quoted: an action's arguments are split on spaces, text isn't.
    /// </summary>
    public static class TokenHelper
    {
        private const string ESCAPED_PERCENT = "%%";
        private const string ESCAPED_PERCENT_PLACEHOLDER = "\u0001";

        private static readonly Regex _tokenPattern = new Regex(@"%(?<name>[A-Za-z]+)(?:\.(?<property>[A-Za-z]+))?(?::(?<argument>[^%]+))?%", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Whether a string is worth running through <see cref="Resolve"/> at all, which is what keeps the change watch off every element that has no tokens in it.</summary>
        public static bool HasTokens(string? text)
        {
            return string.IsNullOrEmpty(text) is false && text.Contains('%');
        }

        /// <summary>Whether an element's authored text carries a token, and so needs watching for a value that changes without a condition changing with it.</summary>
        public static bool HasTokenText(Element element)
        {
            return element.Data is ITextContent textContent && HasTokens(textContent.Text);
        }

        /// <summary>The element's authored text with its tokens resolved, or null when it has no text of its own.</summary>
        public static string? ResolveElementText(Element element)
        {
            if (element.Data is not ITextContent textContent || textContent.Text is null)
            {
                return null;
            }

            return Resolve(textContent.Text, element, quoteValues: false);
        }

        /// <summary>Replaces every token in a string with what it stands for. An unknown or unresolvable token is left in place and logged, so a typo fails visibly rather than turning into an empty gap.</summary>
        /// <param name="element">The element the string belongs to, used by the tokens that read from it. Null for a string that belongs to no element, such as a page's OnView trigger.</param>
        /// <param name="quoteValues">Whether a substituted value is wrapped in quotes. True for a trigger action, where a value containing spaces would otherwise become several arguments.</param>
        public static string Resolve(string text, Element? element, bool quoteValues)
        {
            if (HasTokens(text) is false)
            {
                return text;
            }

            // Held aside so a literal %% can't be read as an empty token, and put back once the real ones are done
            string workingText = text.Replace(ESCAPED_PERCENT, ESCAPED_PERCENT_PLACEHOLDER);

            workingText = _tokenPattern.Replace(workingText, match => ResolveToken(match, text, element, quoteValues));

            return workingText.Replace(ESCAPED_PERCENT_PLACEHOLDER, "%");
        }

        private static string ResolveToken(Match match, string source, Element? element, bool quoteValues)
        {
            string name = match.Groups["name"].Value;
            string property = match.Groups["property"].Value;
            string argument = match.Groups["argument"].Value;

            switch (name.ToLowerInvariant())
            {
                case "input":
                    return ResolveInput(match, source, argument, element, quoteValues);
                case "item":
                    return ResolveItem(match, source, property, element, quoteValues);
                case "variable":
                    return ResolveVariable(match, source, argument, quoteValues);
                case "griddisplayed":
                case "gridmatched":
                case "gridtotal":
                    return ResolveGridCount(match, source, name, argument);
            }

            // Left alone rather than blanked, since an unrecognised token is far more likely to be someone's literal text than a typo
            return match.Value;
        }

        private static string ResolveInput(Match match, string source, string inputId, Element? element, bool quoteValues)
        {
            if (string.IsNullOrWhiteSpace(inputId) is true)
            {
                if (element?.Data is not InputElementData inputData || string.IsNullOrWhiteSpace(inputData.InputId))
                {
                    Parchment.monitor.LogOnce($"'{source}' uses a bare {match.Value} token, which only works on an Input element. Name an input with %Input:yourInputId% instead.", LogLevel.Warn);
                    return match.Value;
                }

                inputId = inputData.InputId;
            }

            if (Parchment.inputManager.IsKnown(inputId) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' refers to the input '{inputId}', which no element on this book has laid out.", LogLevel.Warn);
                return match.Value;
            }

            return Format(Parchment.inputManager.GetText(inputId), quoteValues);
        }

        private static string ResolveItem(Match match, string source, string property, Element? element, bool quoteValues)
        {
            if (element is null || string.IsNullOrWhiteSpace(element.AssignedItemId))
            {
                Parchment.monitor.LogOnce($"'{source}' uses {match.Value}, which only works inside a Grid's result cell.", LogLevel.Warn);
                return match.Value;
            }

            // The bare token is the qualified ID, which is what an action needs to address the item
            if (string.IsNullOrEmpty(property) is true)
            {
                return Format(element.AssignedItemId, quoteValues);
            }

            if (ItemPropertyResolver.IsKnown(property) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' asks for the item property '{property}', which is not one Parchment knows. Try one of: {string.Join(", ", ItemPropertyResolver.GetNames())}.", LogLevel.Warn);
                return match.Value;
            }

            if (element.AssignedItemData is null)
            {
                return match.Value;
            }

            return Format(ItemPropertyResolver.Resolve(property, element.AssignedItemData, element.AssignedItem) ?? string.Empty, quoteValues);
        }

        private static string ResolveVariable(Match match, string source, string variableId, bool quoteValues)
        {
            if (string.IsNullOrWhiteSpace(variableId) is true)
            {
                Parchment.monitor.LogOnce($"'{source}' uses {match.Value} without naming a variable. Use %Variable:yourVariableId% instead.", LogLevel.Warn);
                return match.Value;
            }

            if (Parchment.variableManager.TryGetCurrentBookId(out string bookId) is false)
            {
                return match.Value;
            }

            if (Parchment.variableManager.TryGet(bookId, variableId, out string value, out string error) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' reads the variable '{variableId}', but {error}.", LogLevel.Warn);
                return match.Value;
            }

            return Format(value, quoteValues);
        }

        private static string ResolveGridCount(Match match, string source, string name, string gridId)
        {
            if (string.IsNullOrWhiteSpace(gridId) is true)
            {
                Parchment.monitor.LogOnce($"'{source}' uses {match.Value} without naming a grid. Use %{name}:yourGridId% instead.", LogLevel.Warn);
                return match.Value;
            }

            if (Game1.activeClickableMenu is not BookMenu bookMenu || bookMenu.TryGetGridCounts(gridId, out int displayed, out int matched, out int total) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' refers to the grid '{gridId}', which is not on screen.", LogLevel.Warn);
                return match.Value;
            }

            switch (name.ToLowerInvariant())
            {
                case "griddisplayed":
                    return displayed.ToString();
                case "gridmatched":
                    return matched.ToString();
            }

            return total.ToString();
        }

        /// <summary>Wraps a value so it survives as a single trigger action argument. Quotes are dropped rather than escaped, as action parsing has no escape form for them.</summary>
        private static string Format(string value, bool quoteValues)
        {
            return quoteValues is false ? value : string.Concat("\"", value.Replace("\"", string.Empty), "\"");
        }
    }
}
