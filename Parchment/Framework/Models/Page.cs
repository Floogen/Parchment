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

        public ElementRenderContext? LastLayoutContext;

        public Page(PageData data, ElementRegistry registry, FontResolver fontResolver)
        {
            Data = data;
            Elements = CreateElements(Data.Elements, registry, fontResolver);
            Background = CreateElements(Data.Background, registry, fontResolver);
        }

        private List<Element> CreateElements(List<ElementData>? elementDataCollection, ElementRegistry registry, FontResolver fontResolver)
        {
            var elements = new List<Element>();
            if (elementDataCollection is null)
            {
                return elements;
            }

            foreach (var elementData in elementDataCollection)
            {
                var element = Create(elementData, registry, fontResolver);
                if (element is not null)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }

        private Element? Create(ElementData data, ElementRegistry registry, FontResolver fontResolver)
        {
            if (registry.TryResolve(data.Type, out ElementRegistration registration) is false)
            {
                Parchment.monitor.Log($"No renderer registered for element type {data.Type}; skipping element!", LogLevel.Warn);
                return null;
            }

            IFont? font = null;
            if (data is ITextContent textContent)
            {
                font = fontResolver.Resolve(textContent.FontType);
            }

            var element = new Element(data, registration.Renderer)
            { 
                Font = font,
                TextColor = ResolveTextColor(data) ?? Game1.textColor,
                TintColor = ResolveTintColor(data) ?? Color.White,
                TextureAssetName = ResolveTextureAssetName(data),
                Children = CreateChildren(data, registry, fontResolver)
            };

            RefreshTexture(element);

            return element;
        }

        private IReadOnlyList<Element> CreateChildren(ElementData data, ElementRegistry registry, FontResolver fontResolver)
        {
            if (data is not IContainer container || container.Children is null)
            {
                return Array.Empty<Element>();
            }

            var children = new List<Element>();
            foreach (ElementData childData in container.Children)
            {
                var child = Create(childData, registry, fontResolver);
                if (child is not null)
                {
                    children.Add(child);
                }
            }

            return children;
        }

        private static Color? ResolveTintColor(ElementData data)
        {
            if (data is not ISprite sprite || string.IsNullOrWhiteSpace(sprite.TintColor))
            {
                return null;
            }

            if (ColorParser.TryParse(sprite.TintColor, out Color parsedColor) is false)
            {
                Parchment.monitor.Log($"Element has an unparsable tint color '{sprite.TintColor}'; the sprite will not be tinted.", LogLevel.Warn);
                return null;
            }

            return parsedColor;
        }

        private static Color? ResolveTextColor(ElementData data)
        {
            if (data is not ITextContent textContent || string.IsNullOrWhiteSpace(textContent.TextColor))
            {
                return null;
            }

            if (ColorParser.TryParse(textContent.TextColor, out Color parsedColor) is false)
            {
                Parchment.monitor.Log($"Element has an unparsable color '{textContent.TextColor}'; using the default.", LogLevel.Warn);
                return null;
            }

            return parsedColor;
        }

        private static IAssetName? ResolveTextureAssetName(ElementData data)
        {
            if (data is not ISprite sprite || string.IsNullOrWhiteSpace(sprite.TexturePath))
            {
                return null;
            }

            try
            {
                return Parchment.modHelper.GameContent.ParseAssetName(sprite.TexturePath);
            }
            catch (Exception exception)
            {
                Parchment.monitor.Log($"Element has an unparsable texture path '{sprite.TexturePath}': {exception.Message}", LogLevel.Warn);
                return null;
            }
        }

        private static void RefreshTexture(Element element)
        {
            if (element.TextureAssetName is null)
            {
                return;
            }

            try
            {
                element.Texture = Parchment.modHelper.GameContent.Load<Texture2D>(element.TextureAssetName.Name);
            }
            catch (Exception exception)
            {
                element.Texture = null;
                Parchment.monitor.Log($"Failed to load texture '{element.TextureAssetName.Name}': {exception.Message}", LogLevel.Warn);
            }

            element.LayoutState = null;
        }

        public void RefreshTextures(IReadOnlyCollection<IAssetName> invalidatedAssetNames)
        {
            bool wasAnyTextureRefreshed = false;
            foreach (Element element in Elements)
            {
                if (element.TextureAssetName is null)
                {
                    continue;
                }

                if (invalidatedAssetNames.Any(assetName => assetName.IsEquivalentTo(element.TextureAssetName)) is false)
                {
                    continue;
                }

                RefreshTexture(element);
                wasAnyTextureRefreshed = true;
            }

            if (wasAnyTextureRefreshed)
            {
                LastLayoutContext = null;
            }
        }

        /// <summary>
        /// This should be called anytime the UI changes for scaling / width
        /// </summary>
        public void PerformLayout(ElementRenderContext context)
        {
            PositionElements(Background, context);

            float contentHeight = StackElements(Elements, context);
            if (contentHeight > context.AvailableHeight)
            {
                Parchment.monitor.LogOnce($"Page content is {(int)contentHeight}px tall but the page is only {(int)context.AvailableHeight}px, content will overflow!", LogLevel.Warn);
            }

            LastLayoutContext = context;
        }

        // Start of public static methods
        public static void PositionElements(IReadOnlyList<Element> elements, ElementRenderContext context)
        {
            foreach (Element element in elements)
            {
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
                float elementY = currentY + pendingSpacing;

                // Only stop rendering if it is past the AvailableHeight AND at least one element has been laid out
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

        public static Element? HitTest(IReadOnlyList<Element> elements, Rectangle containerBounds, Point screenPosition)
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
                Element? hitChild = HitTest(element.Children, contentBounds, screenPosition);

                return hitChild ?? element;
            }

            return null;
        }
    }
}
