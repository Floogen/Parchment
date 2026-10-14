using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class ParagraphElementRenderer : TextElementRenderer<ParagraphElementData>
    {
        // How strongly the scroll bar's thumb takes the text color when no color is given for it, at rest and under the cursor
        private const float DEFAULT_BAR_OPACITY = 0.35f;
        private const float DEFAULT_BAR_HOVER_OPACITY = 0.6f;

        // The narrowest strip down the right side that reaches the scroll bar for the cursor, in screen pixels, so a thin bar can still be taken hold of
        private const int MIN_BAR_HIT_WIDTH = 16;

        protected override string GetText(ParagraphElementData data, ElementRenderContext context)
        {
            return data.Text ?? string.Empty;
        }

        protected override float? GetExplicitWidth(ParagraphElementData data, ElementRenderContext context)
        {
            if (data.Width is not int width)
            {
                return null;
            }

            return Math.Min(width * data.Scale, context.AvailableWidth);
        }

        /// <summary>Measures the paragraph as every other text element does, unless it has a <see cref="ParagraphElementData.Height"/>, in which case its text is shown through a window that tall.
        /// A scrolling paragraph whose text runs past the window is wrapped a second time, short of the room the scroll bar takes, so the bar only takes room away when there is something to scroll.
        /// </summary>
        protected override Vector2 Measure(ParagraphElementData data, Element element, ElementRenderContext context)
        {
            if (data.Height is not int height || element.Font is null)
            {
                element.ScrollLayout = null;

                return base.Measure(data, element, context);
            }

            float? explicitWidth = GetExplicitWidth(data, context);
            float availableWidth = explicitWidth ?? context.AvailableWidth;
            float viewportHeight = Math.Min(height * data.Scale, context.AvailableHeight);

            string text = GetText(data, context);
            WrappedText wrappedText = TextWrapper.WrapElementText(text, element, element.Font, availableWidth, data.Scale);

            ScrollBarData scrollBar = data.ScrollBar ?? new ScrollBarData();
            int barWidth = Math.Max(1, (int)Math.Round(scrollBar.Width * data.Scale));
            int gutterWidth = barWidth + (int)Math.Round(scrollBar.Gap * data.Scale);

            bool isScrollable = data.Overflow is OverflowMode.Scroll && wrappedText.Size.Y > viewportHeight && availableWidth > gutterWidth;

            if (isScrollable)
            {
                wrappedText = TextWrapper.WrapElementText(text, element, element.Font, availableWidth - gutterWidth, data.Scale);
            }

            element.LayoutState = wrappedText;

            // A scrolling paragraph takes the whole width it was given, so its bar sits at the edge of the column rather than just past its longest line
            float measuredWidth = isScrollable ? availableWidth : explicitWidth ?? wrappedText.Size.X;
            float textWidth = isScrollable ? availableWidth - gutterWidth : measuredWidth;
            int hitWidth = Math.Min((int)measuredWidth, Math.Max(gutterWidth, MIN_BAR_HIT_WIDTH));

            Color barColor = ResolveBarColor(scrollBar.Color, "Color", element.TextColor * DEFAULT_BAR_OPACITY);
            Color hoverColor = ResolveBarColor(scrollBar.HoverColor, "HoverColor", string.IsNullOrWhiteSpace(scrollBar.Color) ? element.TextColor * DEFAULT_BAR_HOVER_OPACITY : barColor);
            Color activeColor = ResolveBarColor(scrollBar.ActiveColor, "ActiveColor", hoverColor);
            Color? trackColor = string.IsNullOrWhiteSpace(scrollBar.TrackColor) ? null : ResolveBarColor(scrollBar.TrackColor, "TrackColor", Color.Transparent);

            ScrollLayout scrollLayout = new ScrollLayout(wrappedText, viewportHeight, textWidth, isScrollable, barWidth, hitWidth, barColor, hoverColor, activeColor, trackColor);
            element.ScrollLayout = scrollLayout;

            // Placed here rather than at draw, the same as any other text element. ScrollHelper places them again whenever the window moves
            ScrollHelper.ArrangeLinks(element, scrollLayout);

            return new Vector2(measuredWidth, viewportHeight);
        }

        private static Color ResolveBarColor(string? color, string fieldName, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return defaultColor;
            }

            if (ColorParser.TryParse(color, out Color parsedColor) is false)
            {
                Parchment.monitor.LogOnce($"A Paragraph has an unparsable \"ScrollBar.{fieldName}\" '{color}'; the default will be used.", LogLevel.Warn);
                return defaultColor;
            }

            return parsedColor;
        }

        protected override void Draw(SpriteBatch spriteBatch, ParagraphElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (this.TryGetWrappedText(element, out WrappedText wrappedText) is false || element.Font is null)
            {
                return;
            }

            if (element.ScrollLayout is not ScrollLayout scrollLayout)
            {
                StringHelper.DrawLines(spriteBatch, element, wrappedText, bounds, data.Alignment, element.TextColor, data.Scale);
                return;
            }

            // Only the lines in the window are drawn, each in full, so nothing needs cutting off at the edges
            Rectangle textBounds = new Rectangle(bounds.X, bounds.Y, (int)scrollLayout.TextWidth, bounds.Height);
            int firstLine = ScrollHelper.GetFirstLine(element, scrollLayout);

            StringHelper.DrawLines(spriteBatch, element, scrollLayout.WrappedText, textBounds, data.Alignment, element.TextColor, data.Scale, firstLine, scrollLayout.GetVisibleLineCount(firstLine));
            ScrollHelper.DrawScrollBar(spriteBatch, element, scrollLayout, bounds);
        }
    }
}
