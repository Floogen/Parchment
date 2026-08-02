using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TokenizableStrings;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Resolves the placeholders an author can write into an element's text or into a trigger action.
    /// Two vocabularies meet here: Parchment's own %Token% forms, and the game's [Token] tokenizable strings. Parchment's run first so one of its values can be an argument to a game token,
    /// the same order Content Patcher's {{ }} tokens expand in.
    /// A Parchment token means the same thing wherever it appears, and differs only in whether the value is quoted: an action's arguments are split on spaces, text isn't.
    /// </summary>
    public static class TokenHelper
    {
        private const string ESCAPED_PERCENT = "%%";
        private const string ESCAPED_PERCENT_PLACEHOLDER = "\u0001";

        private static readonly Regex _tokenPattern = new Regex(@"%(?<name>[A-Za-z]+)(?:\.(?<property>[A-Za-z]+))?(?::(?<argument>[^%]+))?%", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Whether a string is worth running through <see cref="Resolve"/> at all, which is what keeps the change watch off every element that has no tokens in it.</summary>
        public static bool HasTokens(string? text)
        {
            return HasParchmentTokens(text) is true || HasGameTokens(text) is true;
        }

        /// <summary>Whether a string carries one of Parchment's own %Token% forms.</summary>
        public static bool HasParchmentTokens(string? text)
        {
            return string.IsNullOrEmpty(text) is false && text.Contains('%');
        }

        /// <summary>Whether a string carries a game tokenizable string, which anything holding an opening square bracket might.</summary>
        public static bool HasGameTokens(string? text)
        {
            return string.IsNullOrEmpty(text) is false && text.Contains('[');
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
        /// <param name="quoteValues">Whether a substituted value is wrapped in quotes. True for a trigger action or a condition, where a value containing spaces would otherwise become several arguments.</param>
        /// <param name="parseGameTokens">Whether the result is then handed to the game so its own [Token] forms resolve. False for a trigger action, which the game parses itself once it has the arguments.</param>
        public static string Resolve(string text, Element? element, bool quoteValues, bool parseGameTokens = true)
        {
            if (HasTokens(text) is false)
            {
                return text;
            }

            // Read from the authored text rather than from the resolved one, so a square bracket that arrives through a token (out of something the player typed, say) can never turn ordinary text into a parse attempt
            bool useGameTokens = parseGameTokens is true && HasGameTokens(text) is true && element?.Data.ParseTokenizableStrings is not false;

            string workingText = ResolveParchmentTokens(text, element, quoteValues, stripBrackets: useGameTokens);

            if (useGameTokens is false)
            {
                return workingText;
            }

            return ResolveGameTokens(workingText, text, element, quoteValues);
        }

        private static string ResolveParchmentTokens(string text, Element? element, bool quoteValues, bool stripBrackets)
        {
            if (HasParchmentTokens(text) is false)
            {
                return text;
            }

            // Held aside so a literal %% can't be read as an empty token, and put back once the real ones are done
            string workingText = text.Replace(ESCAPED_PERCENT, ESCAPED_PERCENT_PLACEHOLDER);

            workingText = _tokenPattern.Replace(workingText, match => ResolveToken(match, text, element, quoteValues, stripBrackets));

            return workingText.Replace(ESCAPED_PERCENT_PLACEHOLDER, "%");
        }

        /// <summary>Hands the string to the game so its [Token] forms resolve against the current save. A string the game refuses is kept as it was rather than blanked, which matches how an unknown Parchment token is left in place.
        /// Where values are quoted the tokens are read one at a time, so each result can be quoted the way Parchment's own are. A trigger action doesn't need that, as the game splits its arguments before parsing them, but a condition is parsed here and split afterwards.
        /// </summary>
        private static string ResolveGameTokens(string text, string source, Element? element, bool quoteValues)
        {
            // Made once for the whole string rather than per token, so a string holding two of the same random token still gets two different values out of them
            Random random = CreateTokenRandom(source, element);

            if (quoteValues is false)
            {
                TryParseGameText(text, source, random, out string parsedText);

                return parsedText;
            }

            return ResolveQuotedGameTokens(text, source, random);
        }

        /// <summary>Resolves each [Token] on its own and quotes what it produces, so a result holding a space stays one argument.
        /// A token that is only part of an argument, such as one written against a prefix, is left unquoted, since quoting it there would break the argument in two rather than hold it together.
        /// </summary>
        private static string ResolveQuotedGameTokens(string text, string source, Random random)
        {
            var builder = new StringBuilder();
            int index = 0;

            while (index < text.Length)
            {
                if (text[index] is not '[' || TryReadTokenSpan(text, index, out int tokenEnd) is false)
                {
                    builder.Append(text[index]);
                    index++;

                    continue;
                }

                // Quotes the author put around the token are taken over rather than kept, so what it produces is quoted once rather than twice
                bool isAlreadyQuoted = index > 0 && text[index - 1] is '"' && tokenEnd + 1 < text.Length && text[tokenEnd + 1] is '"';
                if (isAlreadyQuoted is true)
                {
                    builder.Length--;
                }

                int argumentStart = isAlreadyQuoted is true ? index - 1 : index;
                int argumentEnd = isAlreadyQuoted is true ? tokenEnd + 1 : tokenEnd;

                bool isWholeArgument = (argumentStart is 0 || char.IsWhiteSpace(text[argumentStart - 1]) is true) && (argumentEnd == text.Length - 1 || char.IsWhiteSpace(text[argumentEnd + 1]) is true);

                string token = text.Substring(index, tokenEnd - index + 1);
                bool hasParsed = TryParseGameText(token, source, random, out string parsedToken);

                // A token the game wouldn't parse is left as it was written, so quoting it would only hide the mistake behind a pair of quotes
                builder.Append(hasParsed is true && (isAlreadyQuoted is true || isWholeArgument is true) ? Format(parsedToken, quoteValues: true, stripBrackets: false) : parsedToken);

                index = argumentEnd + 1;
            }

            return builder.ToString();
        }

        /// <summary>Finds where the token opening at <paramref name="start"/> closes, counting the brackets so a token holding another token is read whole. False when nothing closes it.</summary>
        private static bool TryReadTokenSpan(string text, int start, out int end)
        {
            int depth = 0;

            for (int index = start; index < text.Length; index++)
            {
                if (text[index] is '[')
                {
                    depth++;
                }
                else if (text[index] is ']')
                {
                    depth--;

                    if (depth is 0)
                    {
                        end = index;

                        return true;
                    }
                }
            }

            end = start;

            return false;
        }

        /// <summary>Parses one tokenizable string, reporting whether the game took it. What the game refuses is handed back as it was rather than blanked, which matches how an unknown Parchment token is left in place.</summary>
        private static bool TryParseGameText(string text, string source, Random random, out string parsedText)
        {
            try
            {
                string? gameText = TokenParser.ParseText(text, random: random, customParser: null, player: Game1.player);

                if (gameText is null)
                {
                    Parchment.monitor.LogOnce($"'{source}' has a tokenizable string the game wouldn't parse. Its own log line names the token it rejected.", LogLevel.Warn);
                    parsedText = text;

                    return false;
                }

                parsedText = gameText;

                return true;
            }
            catch (Exception exception)
            {
                Parchment.monitor.LogOnce($"'{source}' has a tokenizable string that threw while parsing: {exception.Message}", LogLevel.Warn);
                parsedText = text;

                return false;
            }
        }

        /// <summary>A random source that lands on the same value every time the same string is resolved on the same day. A token that picks at random, such as [PositiveAdjective], would otherwise
        /// reroll on every condition refresh, and the change watch would drag a relayout along with it several times a second.
        /// Seeded off the day as well as the save, so the value moves overnight rather than being fixed for the life of the save.
        /// </summary>
        private static Random CreateTokenRandom(string source, Element? element)
        {
            // The game's own hash rather than string.GetHashCode, which is seeded per process and would hand back a different answer every time the game started
            return Utility.CreateDaySaveRandom(Game1.hash.GetDeterministicHashCode(source), Game1.hash.GetDeterministicHashCode(element?.Data.Id ?? string.Empty));
        }

        private static string ResolveToken(Match match, string source, Element? element, bool quoteValues, bool stripBrackets)
        {
            string name = match.Groups["name"].Value;
            string property = match.Groups["property"].Value;
            string argument = match.Groups["argument"].Value;

            switch (name.ToLowerInvariant())
            {
                case "input":
                    return ResolveInput(match, source, argument, element, quoteValues, stripBrackets);
                case "item":
                    return ResolveItem(match, source, property, element, quoteValues, stripBrackets);
                case "variable":
                    return ResolveVariable(match, source, argument, quoteValues, stripBrackets);
                case "griddisplayed":
                case "gridmatched":
                case "gridtotal":
                    return ResolveGridCount(match, source, name, argument);
            }

            // Left alone rather than blanked, since an unrecognised token is far more likely to be someone's literal text than a typo
            return match.Value;
        }

        private static string ResolveInput(Match match, string source, string inputId, Element? element, bool quoteValues, bool stripBrackets)
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

            return Format(Parchment.inputManager.GetText(inputId), quoteValues, stripBrackets);
        }

        private static string ResolveItem(Match match, string source, string property, Element? element, bool quoteValues, bool stripBrackets)
        {
            if (element is null || string.IsNullOrWhiteSpace(element.AssignedItemId))
            {
                Parchment.monitor.LogOnce($"'{source}' uses {match.Value}, which only works inside a Grid's result cell.", LogLevel.Warn);
                return match.Value;
            }

            // The bare token is the qualified ID, which is what an action needs to address the item
            if (string.IsNullOrEmpty(property) is true)
            {
                return Format(element.AssignedItemId, quoteValues, stripBrackets);
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

            return Format(ItemPropertyResolver.Resolve(property, element.AssignedItemData, element.AssignedItem) ?? string.Empty, quoteValues, stripBrackets);
        }

        private static string ResolveVariable(Match match, string source, string variableId, bool quoteValues, bool stripBrackets)
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

            if (Parchment.variableManager.TryGet(Game1.player, bookId, variableId, out string value, out string error) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' reads the variable '{variableId}', but {error}.", LogLevel.Warn);
                return match.Value;
            }

            return Format(value, quoteValues, stripBrackets);
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
        /// <param name="stripBrackets">Whether square brackets are taken out of the value. Only true for a string the game is about to parse, where a bracket the value carried in
        /// would otherwise be read as the start of a token rather than as the text it is.
        /// </param>
        private static string Format(string value, bool quoteValues, bool stripBrackets)
        {
            string plainValue = stripBrackets is false ? value : value.Replace("[", string.Empty).Replace("]", string.Empty);

            return quoteValues is false ? plainValue : string.Concat("\"", plainValue.Replace("\"", string.Empty), "\"");
        }
    }
}
