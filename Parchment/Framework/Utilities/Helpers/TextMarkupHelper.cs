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

        public const float DEFAULT_WAVE_AMPLITUDE = 2f;
        public const float DEFAULT_WAVE_PERIOD = 1000f;
        public const float DEFAULT_SHAKE_AMPLITUDE = 1f;
        public const float DEFAULT_SHAKE_PERIOD = 80f;
        public const float DEFAULT_RAINBOW_PERIOD = 2000f;
        public const float DEFAULT_BOUNCE_AMPLITUDE = 2f;
        public const float DEFAULT_BOUNCE_PERIOD = 800f;
        public const float DEFAULT_PULSE_PERIOD = 1500f;

        // What separates a color from the rest of an effect's value, since a color can hold spaces of its own such as "255 215 0"
        private const char COLOR_VALUE_SEPARATOR = '|';

        private const string COLOR_TAG = "color";
        private const string LINK_TAG = "link";

        // An opening [color] or [link] always carries a value and a closing tag never does, so a bare [color] or [link] is left as the text it is.
        // An effect such as [wave] may go bare, since its value only adjusts it
        private static readonly Regex _markupPattern = new Regex(@"\[(?<tag>color|link)=(?<value>[^\[\]]*)\]|\[(?<effect>wave|shake|rainbow|bounce|gradient|pulse)(?:=(?<effectValue>[^\[\]]*))?\]|\[/(?<closingTag>color|link|wave|shake|rainbow|bounce|gradient|pulse)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly char[] _markerCharacters = new char[] { COLOR_OPEN_MARKER, COLOR_CLOSE_MARKER, LINK_OPEN_MARKER, LINK_CLOSE_MARKER, EFFECT_OPEN_MARKER, EFFECT_CLOSE_MARKER };

        /// <summary>A link tag as it was read, being which occurrence it is (-1 for one that names nothing the element defines) and the color it draws its text in.</summary>
        private readonly record struct OpenedLink(int Occurrence, Color? Color);

        /// <summary>An effect tag as it was read, before it has a place in the plain text to count its characters from.</summary>
        private readonly record struct OpenedEffect(TextEffectType Type, float Amplitude, float Period, IReadOnlyList<Color> Colors);

        private enum TagKind
        {
            Color,
            Link,
            Effect
        }

        /// <summary>One tag still open while the markers are taken back out, innermost last.</summary>
        private readonly record struct OpenTag(TagKind Kind, Color? Color, int Occurrence, TextEffect? Effect);

        /// <summary>Everything an opening or closing tag asked for, collected in the order the tags appear so the markers can be matched back up once tokens have resolved.</summary>
        private class MarkupRecord
        {
            public List<Color?> OpenedColors { get; } = new List<Color?>();
            public List<OpenedLink> OpenedLinks { get; } = new List<OpenedLink>();
            public List<OpenedEffect> OpenedEffects { get; } = new List<OpenedEffect>();
            public List<TextEffectType> ClosedEffects { get; } = new List<TextEffectType>();

            public bool IsEmpty => OpenedColors.Count is 0 && OpenedLinks.Count is 0 && OpenedEffects.Count is 0;
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
            bool hasStrayClose = false;

            string markedText = _markupPattern.Replace(text, match =>
            {
                if (IsOpening(match, COLOR_TAG))
                {
                    colorDepth++;
                    record.OpenedColors.Add(ParseColor(match.Groups["value"].Value, text, "color"));

                    return COLOR_OPEN_MARKER.ToString();
                }

                if (IsOpening(match, LINK_TAG))
                {
                    linkDepth++;
                    record.OpenedLinks.Add(OpenLink(match.Groups["value"].Value, text, element, ref nextOccurrence));

                    return LINK_OPEN_MARKER.ToString();
                }

                if (match.Groups["effect"].Success && TryGetEffectType(match.Groups["effect"].Value, out TextEffectType openedType))
                {
                    effectDepths[openedType] = effectDepths.GetValueOrDefault(openedType) + 1;
                    record.OpenedEffects.Add(ParseEffect(openedType, match.Groups["effectValue"].Success ? match.Groups["effectValue"].Value : null, text));

                    return EFFECT_OPEN_MARKER.ToString();
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

        /// <summary>Reads an effect tag's optional value. A wave, shake or bounce takes its amplitude and then its period, separated by a space, while a rainbow takes only its period.
        /// An effect that takes colors separates them from each other and from anything else with a |, as a color can hold spaces of its own.
        /// A part that is left off or won't parse keeps its default.
        /// </summary>
        private static OpenedEffect ParseEffect(TextEffectType effectType, string? value, string source)
        {
            string tag = effectType.ToString().ToLowerInvariant();

            switch (effectType)
            {
                case TextEffectType.Gradient:
                    return new OpenedEffect(effectType, 0f, 0f, ParseGradientColors(value, tag, source));
                case TextEffectType.Pulse:
                    return ParsePulse(value, tag, source);
            }

            string[] parts = string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (effectType)
            {
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

        /// <summary>Reads a gradient's stops, each a color separated from the next by a |. A stop that won't parse is left out. Fewer than two leave the gradient with nothing to blend, so it keeps the color around it.</summary>
        private static IReadOnlyList<Color> ParseGradientColors(string? value, string tag, string source)
        {
            List<Color> colors = new List<Color>();

            if (string.IsNullOrWhiteSpace(value) is false)
            {
                foreach (string part in value.Split(COLOR_VALUE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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
            }

            if (colors.Count < 2)
            {
                Parchment.monitor.LogOnce($"'{source}' has a [{tag}] without two colors to blend between, such as [{tag}=Red|Blue], so the text keeps the color around it.", LogLevel.Warn);
                return Array.Empty<Color>();
            }

            return colors;
        }

        /// <summary>Reads a pulse's color and then its period, separated by a |. The color is required, as without one there is nothing to fade towards and the text keeps the color around it.</summary>
        private static OpenedEffect ParsePulse(string? value, string tag, string source)
        {
            string[] parts = string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(COLOR_VALUE_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] amplitude of '{parts[0]}', which isn't a number, so the default of {defaultAmplitude} is used.", LogLevel.Warn);
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
            Parchment.monitor.LogOnce($"'{source}' has a [{tag}] period of '{parts[partIndex]}', which isn't {expected}, so the default of {defaultPeriod} is used.", LogLevel.Warn);

            return defaultPeriod;
        }

        /// <summary>Pairs a [link] tag with the link element built for it, being the next of the element's links still unclaimed. The id was already looked up when that element was built, so it is only compared here rather than looked up again.
        /// A tag naming nothing the element or its book defines had no element built for it, so it doesn't claim one. It is kept as plain text, though its marker still goes in so its closing tag pairs up.
        /// </summary>
        private static OpenedLink OpenLink(string id, string source, Element? element, ref int nextOccurrence)
        {
            if (element is null || element.Data is not ILinkHost)
            {
                Parchment.monitor.LogOnce($"'{source}' has [link={id}], but links only work on Title, Heading, Paragraph and PageNumber elements. The text is drawn without it.", LogLevel.Warn);
                return new OpenedLink(-1, null);
            }

            if (nextOccurrence >= element.Children.Count || element.Children[nextOccurrence].Data is not LinkElementData linkData || string.Equals(linkData.LinkId, id, StringComparison.OrdinalIgnoreCase) is false)
            {
                Parchment.monitor.LogOnce($"'{source}' has [link={id}], which isn't in the element's \"Links\" or the book's. The text is drawn without it.", LogLevel.Warn);
                return new OpenedLink(-1, null);
            }

            LinkData link = linkData.Link;
            Color? linkColor = string.IsNullOrWhiteSpace(link.TextColor) ? null : ParseColor(link.TextColor, source, $"link '{id}' TextColor");
            OpenedLink openedLink = new OpenedLink(nextOccurrence, linkColor);
            nextOccurrence++;

            return openedLink;
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

            int nextColorIndex = 0;
            int nextLinkIndex = 0;
            int nextEffectIndex = 0;
            int nextClosedEffectIndex = 0;

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
                        OpenedLink openedLink = nextLinkIndex < record.OpenedLinks.Count ? record.OpenedLinks[nextLinkIndex] : new OpenedLink(-1, null);
                        nextLinkIndex++;

                        openTags.Add(new OpenTag(TagKind.Link, openedLink.Color, openedLink.Occurrence, null));
                        break;
                    case EFFECT_OPEN_MARKER:
                        if (nextEffectIndex < record.OpenedEffects.Count)
                        {
                            OpenedEffect openedEffect = record.OpenedEffects[nextEffectIndex];
                            openTags.Add(new OpenTag(TagKind.Effect, null, -1, new TextEffect(openedEffect.Type, openedEffect.Amplitude, openedEffect.Period, plainText.Length, openedEffect.Colors)));
                        }

                        nextEffectIndex++;
                        break;
                    case COLOR_CLOSE_MARKER:
                        CloseInnermost(tag => tag.Kind is TagKind.Color);
                        break;
                    case LINK_CLOSE_MARKER:
                        CloseInnermost(tag => tag.Kind is TagKind.Link);
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

            return new StyledText(plainText.ToString(), colorRuns, linkRuns, effectRuns);

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
                    if (tag.Effect is not null)
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
