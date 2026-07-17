using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Rendering;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class ElementFactory
    {
        public static List<Element> CreateList(List<ElementData>? elementDataCollection, ElementRegistry registry, FontResolver fontResolver)
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

        public static Element? Create(ElementData data, ElementRegistry registry, FontResolver fontResolver)
        {
            if (registry.TryResolve(data.Type, out ElementRegistration registration) is false)
            {
                Parchment.monitor.Log($"No renderer registered for element type {data.Type}; skipping element!", LogLevel.Warn);
                return null;
            }

            // Get font
            IFont? font = null;
            if (data is ITextContent textContent)
            {
                font = fontResolver.Resolve(textContent.FontType);
            }

            // Get textures and item related details
            string? displayName = data.DisplayName;
            string? description = data.Description;

            IAssetName? textureAssetName = null;
            Texture2D? texture = null;
            Rectangle? sourceRectangle = null;

            if (data is ImageElementData imageData && string.IsNullOrWhiteSpace(imageData.ItemId) is false)
            {
                ParsedItemData? itemData = ItemRegistry.GetData(imageData.ItemId);

                if (itemData is null)
                {
                    Parchment.monitor.Log($"Unknown item ID '{imageData.ItemId}'; the element will not render.", LogLevel.Warn);
                }
                else
                {
                    texture = itemData.GetTexture();
                    sourceRectangle = itemData.GetSourceRect();

                    if (displayName is null)
                    {
                        displayName = itemData.DisplayName;
                    }
                    if (description is null)
                    {
                        description = itemData.Description;
                    }
                }
            }
            else
            {
                textureAssetName = ResolveTextureAssetName(data);
            }

            // Determine visiblity
            bool isVisible = true;
            if (string.IsNullOrEmpty(data.Condition) is false)
            {
                isVisible = false;
            }

            // Create element
            var element = new Element(data, registration.Renderer)
            {
                DisplayName = displayName,
                Description = description,
                IsVisible = isVisible,
                Font = font,
                TextColor = ResolveTextColor(data) ?? Game1.textColor,
                TintColor = ResolveTintColor(data) ?? Color.White,
                TextureAssetName = textureAssetName,
                SourceRectangle = sourceRectangle,
                Texture = texture,
                Children = CreateChildren(data, registry, fontResolver)
            };

            // Pull latest texture
            if (textureAssetName is not null)
            {
                RefreshTexture(element);
            }

            return element;
        }

        private static IReadOnlyList<Element> CreateChildren(ElementData data, ElementRegistry registry, FontResolver fontResolver)
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

        public static bool RefreshTextures(List<Element> elements, IReadOnlyCollection<IAssetName> invalidatedAssetNames)
        {
            bool wasAnyTextureRefreshed = false;
            foreach (Element element in elements)
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
                return true;
            }

            return false;
        }
    }
}
