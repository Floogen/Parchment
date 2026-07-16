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
        public ElementRenderContext? LastLayoutContext;

        public Page(PageData data, ElementRegistry registry, FontResolver fontResolver)
        {
            Data = data;
            Elements = CreateElements(registry, fontResolver);
        }

        private List<Element> CreateElements(ElementRegistry registry, FontResolver fontResolver)
        {
            var elements = new List<Element>();
            foreach (var elementData in Data.Elements ?? Enumerable.Empty<ElementData>())
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

        // Keep this static so it can be called outside Page
        public static float StackElements(IReadOnlyList<Element> elements, ElementRenderContext context)
        {
            float currentY = 0f;

            for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++)
            {
                Element element = elements[elementIndex];

                // Only stop rendering if it is past the AvailableHeight AND it already has rendered at least one element
                if (elementIndex > 0 && currentY >= context.AvailableHeight)
                {
                    element.Bounds = Rectangle.Empty;
                    continue;
                }

                Vector2 elementSize = element.Renderer.Measure(element, context);
                float elementX = AlignmentHelper.GetAlignedX(availableWidth: context.AvailableWidth, contentWidth: elementSize.X, alignment: element.Data.Alignment);

                element.Bounds = new Rectangle((int)elementX, (int)currentY, (int)elementSize.X, (int)elementSize.Y);
                currentY += elementSize.Y;

                if (elementIndex < elements.Count - 1)
                {
                    currentY += element.Data.SpacingAfter * element.Data.Scale;
                }
            }

            return currentY;
        }



        /// <summary>
        /// This should be called anytime the UI changes for scaling / width
        /// </summary>
        public void PerformLayout(ElementRenderContext context)
        {
            float contentHeight = StackElements(Elements, context);

            if (contentHeight > context.AvailableHeight)
            {
                Parchment.monitor.LogOnce($"Page content is {(int)contentHeight}px tall but the page is only {(int)context.AvailableHeight}px, content will overflow!", LogLevel.Warn);
            }

            LastLayoutContext = context;
        }
    }
}
