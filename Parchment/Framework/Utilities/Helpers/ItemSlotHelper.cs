using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Points an already-built element at an item, which is what lets a Grid's cells be made once and re-pointed as a filter narrows them.
    /// Everything an item contributes lives on the runtime element rather than on its data, so this changes what a cell shows without touching the shared element data underneath it.
    /// </summary>
    public static class ItemSlotHelper
    {
        /// <summary>Applies an item to a cell and everything inside it. Passing null empties the cell, which is what a slot past the last match gets.</summary>
        public static void ApplyItem(Element element, string? qualifiedItemId)
        {
            ParsedItemData? itemData = string.IsNullOrWhiteSpace(qualifiedItemId) ? null : ItemRegistry.GetData(qualifiedItemId);

            // Built once here rather than per token resolution, since a handful of instances per filter change is nothing while a handful per frame would not be
            Item? item = itemData is null ? null : ItemRegistry.Create(qualifiedItemId, allowNull: true);

            Apply(element, qualifiedItemId, itemData, item);
        }

        private static void Apply(Element element, string? qualifiedItemId, ParsedItemData? itemData, Item? item)
        {
            element.AssignedItemId = qualifiedItemId;
            element.AssignedItemData = itemData;
            element.AssignedItem = item;

            // An Image with its own ItemId is authored art rather than a hole for the result to fill, so it is left alone
            if (element.Data is ImageElementData imageData && string.IsNullOrWhiteSpace(imageData.ItemId) is true && string.IsNullOrWhiteSpace(imageData.TexturePath) is true)
            {
                element.Texture = itemData?.GetTexture();
                element.SourceRectangle = itemData?.GetSourceRect();

                // Only filled in where the author left them out, the same rule the element factory follows for an authored ItemId
                element.DisplayName = element.Data.DisplayName ?? itemData?.DisplayName;
                element.Description = element.Data.Description ?? itemData?.Description;
            }

            ApplyToList(element.Children, qualifiedItemId, itemData, item);
            ApplyToList(element.Background, qualifiedItemId, itemData, item);
            ApplyToList(element.Foreground, qualifiedItemId, itemData, item);
        }

        private static void ApplyToList(System.Collections.Generic.IReadOnlyList<Element> elements, string? qualifiedItemId, ParsedItemData? itemData, Item? item)
        {
            foreach (Element element in elements)
            {
                Apply(element, qualifiedItemId, itemData, item);
            }
        }
    }
}
