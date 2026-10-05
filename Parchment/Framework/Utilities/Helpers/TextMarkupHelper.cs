using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Links;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Reads the [color=...] and [link=...] markup an author can write into an element's text.
    /// The markup is read out of the authored text before any token resolves. It stands in the text as marker characters while the tokens do.
    /// That keeps the game from ever seeing it as one of its [Token] forms. It also means a value a token brings in (out of something the player typed, say) can never open a run of its own.
    /// </summary>
    public static class TextMarkupHelper
    {
        public const char COLOR_OPEN_MARKER = '\u0002';
        public const char COLOR_CLOSE_MARKER = '\u0003';
        public const char LINK_OPEN_MARKER = '\u0004';
        public const char LINK_CLOSE_MARKER = '\u0005';

        private const string COLOR_TAG = "color";
        private const string LINK_TAG = "link";

        // An opening tag always carries a value and a closing one never does, so a bare [color] or [link] is left as the text it is
        private static readonly Regex _markupPattern = new Regex(@"\[(?<tag>color|link)=(?<value>[^\[\]]*)\]|\[/(?<closingTag>color|link)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] _markers = new string[] { COLOR_OPEN_MARKER.ToString(), COLOR_CLOSE_MARKER.ToString(), LINK_OPEN_MARKER.ToString(), LINK_CLOSE_MARKER.ToString() };

        /// <summary>A link tag as it was read, being which occurrence it is (-1 for one that names nothing the element defines) and the color it draws its text in.</summary>
        private readonly record struct OpenedLink(int Occurrence, Color? Color);

        /// <summary>One tag still open while the markers are taken back out, innermost last.</summary>
        private readonly record struct OpenTag(bool IsLink, Color? Color, int Occurrence);

        /// <summary>The text with its markup taken out, for somewhere that draws in a single color such as a tooltip.</summary>
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
            if (value.IndexOfAny(new char[] { COLOR_OPEN_MARKER, COLOR_CLOSE_MARKER, LINK_OPEN_MARKER, LINK_CLOSE_MARKER }) < 0)
            {
                return value;
            }

            foreach (string marker in _markers)
            {
                value = value.Replace(marker, string.Empty);
            }

            return value;
        }

        /// <summary>The links an element's text points at, in the order they appear and once per occurrence.
        /// Read the same way <see cref="Resolve"/> numbers them, which is what lets each occurrence's element be found again by its position.
        /// </summary>
        public static List<(string LinkId, LinkData Link)> GetLinkOccurrences(string? text, Dictionary<string, LinkData>? links)
        {
            var occurrences = new List<(string LinkId, LinkData Link)>();

            if (string.IsNullOrEmpty(text) || text.Contains('[') is false || links is null || links.Count is 0)
            {
                return occurrences;
            }

            foreach (Match match in _markupPattern.Matches(text))
            {
                if (IsOpening(match, LINK_TAG) is false)
                {
                    continue;
                }

                if (TryGetLink(links, match.Groups["value"].Value, out string linkId, out LinkData? link) && link is not null)
                {
                    occurrences.Add((linkId, link));
                }
            }

            return occurrences;
        }

        /// <summary>Resolves an element's text into what to draw and where each color and link applies.
        /// Tags nest, so text after an inner closing tag goes back to whatever is around it. An unclosed tag carries on to the end, a stray closing tag is dropped and a color that won't parse keeps the color around it.
        /// </summary>
        public static StyledText Resolve(string? text, Element? element)
        {
            if (string.IsNullOrEmpty(text))
            {
                return StyledText.Empty;
            }

            List<Color?> openedColors = new List<Color?>();
            List<OpenedLink> openedLinks = new List<OpenedLink>();
            string markedText = MarkRuns(text, element?.Data as ILinkHost, openedColors, openedLinks);

            string resolvedText = TokenHelper.Resolve(markedText, element, quoteValues: false).Replace("\r\n", "\n");

            if (openedColors.Count is 0 && openedLinks.Count is 0)
            {
                return new StyledText(resolvedText, Array.Empty<ColorRun>(), Array.Empty<LinkRun>());
            }

            return BuildRuns(resolvedText, openedColors, openedLinks);
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

        /// <summary>Swaps each tag for a marker, collecting what each opening tag asks for in the order they appear.</summary>
        private static string MarkRuns(string text, ILinkHost? linkHost, List<Color?> openedColors, List<OpenedLink> openedLinks)
        {
            if (text.Contains('[') is false)
            {
                return text;
            }

            int colorDepth = 0;
            int linkDepth = 0;
            int nextOccurrence = 0;
            bool hasStrayClose = false;

            string markedText = _markupPattern.Replace(text, match =>
            {
                if (IsOpening(match, COLOR_TAG))
                {
                    colorDepth++;
                    openedColors.Add(ParseColor(match.Groups["value"].Value, text, "color"));

                    return COLOR_OPEN_MARKER.ToString();
                }

                if (IsOpening(match, LINK_TAG))
                {
                    linkDepth++;
                    openedLinks.Add(OpenLink(match.Groups["value"].Value, text, linkHost, ref nextOccurrence));

                    return LINK_OPEN_MARKER.ToString();
                }

                bool isColorClose = string.Equals(match.Groups["closingTag"].Value, COLOR_TAG, StringComparison.OrdinalIgnoreCase);

                if ((isColorClose ? colorDepth : linkDepth) is 0)
                {
                    hasStrayClose = true;
                    return string.Empty;
                }

                if (isColorClose)
                {
                    colorDepth--;
                    return COLOR_CLOSE_MARKER.ToString();
                }

                linkDepth--;
                return LINK_CLOSE_MARKER.ToString();
            });

            if (hasStrayClose)
            {
                Parchment.monitor.LogOnce($"'{text}' has a closing tag with nothing open before it, so it was dropped.", LogLevel.Warn);
            }

            if (colorDepth > 0 || linkDepth > 0)
            {
                Parchment.monitor.LogOnce($"'{text}' has a tag that is never closed, so it runs to the end of the text.", LogLevel.Warn);
            }

            return markedText;
        }

        /// <summary>Reads a [link] tag against the links the element defines. One that names nothing it defines is kept as plain text, though its marker still goes in so its closing tag pairs up.</summary>
        private static OpenedLink OpenLink(string id, string source, ILinkHost? linkHost, ref int nextOccurrence)
        {
            if (linkHost is null)
            {
                Parchment.monitor.LogOnce($"'{source}' has [link={id}], but links only work on Title, Heading, Paragraph and PageNumber elements. The text is drawn without it.", LogLevel.Warn);
                return new OpenedLink(-1, null);
            }

            if (linkHost.Links is null || TryGetLink(linkHost.Links, id, out _, out LinkData? link) is false || link is null)
            {
                Parchment.monitor.LogOnce($"'{source}' has [link={id}], which isn't one of the element's \"Links\". The text is drawn without it.", LogLevel.Warn);
                return new OpenedLink(-1, null);
            }

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

        /// <summary>Takes the markers back out of the resolved text, recording the colors and links each one opened or closed against the plain text that is left.
        /// A closing marker closes the innermost tag of its own kind, so [color] and [link] tags that overlap rather than nest still pair up the way they were written.
        /// </summary>
        private static StyledText BuildRuns(string resolvedText, IReadOnlyList<Color?> openedColors, IReadOnlyList<OpenedLink> openedLinks)
        {
            StringBuilder plainText = new StringBuilder(resolvedText.Length);
            List<ColorRun> colorRuns = new List<ColorRun>();
            List<LinkRun> linkRuns = new List<LinkRun>();
            List<OpenTag> openTags = new List<OpenTag>();

            int nextColorIndex = 0;
            int nextLinkIndex = 0;

            Color? currentColor = null;
            int colorRunStart = 0;
            int currentOccurrence = -1;
            int linkRunStart = 0;

            foreach (char character in resolvedText)
            {
                switch (character)
                {
                    case COLOR_OPEN_MARKER:
                        Color? openedColor = nextColorIndex < openedColors.Count ? openedColors[nextColorIndex] : null;
                        nextColorIndex++;

                        openTags.Add(new OpenTag(false, openedColor, -1));
                        break;
                    case LINK_OPEN_MARKER:
                        OpenedLink openedLink = nextLinkIndex < openedLinks.Count ? openedLinks[nextLinkIndex] : new OpenedLink(-1, null);
                        nextLinkIndex++;

                        openTags.Add(new OpenTag(true, openedLink.Color, openedLink.Occurrence));
                        break;
                    case COLOR_CLOSE_MARKER:
                        CloseInnermost(isLink: false);
                        break;
                    case LINK_CLOSE_MARKER:
                        CloseInnermost(isLink: true);
                        break;
                    default:
                        plainText.Append(character);
                        continue;
                }

                ChangeColor(GetInnermostColor());
                ChangeOccurrence(GetInnermostOccurrence());
            }

            ChangeColor(null);
            ChangeOccurrence(-1);

            return new StyledText(plainText.ToString(), colorRuns, linkRuns);

            void CloseInnermost(bool isLink)
            {
                for (int index = openTags.Count - 1; index >= 0; index--)
                {
                    if (openTags[index].IsLink == isLink)
                    {
                        openTags.RemoveAt(index);
                        return;
                    }
                }
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
                    if (openTags[index].IsLink && openTags[index].Occurrence >= 0)
                    {
                        return openTags[index].Occurrence;
                    }
                }

                return -1;
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
