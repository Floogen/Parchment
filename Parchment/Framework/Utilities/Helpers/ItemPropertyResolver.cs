using StardewValley;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>The item properties the %Item.Something% token can reach.
    /// Deliberately a list rather than reflection over the item: these names are Parchment's to keep, so a game update that renames a field underneath one of them is a line to fix here rather than a break in every content pack using it.
    /// </summary>
    public static class ItemPropertyResolver
    {
        private static readonly Dictionary<string, Func<ParsedItemData, Item?, string?>> _properties = new Dictionary<string, Func<ParsedItemData, Item?, string?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = (itemData, item) => itemData.ItemId,
            ["Name"] = (itemData, item) => itemData.DisplayName,
            ["InternalName"] = (itemData, item) => itemData.InternalName,
            ["Description"] = (itemData, item) => itemData.Description,
            ["Type"] = (itemData, item) => itemData.ObjectType,

            // The name rather than the number
            ["Category"] = (itemData, item) => item?.getCategoryName(),
            ["Price"] = (itemData, item) => item?.salePrice().ToString()
        };

        /// <summary>The value of one property, or null when the name isn't one Parchment knows or the item can't answer it.</summary>
        public static string? Resolve(string propertyName, ParsedItemData itemData, Item? item)
        {
            if (_properties.TryGetValue(propertyName, out Func<ParsedItemData, Item?, string?>? property) is false)
            {
                return null;
            }

            return property(itemData, item);
        }

        /// <summary>Whether a name is one of the known properties, used to tell a typo apart from a property that simply had nothing to give.</summary>
        public static bool IsKnown(string propertyName)
        {
            return _properties.ContainsKey(propertyName);
        }

        /// <summary>Every property name, for reporting what was expected when an author gets one wrong.</summary>
        public static IEnumerable<string> GetNames()
        {
            return _properties.Keys;
        }
    }
}
