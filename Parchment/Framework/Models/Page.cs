using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
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
        /// <summary>A height no page will ever reach, used to stop <see cref="MeasureStack"/> clipping. Kept well short of <see cref="float.MaxValue"/> so the subtraction a container does for its children can't overflow.</summary>
        private const float UNBOUNDED_MEASURE_HEIGHT = 1000000f;

        public PageData Data { get; }

        /// <summary>This page's 0-based position within the book, which is what a <see cref="Enums.ElementType.PageNumber"/> element renders (as a 1-based number).</summary>
        public int Index { get; }

        /// <summary>This page's 0-based position within its chapter, used by a <see cref="Enums.ElementType.PageNumber"/> element scoped to <see cref="Enums.PageNumberScope.Chapter"/>.
        /// Assigned by <see cref="Book"/> once chapters are known, as a page can't work out its own chapter while the chapters are still being built.
        /// </summary>
        public int IndexInChapter { get; internal set; } = -1;

        public List<Element> Elements { get; }
        public List<Element> Background { get; }
        public List<Element> Foreground { get; }

        public ElementRenderContext? LastLayoutContext;

        /// <summary>Every element on this page carrying a frame action, gathered once at construction. Frame actions are dispatched every tick, so this is what keeps a page with none from walking its whole element tree sixty times a second.</summary>
        public List<Element> FrameActionElements { get; }

        public Page(PageData data, int index, ElementRegistry registry, FontResolver fontResolver)
        {
            Data = data;
            Index = index;
            Elements = ElementFactory.CreateList(Data.Elements, registry, fontResolver);
            Background = ElementFactory.CreateList(Data.Background, registry, fontResolver);
            Foreground = ElementFactory.CreateList(Data.Foreground, registry, fontResolver);

            FrameActionElements = new List<Element>();
            AnimationHelper.CollectFrameActionElements(Elements, FrameActionElements);
            AnimationHelper.CollectFrameActionElements(Background, FrameActionElements);
            AnimationHelper.CollectFrameActionElements(Foreground, FrameActionElements);
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

            hasChanged |= RefreshConditionsFor(element.Children);
            hasChanged |= RefreshConditionsFor(element.Background);
            hasChanged |= RefreshConditionsFor(element.Foreground);

            return hasChanged;
        }

        // Start of public static methods
        /// <summary>Places each element at its own <see cref="ElementData.Position"/> rather than stacking it, used for a page's Background and Foreground, a book's Underlay and Overlay and a container's own layers.
        /// <see cref="ElementData.Alignment"/> and <see cref="ElementData.VerticalAlignment"/> anchor the element within the container first and <see cref="ElementData.Position"/> is then an offset from that anchor.
        /// Left and Top anchor at zero, so a default-aligned element's position still reads as a plain coordinate. Unlike <see cref="StackElements"/> this ignores the element's margins, as Position is already the way to inset a placed element.
        /// </summary>
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

                int alignedX = (int)AlignmentHelper.GetAlignedX(availableWidth: context.AvailableWidth, contentWidth: elementSize.X, alignment: element.Data.Alignment);
                int alignedY = (int)AlignmentHelper.GetAlignedY(availableHeight: context.AvailableHeight, contentHeight: elementSize.Y, alignment: element.Data.VerticalAlignment);

                element.Bounds = new Rectangle(alignedX + element.Data.Position.X, alignedY + element.Data.Position.Y, (int)elementSize.X, (int)elementSize.Y);
            }
        }

        /// <summary>Measures how tall a stack of elements comes to, without the clipping <see cref="StackElements"/> applies once content runs past the bottom of the page.
        /// Use this to ask whether content fits, since the clipped height stops growing at the page edge and so can't answer that.
        /// </summary>
        /// <param name="elements">The elements that would be stacked.</param>
        /// <param name="availableWidth">The width the content wraps to, being the page's content width.</param>
        public static float MeasureStack(IReadOnlyList<Element> elements, float availableWidth)
        {
            return StackElements(elements, new ElementRenderContext(availableWidth, UNBOUNDED_MEASURE_HEIGHT));
        }

        // Keep this static so it can be called outside Page
        /// <summary>Stacks each element top to bottom, returning the total height the stack came to.
        /// <see cref="ElementData.VerticalAlignment"/> has no meaning here, as a stacked element's vertical position comes from the elements above it rather than from the space around it.
        /// </summary>
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

                WarnOnStackedVerticalAlignment(element);

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

        /// <summary>Reports a stacked element carrying a <see cref="ElementData.VerticalAlignment"/>, which is always an authoring mistake.
        /// Logged once per element type and ID, as this runs on every layout pass.
        /// </summary>
        private static void WarnOnStackedVerticalAlignment(Element element)
        {
            if (element.Data.VerticalAlignment is VerticalAlignmentType.Top)
            {
                return;
            }

            string elementLabel = string.IsNullOrWhiteSpace(element.Data.Id) ? $"A {element.Data.Type} element" : $"The {element.Data.Type} element \"{element.Data.Id}\"";

            Parchment.monitor.LogOnce($"{elementLabel} sets \"VerticalAlignment\" to {element.Data.VerticalAlignment} while stacked, where it is ignored. It only applies to placed elements, in a page's Background or Foreground or a book's Underlay or Overlay.", LogLevel.Error);
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

                // A container's own layers are anchored to its content area, so they are tested against the same rectangle its children are
                // Both are always interactiveOnly, whatever the outer list is, so decorative art inside a panel doesn't swallow the cursor
                Element? hitLayer = HitTest(element.Foreground, contentBounds, screenPosition, interactiveOnly: true);
                if (hitLayer is not null)
                {
                    return hitLayer;
                }

                Element? hitChild = HitTest(element.Children, contentBounds, screenPosition, interactiveOnly);
                if (hitChild is not null)
                {
                    return hitChild;
                }

                hitLayer = HitTest(element.Background, contentBounds, screenPosition, interactiveOnly: true);
                if (hitLayer is not null)
                {
                    return hitLayer;
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
