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
                Results = data is GridElementData sourceGrid && sourceGrid.Source is not null ? new ResultSet(sourceGrid.Source) : null,
                Font = font,
                TextColor = ResolveTextColor(data) ?? Game1.textColor,
                TintColor = ResolveTintColor(data) ?? Color.White,
                TextureAssetName = textureAssetName,
                SourceRectangle = sourceRectangle,
                Texture = texture,
                Children = CreateChildren(data, registry, fontResolver),
                Background = CreateLayer(data is ILayeredContainer backgroundContainer ? backgroundContainer.Background : null, registry, fontResolver),
                Foreground = CreateLayer(data is ILayeredContainer foregroundContainer ? foregroundContainer.Foreground : null, registry, fontResolver)
            };

            WarnOnUnreachableContent(data);

            // Prep the active frames, so a conditional animation is correct on the first draw rather than after the first condition refresh
            AnimationHelper.RefreshActiveFrames(element);

            // Pull latest texture
            if (textureAssetName is not null)
            {
                RefreshTexture(element);
            }

            return element;
        }

        /// <summary>Reports a tooltip or hover art on an element the cursor passes through, which is always an authoring mistake.
        /// Logged once per element type and ID, as a Grid builds one element per cell from the same template.
        /// </summary>
        private static void WarnOnUnreachableContent(ElementData data)
        {
            if (data.IgnoreCursor is false)
            {
                return;
            }

            var unreachableFields = new List<string>();

            if (string.IsNullOrEmpty(data.DisplayName) is false) { unreachableFields.Add("DisplayName"); }
            if (string.IsNullOrEmpty(data.Description) is false) { unreachableFields.Add("Description"); }
            if (data is ISprite sprite && sprite.HoverTextureSourceRectangle is not null) { unreachableFields.Add("HoverTextureSourceRectangle"); }
            if (data is ImageElementData hoverImageData && hoverImageData.HoverFrames is not null && hoverImageData.HoverFrames.Count is not 0) { unreachableFields.Add("HoverFrames"); }

            if (unreachableFields.Count is 0)
            {
                return;
            }

            string elementLabel = string.IsNullOrWhiteSpace(data.Id) ? $"A {data.Type} element" : $"The {data.Type} element \"{data.Id}\"";

            Parchment.monitor.LogOnce($"{elementLabel} sets \"IgnoreCursor\" alongside {string.Join(", ", unreachableFields)}, which the cursor never reaches.", LogLevel.Warn);
        }

        private static IReadOnlyList<Element> CreateLayer(List<ElementData>? layerData, ElementRegistry registry, FontResolver fontResolver)
        {
            if (layerData is null || layerData.Count is 0)
            {
                return Array.Empty<Element>();
            }

            return CreateList(layerData, registry, fontResolver);
        }

        private static IReadOnlyList<Element> CreateChildren(ElementData data, ElementRegistry registry, FontResolver fontResolver)
        {
            if (data is not IContainer container || (container.Children is null && data is not GridElementData { Source: not null }))
            {
                return Array.Empty<Element>();
            }

            var children = new List<Element>();

            if (data is GridElementData gridData && gridData.Source?.Template is ElementData template)
            {
                int slotCount = gridData.GetSlotCount();

                for (int index = 0; index < slotCount; index++)
                {
                    var slot = Create(template, registry, fontResolver);
                    if (slot is not null)
                    {
                        // A cell starts empty and is shown once the filter hands it an item, so a grid never flashes a full set of blanks before its first assignment
                        slot.IsVisible = false;
                        children.Add(slot);
                    }
                }

                return children;
            }

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

        /// <summary>Reloads any texture belonging to an invalidated asset, walking nested elements as well as the list given, since a container's children and layers hold their own textures.</summary>
        public static bool RefreshTextures(IReadOnlyList<Element> elements, IReadOnlyCollection<IAssetName> invalidatedAssetNames)
        {
            bool wasAnyTextureRefreshed = false;

            foreach (Element element in elements)
            {
                wasAnyTextureRefreshed |= RefreshTextures(element, invalidatedAssetNames);
            }

            return wasAnyTextureRefreshed;
        }

        private static bool RefreshTextures(Element element, IReadOnlyCollection<IAssetName> invalidatedAssetNames)
        {
            bool wasAnyTextureRefreshed = false;

            if (element.TextureAssetName is not null && invalidatedAssetNames.Any(assetName => assetName.IsEquivalentTo(element.TextureAssetName)))
            {
                RefreshTexture(element);
                wasAnyTextureRefreshed = true;
            }

            wasAnyTextureRefreshed |= RefreshTextures(element.Children, invalidatedAssetNames);
            wasAnyTextureRefreshed |= RefreshTextures(element.Background, invalidatedAssetNames);
            wasAnyTextureRefreshed |= RefreshTextures(element.Foreground, invalidatedAssetNames);

            // A nested texture swap can change a container's measured size, so drop the cached layout even when this element owns no texture itself
            if (wasAnyTextureRefreshed)
            {
                element.LayoutState = null;
            }

            return wasAnyTextureRefreshed;
        }
    }
}
