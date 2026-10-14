using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Layouts;
using StardewValley;
using System;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Moves a scrolling element through its text and works out where its scroll bar sits. Scrolling moves by whole lines, so what is in view is always a run of complete lines.</summary>
    public static class ScrollHelper
    {
        // The shortest the thumb is drawn, in screen pixels, so a very long text still leaves something to take hold of
        private const int MIN_THUMB_HEIGHT = 16;

        // How long a bar that hides itself takes to fade out once its wait is over, in milliseconds
        private const float SCROLL_BAR_FADE_DURATION = 300f;

        /// <summary>Where a scroll bar sits on screen, being the channel the thumb runs along, the thumb itself and the strip of the element that reaches the bar for the cursor.</summary>
        public readonly record struct ScrollBarGeometry(Rectangle Track, Rectangle Thumb, Rectangle HitArea);

        /// <summary>Whether the element is showing text the reader can move through, which takes it asking to scroll and there being more text than fits.</summary>
        public static bool CanScroll(Element element)
        {
            return element.ScrollLayout is { CanScroll: true };
        }

        /// <summary>The line the element's window starts from, being its <see cref="Element.ScrollLine"/> held to the lines it can start from.
        /// Held here rather than written back, as an element can be measured at a trial width before its real one and the reader's place shouldn't be lost to the trial.
        /// </summary>
        public static int GetFirstLine(Element element, ScrollLayout scrollLayout)
        {
            return scrollLayout.ClampLine(element.ScrollLine);
        }

        /// <summary>Moves the element's window by a number of lines, down for a positive count and up for a negative one. Reports whether it moved, which it doesn't once it's already at that end.</summary>
        public static bool TryScrollBy(Element element, int lineCount)
        {
            if (element.ScrollLayout is not ScrollLayout scrollLayout)
            {
                return false;
            }

            return TryScrollTo(element, GetFirstLine(element, scrollLayout) + lineCount);
        }

        /// <summary>Starts the element's window at the given line, held to the lines it can start from. Reports whether it moved.</summary>
        public static bool TryScrollTo(Element element, int line)
        {
            if (element.ScrollLayout is not ScrollLayout scrollLayout || scrollLayout.CanScroll is false)
            {
                return false;
            }

            int clampedLine = scrollLayout.ClampLine(line);

            if (clampedLine == GetFirstLine(element, scrollLayout))
            {
                return false;
            }

            element.ScrollLine = clampedLine;

            // Any scroll brings a bar that hides itself back up, whichever way it was asked for
            ShowScrollBar(element);

            // The links follow the text they sit on, so the ones scrolled away stop answering to the cursor and the ones scrolled in start to
            ArrangeLinks(element, scrollLayout);

            return true;
        }

        /// <summary>How many lines a page of the element's text is, being one fewer than the window holds so the line at the edge stays in view as a place to pick up from.</summary>
        public static int GetPageLineCount(Element element)
        {
            if (element.ScrollLayout is not ScrollLayout scrollLayout)
            {
                return 1;
            }

            return Math.Max(1, scrollLayout.GetVisibleLineCount(GetFirstLine(element, scrollLayout)) - 1);
        }

        /// <summary>Places the element's links over the lines currently in view, leaving the ones out of view with nothing for the cursor or a controller to reach.</summary>
        public static void ArrangeLinks(Element element, ScrollLayout scrollLayout)
        {
            int firstLine = GetFirstLine(element, scrollLayout);

            LinkLayoutHelper.ArrangeLinks(element, scrollLayout.WrappedText, scrollLayout.TextWidth, element.Data.Alignment, firstLine, scrollLayout.GetVisibleLineCount(firstLine));
        }

        /// <summary>Keeps a typewriter's newest line in view by scrolling down to it as it types, until the reader scrolls the element themselves.
        /// Reports whether the window moved, which leaves whatever the cursor was over possibly somewhere else.
        /// </summary>
        public static bool FollowTyping(Element element, double time)
        {
            if (element.HasScrolledByHand || element.ScrollLayout is not ScrollLayout scrollLayout || scrollLayout.CanScroll is false || element.TypewriterEffects.Count is 0)
            {
                return false;
            }

            if (TypewriterHelper.HasFinishedTyping(element))
            {
                return false;
            }

            int typingLine = GetTypingLine(element, scrollLayout, time);

            if (typingLine < 0)
            {
                return false;
            }

            int firstLine = GetFirstLine(element, scrollLayout);
            int lastVisibleLine = firstLine + scrollLayout.GetVisibleLineCount(firstLine) - 1;

            if (typingLine <= lastVisibleLine && typingLine >= firstLine)
            {
                return false;
            }

            return TryScrollTo(element, scrollLayout.GetFirstLineShowing(typingLine));
        }

        /// <summary>The lowest line a typewriter has started revealing (-1 when none has revealed anything yet). A line counts as soon as its first typed character appears.</summary>
        private static int GetTypingLine(Element element, ScrollLayout scrollLayout, double time)
        {
            for (int lineIndex = scrollLayout.WrappedText.Lines.Count - 1; lineIndex >= 0; lineIndex--)
            {
                WrappedLine line = scrollLayout.WrappedText.Lines[lineIndex];

                if (line.Segments is null)
                {
                    continue;
                }

                foreach (TextSegment segment in line.Segments)
                {
                    if (segment.Effects is null || segment.Text.Length is 0 || TextEffectHelper.HasEffect(segment.Effects, TextEffectType.Typewriter) is false)
                    {
                        continue;
                    }

                    if (TypewriterHelper.GetRevealAlpha(element, segment.Effects, segment.SourceStart, time) > 0f)
                    {
                        return lineIndex;
                    }
                }
            }

            return -1;
        }

        /// <summary>Where the element's scroll bar sits, against the element's bounds on screen. The bar runs the element's full height down its right edge.</summary>
        public static ScrollBarGeometry GetScrollBarGeometry(Element element, ScrollLayout scrollLayout, Rectangle bounds)
        {
            Rectangle track = new Rectangle(bounds.Right - scrollLayout.BarWidth, bounds.Y, scrollLayout.BarWidth, bounds.Height);

            int thumbHeight = Math.Min(track.Height, Math.Max(MIN_THUMB_HEIGHT, (int)Math.Round(track.Height * scrollLayout.GetVisibleFraction())));
            int thumbTravel = track.Height - thumbHeight;
            float scrollProgress = scrollLayout.MaxScrollLine > 0 ? GetFirstLine(element, scrollLayout) / (float)scrollLayout.MaxScrollLine : 0f;

            Rectangle thumb = new Rectangle(track.X, track.Y + (int)Math.Round(thumbTravel * scrollProgress), track.Width, thumbHeight);
            Rectangle hitArea = new Rectangle(bounds.Right - scrollLayout.HitWidth, bounds.Y, scrollLayout.HitWidth, bounds.Height);

            return new ScrollBarGeometry(track, thumb, hitArea);
        }

        /// <summary>The line the window starts from with the thumb's top at the given height on screen, being whichever line puts the thumb nearest there.</summary>
        public static int GetLineAtThumbTop(Element element, ScrollLayout scrollLayout, Rectangle bounds, float thumbTop)
        {
            ScrollBarGeometry geometry = GetScrollBarGeometry(element, scrollLayout, bounds);
            int thumbTravel = geometry.Track.Height - geometry.Thumb.Height;

            if (thumbTravel <= 0)
            {
                return 0;
            }

            float scrollProgress = Math.Clamp((thumbTop - geometry.Track.Y) / thumbTravel, 0f, 1f);

            return (int)Math.Round(scrollProgress * scrollLayout.MaxScrollLine);
        }

        /// <summary>Brings the element's scroll bar back into view, restarting the wait before a bar that hides itself fades.</summary>
        public static void ShowScrollBar(Element element)
        {
            element.ScrollBarShownAt = AnimationHelper.GetAnimationTime();
        }

        /// <summary>How strongly the element's scroll bar is drawn, being fully for a bar that doesn't hide itself.
        /// One that does stays fully drawn while the cursor is over the text or dragging the thumb, then holds for its wait before fading out.
        /// </summary>
        public static float GetScrollBarAlpha(Element element, ScrollLayout scrollLayout)
        {
            if (scrollLayout.AutoHide is false || element.IsScrollAreaHovered || element.IsScrollBarDragging)
            {
                return 1f;
            }

            double fadeElapsed = AnimationHelper.GetAnimationTime() - element.ScrollBarShownAt - scrollLayout.HideAfter;

            if (fadeElapsed <= 0d)
            {
                return 1f;
            }

            return Math.Clamp(1f - (float)(fadeElapsed / SCROLL_BAR_FADE_DURATION), 0f, 1f);
        }

        /// <summary>Draws the element's scroll bar, the thumb taking its hover or active color while the cursor is on the bar or dragging it. Nothing is drawn while all of the text fits.</summary>
        public static void DrawScrollBar(SpriteBatch spriteBatch, Element element, ScrollLayout scrollLayout, Rectangle bounds)
        {
            if (scrollLayout.CanScroll is false)
            {
                return;
            }

            // A bar that hides itself shows for its wait the first time it is drawn, so the reader sees there is more to the text before it fades
            if (element.ScrollBarShownAt == double.MinValue)
            {
                ShowScrollBar(element);
            }

            float barAlpha = GetScrollBarAlpha(element, scrollLayout) * element.DrawAlpha;

            if (barAlpha <= 0f)
            {
                return;
            }

            ScrollBarGeometry geometry = GetScrollBarGeometry(element, scrollLayout, bounds);

            if (scrollLayout.TrackColor is Color trackColor)
            {
                spriteBatch.Draw(Game1.staminaRect, geometry.Track, trackColor * barAlpha);
            }

            Color thumbColor = scrollLayout.BarColor;

            if (element.IsScrollBarDragging)
            {
                thumbColor = scrollLayout.ActiveColor;
            }
            else if (element.IsScrollBarHovered)
            {
                thumbColor = scrollLayout.HoverColor;
            }

            spriteBatch.Draw(Game1.staminaRect, geometry.Thumb, thumbColor * barAlpha);
        }
    }
}
