using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Links;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Reads the [color=...], [link=...] and effect markup (such as [wave] or [gradient]) an author can write into an element's text.
    /// The markup is read out of the authored text before any token resolves. It stands in the text as marker characters while the tokens do.
    /// That keeps the game from ever seeing it as one of its [Token] forms. It also means a value a token brings in (out of something the player typed, say) can never open a run of its own.
    /// </summary>
    public static class TextMarkupHelper
    {
        public const char COLOR_OPEN_MARKER = '\u0002';
        public const char COLOR_CLOSE_MARKER = '\u0003';
        public const char LINK_OPEN_MARKER = '\u0004';
        public const char LINK_CLOSE_MARKER = '\u0005';
        public const char EFFECT_OPEN_MARKER = '\u0006';
        public const char EFFECT_CLOSE_MARKER = '\u0007';
        public const char PAUSE_MARKER = '\u0008';

        public const float DEFAULT_WAVE_AMPLITUDE = 2f;
        public const float DEFAULT_WAVE_PERIOD = 1000f;
        public const float DEFAULT_SHAKE_AMPLITUDE = 1f;
        public const float DEFAULT_SHAKE_PERIOD = 80f;
        public const float DEFAULT_RAINBOW_PERIOD = 2000f;
        public const float DEFAULT_BOUNCE_AMPLITUDE = 2f;
        public const float DEFAULT_BOUNCE_PERIOD = 800f;
        public const float DEFAULT_PULSE_PERIOD = 1500f;
        public const float DEFAULT_TYPEWRITER_SPEED = 30f;
        public const float DEFAULT_TYPEWRITER_FADE = 100f;
        public const float DEFAULT_PAUSE_DURATION = 500f;
        public const float DEFAULT_SCRAMBLE_PERIOD = 80f;
        public const float DEFAULT_SCRAMBLE_SETTLE = 40f;
        public const string DEFAULT_SCRAMBLE_GLYPHS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private const string SCRAMBLE_GLYPHS_OPTION = "glyphs";
        private const string SCRAMBLE_SETTLE_OPTION = "settle";

        private const string TYPEWRITER_IMMEDIATE_OPTION = "immediate";
        private const string TYPEWRITER_FADE_OPTION = "fade";
        private const string TYPEWRITER_SOUND_OPTION = "sound";
        private const string TYPEWRITER_TAG = "[typewriter";

        // The part of any tag's value that gates it on a game state query, such as [wave=4|condition=WEATHER Here Rain]
        private const string CONDITION_PART = "condition";

        // What separates the parts of an effect's value, the same for every effect. Not a space, since a color can hold spaces of its own such as "255 215 0"
        private const char VALUE_SEPARATOR = '|';

        private const string COLOR_TAG = "color";
        private const string LINK_TAG = "link";

        // An opening [color] or [link] always carries a value and a closing tag never does, so a bare [color] or [link] is left as the text it is.
        // An effect such as [wave] may go bare, since its value only adjusts it
        private static readonly Regex _markupPattern = new Regex(@"\[(?<tag>color|link)=(?<value>[^\[\]]*)\]|\[(?<effect>wave|shake|rainbow|bounce|gradient|pulse|typewriter|underline|strike|highlight|redact|scramble)(?:=(?<effectValue>[^\[\]]*))?\]|\[/(?<closingTag>color|link|wave|shake|rainbow|bounce|gradient|pulse|typewriter|underline|strike|highlight|redact|scramble)\]|\[(?<pause>pause)(?:=(?<pauseValue>[^\[\]]*))?\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly char[] _markerCharacters = new char[] { COLOR_OPEN_MARKER, COLOR_CLOSE_MARKER, LINK_OPEN_MARKER, LINK_CLOSE_MARKER, EFFECT_OPEN_MARKER, EFFECT_CLOSE_MARKER, PAUSE_MARKER };

        /// <summary>A link tag as it was read, being which occurrence it is (-1 for one that names nothing the element defines) and the color it draws its text in.</summary>
        private readonly record struct OpenedLink(int Occurrence, Color? Color, IReadOnlyList<OpenedEffect> HoverEffects)
        {
            public static readonly OpenedLink None = new OpenedLink(-1, null, Array.Empty<OpenedEffect>());
        }

        /// <summary>An effect tag as it was read, before it has a place in the plain text to count its characters from.</summary>
        private readonly record struct OpenedEffect(TextEffectType Type, float Amplitude, float Period, IReadOnlyList<Color> Colors, TypingOptions? Typing = null)
        {
            /// <summary>How a scramble hides its text. Null for every other effect.</summary>
            public ScrambleOptions? Scramble { get; init; }

            /// <summary>Whether the effect applies, being false when its tag's condition failed. A failed effect still pairs with its closing tag so the tags around it stay matched.</summary>
            public bool IsActive { get; init; } = true;
        }

        private enum TagKind
        {
            Color,
            Link,
            Effect
        }

        /// <summary>One tag still open while the markers are taken back out, innermost last.</summary>
        private readonly record struct OpenTag(TagKind Kind, Color? Color, int Occurrence, TextEffect? Effect, bool IsSuppressed = false);

        /// <summary>Everything an opening or closing tag asked for, collected in the order the tags appear so the markers can be matched back up once tokens have resolved.</summary>
        private class MarkupRecord
        {
            public List<Color?> OpenedColors { get; } = new List<Color?>();
            public List<OpenedLink> OpenedLinks { get; } = new List<OpenedLink>();
            public List<OpenedEffect> OpenedEffects { get; } = new List<OpenedEffect>();
            public List<TextEffectType> ClosedEffects { get; } = new List<TextEffectType>();
            public List<(float Duration, string? Condition)> Pauses { get; } = new List<(float Duration, string? Condition)>();

            public bool IsEmpty => OpenedColors.Count is 0 && OpenedLinks.Count is 0 && OpenedEffects.Count is 0;
        }

        /// <summary>The conditions written into authored text's tags with a "condition=" part, in the order the tags appear, leaving out a typewriter's.
        /// A typewriter checks its condition once when it would start, so only the rest are refreshed alongside the element's own Condition. Their order is how each tag finds its result again.
        /// </summary>
        public static IReadOnlyList<string> GetInlineConditions(string? text)
        {
            if (string.IsNullOrEmpty(text) || text.Contains(CONDITION_PART, StringComparison.OrdinalIgnoreCase) is false)
            {
                return Array.Empty<string>();
            }

            List<string> conditions = new List<string>();

            foreach (Match match in _markupPattern.Matches(text))
            {
                // A pause's condition is checked once when its typewriter starts, the same as the typewriter's own, so it isn't refreshed with the rest
                if (match.Groups["closingTag"].Success || match.Groups["pause"].Success)
                {
                    continue;
                }

                if (match.Groups["effect"].Success && string.Equals(match.Groups["effect"].Value, nameof(TextEffectType.Typewriter), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? value = match.Groups["value"].Success ? match.Groups["value"].Value : match.Groups["effectValue"].Success ? match.Groups["effectValue"].Value : null;
                ExtractCondition(value, out string? condition);

                if (condition is not null)
                {
                    conditions.Add(condition);
                }
            }

            return conditions;
        }

        /// <summary>Takes a "condition=" part out of a tag's value, handing back what remains for the tag to read as it always has. Null when the value was only a condition.
        /// Everything after the first = is the query, which is why a query can hold an = of its own but not a |, as that would start the tag's next part.
        /// </summary>
        private static string? ExtractCondition(string? value, out string? condition)
        {
            condition = null;

            if (string.IsNullOrEmpty(value) || value.Contains(CONDITION_PART, StringComparison.OrdinalIgnoreCase) is false)
            {
                return value;
            }

            List<string> remainingParts = new List<string>();

            foreach (string part in value.Split(VALUE_SEPARATOR))
            {
                string trimmedPart = part.Trim();
                int separatorIndex = trimmedPart.IndexOf('=');

                if (separatorIndex > 0 && string.Equals(trimmedPart.Substring(0, separatorIndex).Trim(), CONDITION_PART, StringComparison.OrdinalIgnoreCase))
                {
                    condition = trimmedPart.Substring(separatorIndex + 1).Trim();
                    continue;
                }

                remainingParts.Add(part);
            }

            return remainingParts.Count is 0 ? null : string.Join(VALUE_SEPARATOR, remainingParts);
        }

        /// <summary>Whether a tag's condition lets it apply, which it always does without one. Each condition takes the next result the element holds, refreshed alongside its own Condition,
        /// so laying the text out again (as a changing token does every tick) never runs the query itself. One with no result yet is checked here and the result kept.
        /// </summary>
        private static bool IsConditionMet(string? condition, Element? element, ref int nextConditionIndex)
        {
            if (condition is null)
            {
                return true;
            }

            int conditionIndex = nextConditionIndex++;

            if (element is null)
            {
                return ConditionHelper.Check(condition, element);
            }

            if (conditionIndex < element.InlineConditionResults.Count)
            {
                return element.InlineConditionResults[conditionIndex];
            }

            bool isMet = ConditionHelper.Check(condition, element);

            if (conditionIndex == element.InlineConditionResults.Count)
            {
                element.InlineConditionResults.Add(isMet);
                element.InlineConditionChangedAt.Add(null);
            }

            return isMet;
        }

        /// <summary>Whether authored text holds a [typewriter], which is what puts its element on the list the typing is scheduled from. Read from the authored text, as a token can't bring one in.</summary>
        public static bool HasTypewriter(string? text)
        {
            return string.IsNullOrEmpty(text) is false && text.Contains(TYPEWRITER_TAG, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>The text with its markup taken out, for somewhere that draws plain text such as a tooltip.</summary>
        public static string RemoveMarkup(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Contains('[') is false)
            {
                return text;
            }

            return _markupPattern.Replace(text, string.Empty);
        }

        /// <summary>The value with any markers taken out, so nothing a token brings in can open or close a run.</summary>
        public static string RemoveMarkers(string value)
        {
            if (value.IndexOfAny(_markerCharacters) < 0)
            {
                return value;
            }

            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (Array.IndexOf(_markerCharacters, character) < 0)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        /// <summary>The links an element's text points at, in the order they appear and once per occurrence.
        /// Each id is looked up in the element's own links first and the book's second. An entry in the element's replaces the book's whole rather than filling in around it.
        /// One that neither defines is left out, which is what lets <see cref="Resolve"/> pair each remaining [link] with the element built for it by position.
        /// </summary>
        public static List<(string LinkId, LinkData Link)> GetLinkOccurrences(string? text, Dictionary<string, LinkData>? elementLinks, Dictionary<string, LinkData>? bookLinks)
        {
            var occurrences = new List<(string LinkId, LinkData Link)>();
            bool hasLinks = (elementLinks is not null && elementLinks.Count is not 0) || (bookLinks is not null && bookLinks.Count is not 0);

            if (string.IsNullOrEmpty(text) || text.Contains('[') is false || hasLinks is false)
            {
                return occurrences;
            }

            foreach (Match match in _markupPattern.Matches(text))
            {
                if (IsOpening(match, LINK_TAG) is false)
                {
                    continue;
                }

                string id = match.Groups["value"].Value;

                if (elementLinks is not null && TryGetLink(elementLinks, id, out string elementLinkId, out LinkData? elementLink) && elementLink is not null)
                {
                    occurrences.Add((elementLinkId, elementLink));
                }
                else if (bookLinks is not null && TryGetLink(bookLinks, id, out string bookLinkId, out LinkData? bookLink) && bookLink is not null)
                {
                    occurrences.Add((bookLinkId, bookLink));
                }
            }

            return occurrences;
        }

        /// <summary>Resolves an element's text into what to draw and where each color, link and effect applies.
        /// Tags nest, so text after an inner closing tag goes back to whatever is around it. An unclosed tag carries on to the end, a stray closing tag is dropped and a color that won't parse keeps the color around it.
        /// </summary>
        public static StyledText Resolve(string? text, Element? element)
        {
            if (string.IsNullOrEmpty(text))
            {
                return StyledText.Empty;
            }

            MarkupRecord record = new MarkupRecord();
            string markedText = MarkRuns(text, element, record);

            string resolvedText = TokenHelper.Resolve(markedText, element, quoteValues: false).Replace("\r\n", "\n");

            if (record.IsEmpty)
            {
                return new StyledText(resolvedText, Array.Empty<ColorRun>(), Array.Empty<LinkRun>(), Array.Empty<EffectRun>());
            }

            return BuildRuns(resolvedText, record);
        }

        private static bool IsOpening(Match match, string tag)
        {
            return match.Groups["tag"].Success && string.Equals(match.Groups["tag"].Value, tag, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Finds a link by the id the markup named, ignoring case, handing back the id as it was defined.</summary>
        private static bool TryGetLink(Dictionary<string, LinkData> links, string id, out string linkId, out LinkData? link)
        {
            if (links.TryGetValue(id, out link))
            {
                linkId = id;
                return true;
            }

            foreach (KeyValuePair<string, LinkData> entry in links)
            {
                if (string.Equals(entry.Key, id, StringComparison.OrdinalIgnoreCase))
                {
                    linkId = entry.Key;
                    link = entry.Value;

                    return true;
                }
            }

            linkId = id;
            link = null;

            return false;
        }

        /// <summary>Swaps each tag for a marker, collecting what each tag asks for in the order they appear.</summary>
        private static string MarkRuns(string text, Element? element, MarkupRecord record)
        {
            if (text.Contains('[') is false)
            {
                return text;
            }

            int colorDepth = 0;
            int linkDepth = 0;
            Dictionary<TextEffectType, int> effectDepths = new Dictionary<TextEffectType, int>();
            int nextOccurrence = 0;
            int nextConditionIndex = 0;
            bool hasStrayClose = false;

            string markedText = _markupPattern.Replace(text, match =>
            {
                if (IsOpening(match, COLOR_TAG))
                {
                    colorDepth++;

                    string colorValue = ExtractCondition(match.Groups["value"].Value, out string? colorCondition);
                    bool isColorActive = IsConditionMet(colorCondition, element, ref nextConditionIndex);

                    // A color whose condition failed keeps the color around it, the same as one that won't parse
                    record.OpenedColors.Add(isColorActive ? ParseColor(colorValue ?? string.Empty, text, "color") : null);

                    return COLOR_OPEN_MARKER.ToString();
                }

                if (IsOpening(match, LINK_TAG))
                {
                    linkDepth++;

                    string linkId = ExtractCondition(match.Groups["value"].Value, out string? linkCondition);
                    bool isLinkActive = IsConditionMet(linkCondition, element, ref nextConditionIndex);

                    record.OpenedLinks.Add(OpenLink(linkId, text, element, isLinkActive, ref nextOccurrence));

                    return LINK_OPEN_MARKER.ToString();
                }

                if (match.Groups["effect"].Success && TryGetEffectType(match.Groups["effect"].Value, out TextEffectType openedType))
                {
                    effectDepths[openedType] = effectDepths.GetValueOrDefault(openedType) + 1;

                    string? effectValue = ExtractCondition(match.Groups["effectValue"].Success ? match.Groups["effectValue"].Value : null, out string? effectCondition);
                    OpenedEffect openedEffect = ParseEffect(openedType, effectValue, text);

                    // A typewriter checks its condition once, when it would start, rather than coming and going with it
                    if (openedType is TextEffectType.Typewriter)
                    {
                        record.OpenedEffects.Add(openedEffect with { Typing = openedEffect.Typing?.WithCondition(effectCondition) });
                    }
                    else if (openedType is TextEffectType.Scramble)
                    {
                        // Never stepped aside like other tags, as settling needs to see the condition stop passing. It keeps its place among the element's conditions and reads the result as it draws
                        int? scrambleConditionIndex = effectCondition is null ? null : nextConditionIndex;
                        IsConditionMet(effectCondition, element, ref nextConditionIndex);

                        record.OpenedEffects.Add(openedEffect with { Scramble = openedEffect.Scramble! with { ConditionIndex = scrambleConditionIndex } });
                    }
                    else
                    {
                        record.OpenedEffects.Add(openedEffect with { IsActive = IsConditionMet(effectCondition, element, ref nextConditionIndex) });
                    }

                    return EFFECT_OPEN_MARKER.ToString();
                }

                if (match.Groups["pause"].Success)
                {
                    // Only a typewriter has anything to hold, so a pause anywhere else is taken out of the text and does nothing
                    if (effectDepths.GetValueOrDefault(TextEffectType.Typewriter) is 0)
                    {
                        Parchment.monitor.LogOnce($"'{text}' has a [pause] outside a [typewriter], where there's nothing for it to hold, so it was ignored.", LogLevel.Warn);
                        return string.Empty;
                    }

                    string? pauseValue = ExtractCondition(match.Groups["pauseValue"].Success ? match.Groups["pauseValue"].Value : null, out string? pauseCondition);
                    record.Pauses.Add((ParsePauseDuration(pauseValue, text), pauseCondition));

                    return PAUSE_MARKER.ToString();
                }

                string closingTag = match.Groups["closingTag"].Value;

                if (string.Equals(closingTag, COLOR_TAG, StringComparison.OrdinalIgnoreCase))
                {
                    if (colorDepth is 0)
                    {
                        hasStrayClose = true;
                        return string.Empty;
                    }

                    colorDepth--;
                    return COLOR_CLOSE_MARKER.ToString();
                }

                if (string.Equals(closingTag, LINK_TAG, StringComparison.OrdinalIgnoreCase))
                {
                    if (linkDepth is 0)
                    {
                        hasStrayClose = true;
                        return string.Empty;
                    }

                    linkDepth--;
                    return LINK_CLOSE_MARKER.ToString();
                }

                if (TryGetEffectType(closingTag, out TextEffectType closedType) is false || effectDepths.GetValueOrDefault(closedType) is 0)
                {
                    hasStrayClose = true;
                    return string.Empty;
                }

                effectDepths[closedType]--;
                record.ClosedEffects.Add(closedType);

                return EFFECT_CLOSE_MARKER.ToString();
            });

            if (hasStrayClose)
            {
                Parchment.monitor.LogOnce($"'{text}' has a closing tag with nothing open before it, so it was dropped.", LogLevel.Warn);
            }

            if (colorDepth > 0 || linkDepth > 0 || effectDepths.Values.Any(depth => depth > 0))
            {
                Parchment.monitor.LogOnce($"'{text}' has a tag that is never closed, so it runs to the end of the text.", LogLevel.Warn);
            }

            return markedText;
        }

        private static bool TryGetEffectType(string tag, out TextEffectType effectType)
        {
            return Enum.TryParse(tag, ignoreCase: true, out effectType);
        }

        /// <summary>Reads an effect tag's optional value, whose parts are always separated by a |. A wave, shake or bounce takes its amplitude and then its period, a rainbow takes only its period,
        /// a gradient takes its colors and a pulse takes its color and then its period. A color keeps any spaces of its own.
        /// A part that is left off or won't parse keeps its default.
        /// </summary>
        private static OpenedEffect ParseEffect(TextEffectType effectType, string? value, string source)
        {
            string tag = effectType.ToString().ToLowerInvariant();
            string[] parts = string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(VALUE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            switch (effectType)
            {
                case TextEffectType.Gradient:
                    return new OpenedEffect(effectType, 0f, 0f, ParseGradientColors(parts, tag, source));
                case TextEffectType.Pulse:
                    return ParsePulse(parts, tag, source);
                case TextEffectType.Typewriter:
                    return new OpenedEffect(effectType, 0f, 0f, Array.Empty<Color>(), ParseTypewriter(parts, tag, source));
                case TextEffectType.Underline:
                case TextEffectType.Strike:
                case TextEffectType.Highlight:
                case TextEffectType.Redact:
                    return new OpenedEffect(effectType, 0f, 0f, ParseDecorationColor(parts, tag, source));
                case TextEffectType.Scramble:
                    return new OpenedEffect(effectType, 0f, 0f, Array.Empty<Color>()) { Scramble = ParseScramble(parts, tag, source) };
                case TextEffectType.Shake:
                    return new OpenedEffect(effectType, ParseAmplitude(parts, DEFAULT_SHAKE_AMPLITUDE, tag, source), ParsePeriod(parts, 1, DEFAULT_SHAKE_PERIOD, allowZero: false, tag, source), Array.Empty<Color>());
                case TextEffectType.Bounce:
                    return new OpenedEffect(effectType, ParseAmplitude(parts, DEFAULT_BOUNCE_AMPLITUDE, tag, source), ParsePeriod(parts, 1, DEFAULT_BOUNCE_PERIOD, allowZero: false, tag, source), Array.Empty<Color>());
                case TextEffectType.Rainbow:
                    // Zero holds the colors still, which is how the game draws its own rainbow text
                    return new OpenedEffect(effectType, 0f, ParsePeriod(parts, 0, DEFAULT_RAINBOW_PERIOD, allowZero: true, tag, source), Array.Empty<Color>());
            }

            return new OpenedEffect(effectType, ParseAmplitude(parts, DEFAULT_WAVE_AMPLITUDE, tag, source), ParsePeriod(parts, 1, DEFAULT_WAVE_PERIOD, allowZero: false, tag, source), Array.Empty<Color>());
        }

        /// <summary>Reads a gradient's stops, one color per part. A stop that won't parse is left out. Fewer than two leave the gradient with nothing to blend, so it keeps the color around it.</summary>
        private static IReadOnlyList<Color> ParseGradientColors(string[] parts, string tag, string source)
        {
            List<Color> colors = new List<Color>();

            foreach (string part in parts)
            {
                if (ColorParser.TryParse(part, out Color color))
                {
                    colors.Add(color);
                }
                else
                {
                    Parchment.monitor.LogOnce($"'{source}' has a [{tag}] color of '{part}', which isn't a color Parchment can read, so it was left out.", LogLevel.Warn);
                }
            }

            if (colors.Count < 2)
            {
                Parchment.monitor.LogOnce($"'{source}' has a [{tag}] without two colors to blend between, such as [{tag}=Red|Blue], so the text keeps the color around it.", LogLevel.Warn);
                return Array.Empty<Color>();
            }

            return colors;
        }

        /// <summary>Reads a scramble's value. A number is how often its glyphs change, in milliseconds. "glyphs=…" sets the characters it picks from and "settle=milliseconds" sets how long each character takes to lock into place after the one before it.
        /// Every part is optional and the named ones can come in any order.
        /// </summary>
        private static ScrambleOptions ParseScramble(string[] parts, string tag, string source)
        {
            float period = DEFAULT_SCRAMBLE_PERIOD;
            string glyphs = DEFAULT_SCRAMBLE_GLYPHS;
            float settle = DEFAULT_SCRAMBLE_SETTLE;

            foreach (string part in parts)
            {
                if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                {
                    if (number > 0f)
                    {
                        period = number;
                    }
                    else
                    {
                        Parchment.monitor.LogOnce($"'{source}' has a [{tag}] period of '{part}', which isn't a positive number of milliseconds, so the default of {DEFAULT_SCRAMBLE_PERIOD} is used.", LogLevel.Warn);
                    }

                    continue;
                }

                int separatorIndex = part.IndexOf('=');
                string option = separatorIndex < 0 ? part : part.Substring(0, separatorIndex).Trim();
                string optionValue = separatorIndex < 0 ? string.Empty : part.Substring(separatorIndex + 1);

                if (string.Equals(option, SCRAMBLE_GLYPHS_OPTION, StringComparison.OrdinalIgnoreCase) && optionValue.Length > 0)
                {
                    // Inner spaces are kept, since a space in the set is a glyph like any other
                    glyphs = optionValue.Trim();
                }
                else if (string.Equals(option, SCRAMBLE_SETTLE_OPTION, StringComparison.OrdinalIgnoreCase) && float.TryParse(optionValue.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedSettle) && parsedSettle >= 0f)
                {
                    settle = parsedSettle;
                }
                else
                {
                    Parchment.monitor.LogOnce($"'{source}' has a [{tag}] part of '{part}', which isn't one Parchment can read, so it was ignored. Try a number of milliseconds, \"{SCRAMBLE_GLYPHS_OPTION}=…\" or \"{SCRAMBLE_SETTLE_OPTION}=milliseconds\".{GetSeparatorHint(part, tag)}", LogLevel.Warn);
                }
            }

            return new ScrambleOptions(period, glyphs, settle);
        }

        /// <summary>Reads a pause's optional length in milliseconds. Left off (or one that won't parse), it holds for <see cref="DEFAULT_PAUSE_DURATION"/>.</summary>
        private static float ParsePauseDuration(string? value, string source)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DEFAULT_PAUSE_DURATION;
            }

            if (float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float duration) && duration >= 0f)
            {
                return duration;
            }

            Parchment.monitor.LogOnce($"'{source}' has a [pause] of '{value}', which isn't a number of milliseconds that isn't negative, so the default of {DEFAULT_PAUSE_DURATION} is used.{GetSeparatorHint(value, "pause")}", LogLevel.Warn);
            return DEFAULT_PAUSE_DURATION;
        }

        /// <summary>Reads a decoration's optional color. Left off (or one that won't parse), the decoration takes its default, which for most is the color of the text it decorates.</summary>
        private static IReadOnlyList<Color> ParseDecorationColor(string[] parts, string tag, string source)
        {
            if (parts.Length is 0)
            {
                return Array.Empty<Color>();
            }

            if (parts.Length > 1)
            {
                Parchment.monitor.LogOnce($"'{source}' has a [{tag}] with more than one part, so everything after its color was ignored.", LogLevel.Warn);
            }

            if (ColorParser.TryParse(parts[0], out Color color))
            {
                return new Color[] { color };
            }

            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] color of '{parts[0]}', which isn't a color Parchment can read, so its default is used.", LogLevel.Warn);
            return Array.Empty<Color>();
        }

        /// <summary>Reads a typewriter's value. Its numbers are its speed and then its delay, both in milliseconds. Any other part is an option: "immediate" to start without waiting for the typewriters before it,
        /// "fade" (or "fade=milliseconds") to fade each character in rather than pop it in. "sound=cue" plays a sound as characters appear. Every part is optional and the options can come in any order after the numbers.
        /// </summary>
        private static TypingOptions ParseTypewriter(string[] parts, string tag, string source)
        {
            float speed = DEFAULT_TYPEWRITER_SPEED;
            float delay = 0f;
            int numberCount = 0;
            bool isImmediate = false;
            float fadeDuration = 0f;
            string? sound = null;

            foreach (string part in parts)
            {
                if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                {
                    if (numberCount is 0)
                    {
                        speed = ReadTypewriterNumber(number, part, "speed", DEFAULT_TYPEWRITER_SPEED, allowZero: false, tag, source);
                    }
                    else if (numberCount is 1)
                    {
                        delay = ReadTypewriterNumber(number, part, "delay", 0f, allowZero: true, tag, source);
                    }
                    else
                    {
                        Parchment.monitor.LogOnce($"'{source}' has a [{tag}] with more than two numbers, so '{part}' was ignored. The first is its speed and the second its delay.", LogLevel.Warn);
                    }

                    numberCount++;
                    continue;
                }

                int separatorIndex = part.IndexOf('=');
                string option = separatorIndex < 0 ? part : part.Substring(0, separatorIndex).Trim();
                string? optionValue = separatorIndex < 0 ? null : part.Substring(separatorIndex + 1).Trim();

                if (string.Equals(option, TYPEWRITER_IMMEDIATE_OPTION, StringComparison.OrdinalIgnoreCase))
                {
                    isImmediate = true;
                }
                else if (string.Equals(option, TYPEWRITER_FADE_OPTION, StringComparison.OrdinalIgnoreCase))
                {
                    fadeDuration = DEFAULT_TYPEWRITER_FADE;

                    if (optionValue is not null)
                    {
                        if (float.TryParse(optionValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFade) && parsedFade >= 0f)
                        {
                            fadeDuration = parsedFade;
                        }
                        else
                        {
                            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] fade of '{optionValue}', which isn't a number of milliseconds that isn't negative, so the default of {DEFAULT_TYPEWRITER_FADE} is used.", LogLevel.Warn);
                        }
                    }
                }
                else if (string.Equals(option, TYPEWRITER_SOUND_OPTION, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(optionValue) is false)
                {
                    sound = optionValue;
                }
                else
                {
                    Parchment.monitor.LogOnce($"'{source}' has a [{tag}] option of '{part}', which isn't one Parchment knows, so it was ignored. Try \"{TYPEWRITER_IMMEDIATE_OPTION}\", \"{TYPEWRITER_FADE_OPTION}\" or \"{TYPEWRITER_SOUND_OPTION}=cue\".{GetSeparatorHint(part, tag)}", LogLevel.Warn);
                }
            }

            return new TypingOptions(speed, delay, isImmediate, fadeDuration, sound);
        }

        private static float ReadTypewriterNumber(float number, string part, string label, float defaultValue, bool allowZero, string tag, string source)
        {
            if (number > 0f || (allowZero && number == 0f))
            {
                return number;
            }

            string expected = allowZero ? "a number of milliseconds that isn't negative" : "a positive number of milliseconds";
            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] {label} of '{part}', which isn't {expected}, so the default of {defaultValue} is used.", LogLevel.Warn);

            return defaultValue;
        }

        /// <summary>Reads a pulse's color and then its period. The color is required, as without one there is nothing to fade towards and the text keeps the color around it.</summary>
        private static OpenedEffect ParsePulse(string[] parts, string tag, string source)
        {
            float period = ParsePeriod(parts, 1, DEFAULT_PULSE_PERIOD, allowZero: false, tag, source);

            if (parts.Length is 0)
            {
                Parchment.monitor.LogOnce($"'{source}' has a [{tag}] without a color to fade towards, such as [{tag}=Gold], so the text keeps the color around it.", LogLevel.Warn);
                return new OpenedEffect(TextEffectType.Pulse, 0f, period, Array.Empty<Color>());
            }

            if (ColorParser.TryParse(parts[0], out Color color) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' has a [{tag}] color of '{parts[0]}', which isn't a color Parchment can read, so the text keeps the color around it.", LogLevel.Warn);
                return new OpenedEffect(TextEffectType.Pulse, 0f, period, Array.Empty<Color>());
            }

            return new OpenedEffect(TextEffectType.Pulse, 0f, period, new Color[] { color });
        }

        private static float ParseAmplitude(string[] parts, float defaultAmplitude, string tag, string source)
        {
            if (parts.Length is 0)
            {
                return defaultAmplitude;
            }

            if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float amplitude))
            {
                return amplitude;
            }

            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] amplitude of '{parts[0]}', which isn't a number, so the default of {defaultAmplitude} is used.{GetSeparatorHint(parts[0], tag)}", LogLevel.Warn);
            return defaultAmplitude;
        }

        /// <param name="partIndex">Which part of the value the period is, being the second for an effect that takes an amplitude first.</param>
        /// <param name="allowZero">Whether a period of zero is valid, which holds the effect still rather than running it.</param>
        private static float ParsePeriod(string[] parts, int partIndex, float defaultPeriod, bool allowZero, string tag, string source)
        {
            if (parts.Length <= partIndex)
            {
                return defaultPeriod;
            }

            if (float.TryParse(parts[partIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out float period) && (period > 0f || (allowZero && period == 0f)))
            {
                return period;
            }

            string expected = allowZero ? "a number of milliseconds that isn't negative" : "a positive number of milliseconds";
            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] period of '{parts[partIndex]}', which isn't {expected}, so the default of {defaultPeriod} is used.{GetSeparatorHint(parts[partIndex], tag)}", LogLevel.Warn);

            return defaultPeriod;
        }

        /// <summary>A pointer to the | separator when a number that failed to parse holds a space, which is what writing the parts space separated (such as [wave=4 500]) leaves behind.</summary>
        private static string GetSeparatorHint(string part, string tag)
        {
            if (part.Contains(' ') is false)
            {
                return string.Empty;
            }

            return $" The parts of an effect's value are separated by a |, such as [{tag}={string.Join(VALUE_SEPARATOR, part.Split(' ', StringSplitOptions.RemoveEmptyEntries))}].";
        }

        /// <summary>Pairs a [link] tag with the link element built for it, being the next of the element's links still unclaimed. The id was already looked up when that element was built, so it is only compared here rather than looked up again.
        /// A tag naming nothing the element or its book defines had no element built for it, so it doesn't claim one. It is kept as plain text, though its marker still goes in so its closing tag pairs up.
        /// </summary>
        /// <param name="isActive">Whether the tag's own condition passed. A link that failed it (or whose definition's Condition failed) still claims its element so the links after it pair up. Its text is drawn plain.</param>
        private static OpenedLink OpenLink(string? id, string source, Element? element, bool isActive, ref int nextOccurrence)
        {
            id ??= string.Empty;

            if (element is null || element.Data is not ILinkHost)
            {
                Parchment.monitor.LogOnce($"'{source}' has [link={id}], but links only work on Title, Heading, Paragraph and PageNumber elements. The text is drawn without it.", LogLevel.Warn);
                return OpenedLink.None;
            }

            if (nextOccurrence >= element.Children.Count || element.Children[nextOccurrence].Data is not LinkElementData linkData || string.Equals(linkData.LinkId, id, StringComparison.OrdinalIgnoreCase) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' has [link={id}], which isn't in the element's \"Links\" or the book's. The text is drawn without it.", LogLevel.Warn);
                return OpenedLink.None;
            }

            Element linkElement = element.Children[nextOccurrence];

            if (isActive is false || linkElement.IsVisible is false)
            {
                nextOccurrence++;
                return OpenedLink.None;
            }

            LinkData link = linkData.Link;
            Color? linkColor = string.IsNullOrWhiteSpace(link.TextColor) ? null : ParseColor(link.TextColor, source, $"link '{id}' TextColor");
            OpenedLink openedLink = new OpenedLink(nextOccurrence, linkColor, ParseHoverEffects(link, id, source));
            nextOccurrence++;

            return openedLink;
        }

        /// <summary>Turns each hovered link's parsed effects into effects placed over the whole of the link's text. A link that was never closed runs to the end of the text.</summary>
        private static IReadOnlyDictionary<int, IReadOnlyList<TextEffect>> BuildLinkHoverEffects(Dictionary<int, (OpenedLink Link, int Start, int End)> hoverLinkSpans, int textLength)
        {
            Dictionary<int, IReadOnlyList<TextEffect>> linkHoverEffects = new Dictionary<int, IReadOnlyList<TextEffect>>();

            foreach (KeyValuePair<int, (OpenedLink Link, int Start, int End)> span in hoverLinkSpans)
            {
                int end = span.Value.End < 0 ? textLength : span.Value.End;
                List<TextEffect> effects = new List<TextEffect>(span.Value.Link.HoverEffects.Count);

                foreach (OpenedEffect hoverEffect in span.Value.Link.HoverEffects)
                {
                    effects.Add(new TextEffect(hoverEffect.Type, hoverEffect.Amplitude, hoverEffect.Period, span.Value.Start, hoverEffect.Colors) { Length = end - span.Value.Start });
                }

                linkHoverEffects[span.Key] = effects;
            }

            return linkHoverEffects;
        }

        /// <summary>Reads a link's hover effects, each written the way its inline tag is without the brackets, such as "wave=3|600". An entry naming no effect Parchment knows is skipped with a warning.</summary>
        private static IReadOnlyList<OpenedEffect> ParseHoverEffects(LinkData link, string id, string source)
        {
            List<OpenedEffect>? hoverEffects = null;

            foreach (string entry in link.GetHoverEffects())
            {
                // Brackets are forgiven, since an entry copied from the text would otherwise fail for the sake of them
                string trimmedEntry = entry.Trim().TrimStart('[').TrimEnd(']');
                int separatorIndex = trimmedEntry.IndexOf('=');

                string name = separatorIndex < 0 ? trimmedEntry : trimmedEntry.Substring(0, separatorIndex);
                string? value = separatorIndex < 0 ? null : trimmedEntry.Substring(separatorIndex + 1);

                // A typewriter reveals text once rather than coming and going with the cursor. A redaction or a scramble would hide the very text the cursor is on. None of them has a meaning as a hover effect
                if (TryGetEffectType(name.Trim(), out TextEffectType effectType) is false || effectType is TextEffectType.Typewriter or TextEffectType.Redact or TextEffectType.Scramble)
                {
                    Parchment.monitor.LogOnce($"The link '{id}' has a hover effect of '{entry}', which isn't an effect that can apply on hover. Try one of: {string.Join(", ", Enum.GetValues<TextEffectType>().Where(hoverType => hoverType is not (TextEffectType.Typewriter or TextEffectType.Redact or TextEffectType.Scramble)).Select(hoverType => hoverType.ToString().ToLowerInvariant()))}.", LogLevel.Warn);
                    continue;
                }

                // A hover effect lasts only as long as the cursor does, which the link's own Condition already governs, so a condition of its own is left out rather than half supported
                value = ExtractCondition(value, out string? hoverCondition);

                if (hoverCondition is not null)
                {
                    Parchment.monitor.LogOnce($"The link '{id}' has a hover effect of '{entry}' with a condition, which hover effects don't take, so the effect always applies. Give the link a \"Condition\" instead.", LogLevel.Warn);
                }

                // Named in a way that reads naturally inside the quotes ParseEffect puts around its source, so a warning says which link the bad value came from
                hoverEffects ??= new List<OpenedEffect>();
                hoverEffects.Add(ParseEffect(effectType, value, $"{source}' link '{id}"));
            }

            return hoverEffects is null ? Array.Empty<OpenedEffect>() : hoverEffects;
        }

        /// <summary>The color a tag or link asks for. Null when it won't parse, which keeps the color around it.</summary>
        private static Color? ParseColor(string value, string source, string label)
        {
            if (ColorParser.TryParse(value, out Color color))
            {
                return color;
            }

            Parchment.monitor.LogOnce($"'{source}' has a {label} of '{value}', which isn't a color Parchment can read, so the text keeps the color around it.", LogLevel.Warn);
            return null;
        }

        /// <summary>Takes the markers back out of the resolved text, recording the colors, links and effects each one opened or closed against the plain text that is left.
        /// A closing marker closes the innermost tag of its own kind, so tags of different kinds that overlap rather than nest still pair up the way they were written.
        /// </summary>
        private static StyledText BuildRuns(string resolvedText, MarkupRecord record)
        {
            StringBuilder plainText = new StringBuilder(resolvedText.Length);
            List<ColorRun> colorRuns = new List<ColorRun>();
            List<LinkRun> linkRuns = new List<LinkRun>();
            List<EffectRun> effectRuns = new List<EffectRun>();
            List<OpenTag> openTags = new List<OpenTag>();

            // Where each link with hover effects starts and ends in the plain text, by occurrence. The end stays -1 until its closing tag is reached
            Dictionary<int, (OpenedLink Link, int Start, int End)> hoverLinkSpans = new Dictionary<int, (OpenedLink Link, int Start, int End)>();

            int nextColorIndex = 0;
            int nextLinkIndex = 0;
            int nextEffectIndex = 0;
            int nextClosedEffectIndex = 0;
            int nextTypingOrdinal = 0;
            int nextPauseIndex = 0;

            Color? currentColor = null;
            int colorRunStart = 0;
            int currentOccurrence = -1;
            int linkRunStart = 0;
            IReadOnlyList<TextEffect> currentEffects = Array.Empty<TextEffect>();
            int effectRunStart = 0;

            foreach (char character in resolvedText)
            {
                switch (character)
                {
                    case COLOR_OPEN_MARKER:
                        Color? openedColor = nextColorIndex < record.OpenedColors.Count ? record.OpenedColors[nextColorIndex] : null;
                        nextColorIndex++;

                        openTags.Add(new OpenTag(TagKind.Color, openedColor, -1, null));
                        break;
                    case LINK_OPEN_MARKER:
                        OpenedLink openedLink = nextLinkIndex < record.OpenedLinks.Count ? record.OpenedLinks[nextLinkIndex] : OpenedLink.None;
                        nextLinkIndex++;

                        openTags.Add(new OpenTag(TagKind.Link, openedLink.Color, openedLink.Occurrence, null));

                        if (openedLink.Occurrence >= 0 && openedLink.HoverEffects.Count is not 0)
                        {
                            hoverLinkSpans[openedLink.Occurrence] = (openedLink, plainText.Length, -1);
                        }
                        break;
                    case EFFECT_OPEN_MARKER:
                        if (nextEffectIndex < record.OpenedEffects.Count)
                        {
                            OpenedEffect openedEffect = record.OpenedEffects[nextEffectIndex];
                            int typingOrdinal = openedEffect.Typing is null ? -1 : nextTypingOrdinal++;

                            openTags.Add(new OpenTag(TagKind.Effect, null, -1, new TextEffect(openedEffect.Type, openedEffect.Amplitude, openedEffect.Period, plainText.Length, openedEffect.Colors) { Typing = openedEffect.Typing, TypingOrdinal = typingOrdinal, Scramble = openedEffect.Scramble }, IsSuppressed: openedEffect.IsActive is false));
                        }

                        nextEffectIndex++;
                        break;
                    case PAUSE_MARKER:
                        if (nextPauseIndex < record.Pauses.Count && GetInnermostTypewriter() is TextEffect pausedTypewriter)
                        {
                            (float pauseDuration, string? pauseCondition) = record.Pauses[nextPauseIndex];
                            pausedTypewriter.Pauses.Add(new TypingPause(plainText.Length - pausedTypewriter.Start, pauseDuration, pauseCondition));
                        }

                        nextPauseIndex++;

                        // Takes no room in the text, so nothing about the runs around it changes
                        continue;
                    case COLOR_CLOSE_MARKER:
                        CloseInnermost(tag => tag.Kind is TagKind.Color);
                        break;
                    case LINK_CLOSE_MARKER:
                        OpenTag? closedLinkTag = CloseInnermost(tag => tag.Kind is TagKind.Link);

                        if (closedLinkTag is OpenTag closedLink && hoverLinkSpans.TryGetValue(closedLink.Occurrence, out var closedSpan))
                        {
                            hoverLinkSpans[closedLink.Occurrence] = (closedSpan.Link, closedSpan.Start, plainText.Length);
                        }
                        break;
                    case EFFECT_CLOSE_MARKER:
                        if (nextClosedEffectIndex < record.ClosedEffects.Count)
                        {
                            TextEffectType closedType = record.ClosedEffects[nextClosedEffectIndex];
                            OpenTag? closedTag = CloseInnermost(tag => tag.Kind is TagKind.Effect && tag.Effect?.Type == closedType);

                            if (closedTag?.Effect is TextEffect closedEffect)
                            {
                                closedEffect.Length = plainText.Length - closedEffect.Start;
                            }
                        }

                        nextClosedEffectIndex++;
                        break;
                    default:
                        plainText.Append(character);
                        continue;
                }

                ChangeColor(GetInnermostColor());
                ChangeOccurrence(GetInnermostOccurrence());
                ChangeEffects(GetOpenEffects());
            }

            ChangeColor(null);
            ChangeOccurrence(-1);
            ChangeEffects(Array.Empty<TextEffect>());

            // An effect that was never closed runs to the end of the text, so that is where its length is measured to
            foreach (OpenTag openTag in openTags)
            {
                if (openTag.Effect is TextEffect unclosedEffect)
                {
                    unclosedEffect.Length = plainText.Length - unclosedEffect.Start;
                }
            }

            return new StyledText(plainText.ToString(), colorRuns, linkRuns, effectRuns, BuildLinkHoverEffects(hoverLinkSpans, plainText.Length));

            OpenTag? CloseInnermost(Predicate<OpenTag> isMatch)
            {
                for (int index = openTags.Count - 1; index >= 0; index--)
                {
                    if (isMatch(openTags[index]))
                    {
                        OpenTag closedTag = openTags[index];
                        openTags.RemoveAt(index);

                        return closedTag;
                    }
                }

                return null;
            }

            // The typewriter a pause holds, being the innermost one open where it sits
            TextEffect? GetInnermostTypewriter()
            {
                for (int index = openTags.Count - 1; index >= 0; index--)
                {
                    if (openTags[index].Effect is TextEffect effect && effect.Typing is not null)
                    {
                        return effect;
                    }
                }

                return null;
            }

            // A tag whose color is unset or wouldn't parse is skipped, so the color around it carries on through it
            Color? GetInnermostColor()
            {
                for (int index = openTags.Count - 1; index >= 0; index--)
                {
                    if (openTags[index].Color is Color color)
                    {
                        return color;
                    }
                }

                return null;
            }

            // A link that names nothing the element defines is plain text, so it never hides a link around it
            int GetInnermostOccurrence()
            {
                for (int index = openTags.Count - 1; index >= 0; index--)
                {
                    if (openTags[index].Kind is TagKind.Link && openTags[index].Occurrence >= 0)
                    {
                        return openTags[index].Occurrence;
                    }
                }

                return -1;
            }

            // Every open effect applies, not just the innermost, so a wave inside another effect moves with both
            IReadOnlyList<TextEffect> GetOpenEffects()
            {
                List<TextEffect>? effects = null;

                foreach (OpenTag tag in openTags)
                {
                    if (tag.Effect is not null && tag.IsSuppressed is false)
                    {
                        effects ??= new List<TextEffect>();
                        effects.Add(tag.Effect);
                    }
                }

                return effects is null ? Array.Empty<TextEffect>() : effects;
            }

            void ChangeColor(Color? nextColor)
            {
                if (nextColor == currentColor)
                {
                    return;
                }

                if (currentColor is Color runColor && plainText.Length > colorRunStart)
                {
                    AddColorRun(colorRunStart, plainText.Length - colorRunStart, runColor);
                }

                currentColor = nextColor;
                colorRunStart = plainText.Length;
            }

            void ChangeOccurrence(int nextOccurrence)
            {
                if (nextOccurrence == currentOccurrence)
                {
                    return;
                }

                if (currentOccurrence >= 0 && plainText.Length > linkRunStart)
                {
                    linkRuns.Add(new LinkRun(linkRunStart, plainText.Length - linkRunStart, currentOccurrence));
                }

                currentOccurrence = nextOccurrence;
                linkRunStart = plainText.Length;
            }

            // Compared by the effects themselves rather than by list, as the list is rebuilt after every tag even when nothing about the effects changed
            void ChangeEffects(IReadOnlyList<TextEffect> nextEffects)
            {
                if (nextEffects.SequenceEqual(currentEffects))
                {
                    return;
                }

                if (currentEffects.Count is not 0 && plainText.Length > effectRunStart)
                {
                    effectRuns.Add(new EffectRun(effectRunStart, plainText.Length - effectRunStart, currentEffects));
                }

                currentEffects = nextEffects;
                effectRunStart = plainText.Length;
            }

            void AddColorRun(int start, int length, Color color)
            {
                // Merged into the run before it when the two touch and match, as happens when one run closes and an identical one opens straight after
                if (colorRuns.Count is not 0 && colorRuns[^1].End == start && colorRuns[^1].Color == color)
                {
                    ColorRun previousRun = colorRuns[^1];
                    colorRuns[^1] = new ColorRun(previousRun.Start, previousRun.Length + length, color);

                    return;
                }

                colorRuns.Add(new ColorRun(start, length, color));
            }
        }
    }
}
