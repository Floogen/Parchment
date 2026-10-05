using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.UI.Layouts;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Reads the [color=...] markup an author can write into an element's text to color part of it.
    /// The markup is read out of the authored text before any token resolves. It stands in the text as a pair of marker characters while the tokens do.
    /// That keeps the game from ever seeing it as one of its [Token] forms. It also means a value a token brings in (out of something the player typed, say) can never open a run of its own.
    /// </summary>
    public static class TextMarkupHelper
    {
        public const char RUN_OPEN_MARKER = '\u0002';
        public const char RUN_CLOSE_MARKER = '\u0003';

        private static readonly Regex _markupPattern = new Regex(@"\[color=(?<value>[^\[\]]*)\]|\[/color\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>The text with its markup taken out, for somewhere that draws in a single color such as a tooltip.</summary>
        public static string RemoveMarkup(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Contains('[') is false)
            {
                return text;
            }

            return _markupPattern.Replace(text, string.Empty);
        }

        /// <summary>The value with any run markers taken out, so nothing a token brings in can open or close a run.</summary>
        public static string RemoveMarkers(string value)
        {
            if (value.IndexOf(RUN_OPEN_MARKER) < 0 && value.IndexOf(RUN_CLOSE_MARKER) < 0)
            {
                return value;
            }

            return value.Replace(RUN_OPEN_MARKER.ToString(), string.Empty).Replace(RUN_CLOSE_MARKER.ToString(), string.Empty);
        }

        /// <summary>Resolves an element's text into what to draw and where each color applies.
        /// Runs nest, so text after an inner [/color] goes back to the color around it. An unclosed run carries on to the end, a stray [/color] is dropped and a color that won't parse keeps the color around it.
        /// </summary>
        public static StyledText Resolve(string? text, Element? element)
        {
            if (string.IsNullOrEmpty(text))
            {
                return StyledText.Empty;
            }

            List<Color?> openedColors = new List<Color?>();
            string markedText = MarkRuns(text, openedColors);

            string resolvedText = TokenHelper.Resolve(markedText, element, quoteValues: false).Replace("\r\n", "\n");

            if (openedColors.Count is 0)
            {
                return new StyledText(resolvedText, Array.Empty<TextRun>());
            }

            return BuildRuns(resolvedText, openedColors);
        }

        /// <summary>Swaps each tag for a marker, collecting the color each opening tag asks for in the order they appear.</summary>
        private static string MarkRuns(string text, List<Color?> openedColors)
        {
            if (text.Contains('[') is false)
            {
                return text;
            }

            int depth = 0;
            bool hasStrayClose = false;

            string markedText = _markupPattern.Replace(text, match =>
            {
                if (match.Groups["value"].Success)
                {
                    depth++;
                    openedColors.Add(ParseColor(match.Groups["value"].Value, text));

                    return RUN_OPEN_MARKER.ToString();
                }

                if (depth is 0)
                {
                    hasStrayClose = true;
                    return string.Empty;
                }

                depth--;
                return RUN_CLOSE_MARKER.ToString();
            });

            if (hasStrayClose)
            {
                Parchment.monitor.LogOnce($"'{text}' has a [/color] with no [color] open before it, so it was dropped.", LogLevel.Warn);
            }

            if (depth > 0)
            {
                Parchment.monitor.LogOnce($"'{text}' has a [color] that is never closed, so it runs to the end of the text.", LogLevel.Warn);
            }

            return markedText;
        }

        /// <summary>The color an opening tag asks for. Null when it won't parse, which keeps the color around it.</summary>
        private static Color? ParseColor(string value, string source)
        {
            if (ColorParser.TryParse(value, out Color color))
            {
                return color;
            }

            Parchment.monitor.LogOnce($"'{source}' has [color={value}], which isn't a color Parchment can read, so the text keeps the color around it.", LogLevel.Warn);
            return null;
        }

        /// <summary>Takes the markers back out of the resolved text, recording the run each one opened or closed against the plain text that is left.</summary>
        private static StyledText BuildRuns(string resolvedText, IReadOnlyList<Color?> openedColors)
        {
            StringBuilder plainText = new StringBuilder(resolvedText.Length);
            List<TextRun> runs = new List<TextRun>();
            Stack<Color?> enclosingColors = new Stack<Color?>();

            int nextColorIndex = 0;
            Color? currentColor = null;
            int runStart = 0;

            foreach (char character in resolvedText)
            {
                if (character == RUN_OPEN_MARKER)
                {
                    Color? openedColor = nextColorIndex < openedColors.Count ? openedColors[nextColorIndex] : null;
                    nextColorIndex++;

                    enclosingColors.Push(currentColor);
                    ChangeColor(openedColor ?? currentColor);

                    continue;
                }

                if (character == RUN_CLOSE_MARKER)
                {
                    ChangeColor(enclosingColors.Count is 0 ? null : enclosingColors.Pop());
                    continue;
                }

                plainText.Append(character);
            }

            ChangeColor(null);

            return new StyledText(plainText.ToString(), runs);

            void ChangeColor(Color? nextColor)
            {
                if (nextColor == currentColor)
                {
                    return;
                }

                if (currentColor is Color runColor && plainText.Length > runStart)
                {
                    AddRun(runStart, plainText.Length - runStart, runColor);
                }

                currentColor = nextColor;
                runStart = plainText.Length;
            }

            void AddRun(int start, int length, Color color)
            {
                // Merged into the run before it when the two touch and match, as happens when one run closes and an identical one opens straight after
                if (runs.Count is not 0 && runs[^1].End == start && runs[^1].Color == color)
                {
                    TextRun previousRun = runs[^1];
                    runs[^1] = new TextRun(previousRun.Start, previousRun.Length + length, color);

                    return;
                }

                runs.Add(new TextRun(start, length, color));
            }
        }
    }
}
