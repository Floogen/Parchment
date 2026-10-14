using Microsoft.Xna.Framework;
using System;

namespace Parchment.Framework.UI.Layouts
{
    /// <summary>The layout of text shown through a window of fixed height, being every line it wrapped to and how many of them the window can hold from a given line.
    /// Scrolling moves through whole lines, so a line is either drawn in full or left out and nothing is ever cut through the middle.
    /// </summary>
    public class ScrollLayout
    {
        // Leaves room for the rounding a line height picks up, so lines that add up to exactly the window's height still count as fitting
        private const float FIT_TOLERANCE = 0.01f;

        /// <summary>Every line the text wrapped to, including the ones out of view.</summary>
        public WrappedText WrappedText { get; }

        /// <summary>How tall the window onto the text is, in screen pixels.</summary>
        public float ViewportHeight { get; }

        /// <summary>How wide the text is laid out, in screen pixels, which is the element's width less the room kept for the scroll bar.</summary>
        public float TextWidth { get; }

        /// <summary>The lowest line the window can start from, being the one that brings the last line into view at the bottom. Zero when every line fits.</summary>
        public int MaxScrollLine { get; }

        /// <summary>Whether the reader can move through the text, which takes the element asking to scroll and there being more text than fits.</summary>
        public bool CanScroll { get; }

        /// <summary>How wide the bar is drawn, in screen pixels.</summary>
        public int BarWidth { get; }

        /// <summary>How much of the element's right side reaches the bar for the cursor, in screen pixels. Wider than the bar itself, so a thin bar can still be taken hold of.</summary>
        public int HitWidth { get; }

        public Color BarColor { get; }
        public Color HoverColor { get; }
        public Color ActiveColor { get; }

        /// <summary>The color of the channel the thumb runs along (null when none is drawn).</summary>
        public Color? TrackColor { get; }

        /// <summary>Whether the bar fades out while the reader leaves the text alone.</summary>
        public bool AutoHide { get; }

        /// <summary>How long the bar stays up once left alone before it starts to fade, in milliseconds.</summary>
        public float HideAfter { get; }

        public ScrollLayout(WrappedText wrappedText, float viewportHeight, float textWidth, bool isScrollable, int barWidth, int hitWidth, Color barColor, Color hoverColor, Color activeColor, Color? trackColor, bool autoHide, float hideAfter)
        {
            AutoHide = autoHide;
            HideAfter = hideAfter;
            WrappedText = wrappedText;
            ViewportHeight = viewportHeight;
            TextWidth = textWidth;
            BarWidth = barWidth;
            HitWidth = hitWidth;
            BarColor = barColor;
            HoverColor = hoverColor;
            ActiveColor = activeColor;
            TrackColor = trackColor;

            MaxScrollLine = isScrollable ? GetFirstLineShowing(wrappedText.Lines.Count - 1) : 0;
            CanScroll = isScrollable && MaxScrollLine > 0;
        }

        /// <summary>Holds a line to the ones the window can start from.</summary>
        public int ClampLine(int line)
        {
            return Math.Clamp(line, 0, MaxScrollLine);
        }

        /// <summary>How many lines the window holds starting from the given one. Always at least one while there is a line there, so a single line taller than the window still shows rather than leaving the text blank.</summary>
        public int GetVisibleLineCount(int firstLine)
        {
            float height = 0f;
            int count = 0;

            for (int index = Math.Max(0, firstLine); index < WrappedText.Lines.Count; index++)
            {
                float lineHeight = WrappedText.Lines[index].Size.Y;

                if (count > 0 && height + lineHeight > ViewportHeight + FIT_TOLERANCE)
                {
                    break;
                }

                height += lineHeight;
                count++;
            }

            return count;
        }

        /// <summary>The highest line the window can start from and still reach down to the given one, which is where to scroll to bring that line in at the bottom.</summary>
        public int GetFirstLineShowing(int lastLine)
        {
            if (WrappedText.Lines.Count is 0 || lastLine < 0)
            {
                return 0;
            }

            lastLine = Math.Min(lastLine, WrappedText.Lines.Count - 1);

            float height = 0f;
            int firstLine = lastLine;

            for (int index = lastLine; index >= 0; index--)
            {
                float lineHeight = WrappedText.Lines[index].Size.Y;

                if (index < lastLine && height + lineHeight > ViewportHeight + FIT_TOLERANCE)
                {
                    break;
                }

                height += lineHeight;
                firstLine = index;
            }

            return firstLine;
        }

        /// <summary>How much of the text the window shows, from just above 0 to 1, which is how tall the thumb is against the bar.</summary>
        public float GetVisibleFraction()
        {
            if (WrappedText.Size.Y <= 0f)
            {
                return 1f;
            }

            return Math.Clamp(ViewportHeight / WrappedText.Size.Y, 0f, 1f);
        }
    }
}
