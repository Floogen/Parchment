using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Rendering;
using Parchment.Framework.UI.Rendering.Elements;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Parchment.Framework.Models
{
    public class Page
    {
        public PageData Data { get; }

        public List<Element> Elements { get; }
        public List<Element> Background { get; }
        public List<Element> Foreground { get; }

        public ElementRenderContext? LastLayoutContext;

        public Page(PageData data, ElementRegistry registry, FontResolver fontResolver)
        {
            Data = data;
            Elements = ElementFactory.CreateList(Data.Elements, registry, fontResolver);
            Background = ElementFactory.CreateList(Data.Background, registry, fontResolver);
            Foreground = ElementFactory.CreateList(Data.Foreground, registry, fontResolver);
        }

        /// <summary>
        /// This should be called anytime the UI changes for scaling / width
        /// </summary>
        public void PerformLayout(ElementRenderContext context)
        {
            PositionElements(Background, context);
            PositionElements(Foreground, context);

            float contentHeight = StackElements(Elements, context);
            if (contentHeight > context.AvailableHeight)
            {
                Parchment.monitor.LogOnce($"Page content is {(int)contentHeight}px tall but the page is only {(int)context.AvailableHeight}px, content will overflow!", LogLevel.Trace);
            }

            LastLayoutContext = context;
        }

        public bool RefreshConditions()
        {
            bool hasAnyChanged = false;

            hasAnyChanged |= RefreshConditionsFor(Elements);
            hasAnyChanged |= RefreshConditionsFor(Background);
            hasAnyChanged |= RefreshConditionsFor(Foreground);

            if (hasAnyChanged)
            {
                LastLayoutContext = null;
            }

            return hasAnyChanged;
        }

        public static bool RefreshConditionsFor(IReadOnlyList<Element> elements)
        {
            bool hasAnyChanged = false;

            foreach (Element element in elements)
            {
                hasAnyChanged |= RefreshCondition(element);
            }

            return hasAnyChanged;
        }

        private static bool RefreshCondition(Element element)
        {
            bool hasChanged = false;

            if (string.IsNullOrEmpty(element.Data.Condition) is false)
            {
                bool isVisible = GameStateQuery.CheckConditions(element.Data.Condition);

                if (isVisible != element.IsVisible)
                {
                    element.IsVisible = isVisible;
                    hasChanged = true;
                }
            }

            // Frame conditions don't affect layout, since the element is sized by its source rectangle rather than the active frame, so this deliberately doesn't feed into hasChanged and trigger a relayout
            AnimationHelper.RefreshActiveFrames(element);

            foreach (Element child in element.Children)
            {
                hasChanged |= RefreshCondition(child);
            }

            return hasChanged;
        }

        // Start of public static methods
        public static void PositionElements(IReadOnlyList<Element> elements, ElementRenderContext context)
        {
            foreach (Element element in elements)
            {
                if (element.IsVisible is false)
                {
                    element.Bounds = Rectangle.Empty;
                    continue;
                }

                Vector2 elementSize = element.Renderer.Measure(element, context);

                if (elementSize.Y <= 0f)
                {
                    element.Bounds = Rectangle.Empty;
                    continue;
                }

                element.Bounds = new Rectangle(element.Data.Position.X, element.Data.Position.Y, (int)elementSize.X, (int)elementSize.Y);
            }
        }

        // Keep this static so it can be called outside Page
        public static float StackElements(IReadOnlyList<Element> elements, ElementRenderContext context)
        {
            float currentY = 0f;
            float pendingSpacing = 0f;
            bool hasPrecedingElement = false;

            foreach (Element element in elements)
            {
                // Check is element should be visible at all
                if (element.IsVisible is false)
                {
                    element.Bounds = Rectangle.Empty;
                    continue;
                }

                // Only stop rendering if it is past the AvailableHeight AND at least one element has been laid out
                float elementY = currentY + pendingSpacing;
                if (hasPrecedingElement && elementY >= context.AvailableHeight)
                {
                    element.Bounds = Rectangle.Empty;
                    continue;
                }

                float marginLeft = element.Data.MarginLeft * element.Data.Scale;
                float marginRight = element.Data.MarginRight * element.Data.Scale;
                float elementAvailableWidth = Math.Max(0f, context.AvailableWidth - marginLeft - marginRight);

                ElementRenderContext elementContext = context.WithSize(elementAvailableWidth, Math.Max(0f, context.AvailableHeight - elementY));
                Vector2 elementSize = element.Renderer.Measure(element, elementContext);

                // Skip elements with zero height
                if (elementSize.Y <= 0f)
                {
                    element.Bounds = Rectangle.Empty;
                    continue;
                }

                float elementX = marginLeft + AlignmentHelper.GetAlignedX(availableWidth: elementAvailableWidth, contentWidth: elementSize.X, alignment: element.Data.Alignment);
                element.Bounds = new Rectangle((int)elementX, (int)elementY, (int)elementSize.X, (int)elementSize.Y);

                currentY = elementY + elementSize.Y;
                pendingSpacing = element.Data.SpacingAfter * element.Data.Scale;
                hasPrecedingElement = true;
            }

            return currentY;
        }

        /// <param name="interactiveOnly">When true, elements that do nothing on hover or click are stepped over instead of claiming the cursor. Used for <see cref="PageData.Background"/> and <see cref="PageData.Foreground"/>,
        /// where a decorative element covering the page would otherwise block everything beneath it. Their children are still tested, so a plain panel can hold an element carrying a description.
        /// </param>
        public static Element? HitTest(IReadOnlyList<Element> elements, Rectangle containerBounds, Point screenPosition, bool interactiveOnly = false)
        {
            foreach (Element element in elements)
            {
                if (element.Bounds == Rectangle.Empty)
                {
                    continue;
                }

                Rectangle screenBounds = new Rectangle(element.Bounds.X + containerBounds.X, element.Bounds.Y + containerBounds.Y, element.Bounds.Width, element.Bounds.Height);
                if (screenBounds.Contains(screenPosition) is false)
                {
                    continue;
                }

                Rectangle contentBounds = element.Renderer.GetContentBounds(element, screenBounds);
                Element? hitChild = HitTest(element.Children, contentBounds, screenPosition, interactiveOnly);
                if (hitChild is not null)
                {
                    return hitChild;
                }

                if (interactiveOnly && element.IsInteractive is false)
                {
                    continue;
                }

                return element;
            }

            return null;
        }
    }
}
