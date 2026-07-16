using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Rendering;
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

            var element = new Element(data, registration.Renderer) { Font = font, TextureAssetName = ResolveTextureAssetName(data) };
            RefreshTexture(element);

            return element;
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
            float currentY = 0f;

            foreach (Element element in this.Elements)
            {
                Vector2 elementSize = element.Renderer.Measure(element, context);
                element.Bounds = new Rectangle(0, (int)currentY, (int)elementSize.X, (int)elementSize.Y);
                currentY += elementSize.Y + element.Data.SpacingAfter * element.Data.Scale;
            }

            LastLayoutContext = context;
        }
    }
}
