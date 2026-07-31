using Parchment.Framework.Models.Enums;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>The item properties the %Item.Something% token can reach, which are the same names a Grid's Source orders by.
    /// Deliberately a list rather than reflection over the item: these names are Parchment's to keep, so a game update that renames a field underneath one of them is a line to fix here rather than a break in every content pack using it.
    /// </summary>
    public static class ItemPropertyResolver
    {
        /// <summary>One known property: how it is read off an item, and how it compares when a Source orders by it.</summary>
        private readonly struct ItemProperty
        {
            public Func<ParsedItemData, Item?, string?> Resolve { get; }
            public ItemPropertyKind Kind { get; }

            public ItemProperty(Func<ParsedItemData, Item?, string?> resolve, ItemPropertyKind kind = ItemPropertyKind.Text)
            {
                Resolve = resolve;
                Kind = kind;
            }
        }

        private static readonly Dictionary<string, ItemProperty> _properties = new Dictionary<string, ItemProperty>(StringComparer.OrdinalIgnoreCase)
        {
            // Ids are text rather than numbers, as a mod's item is as likely to be "Bob.Cool_Sword" as it is to be 128
            ["Id"] = new ItemProperty((itemData, item) => itemData.ItemId),
            ["Name"] = new ItemProperty((itemData, item) => itemData.DisplayName),
            ["InternalName"] = new ItemProperty((itemData, item) => itemData.InternalName),
            ["Description"] = new ItemProperty((itemData, item) => itemData.Description),
            ["Type"] = new ItemProperty((itemData, item) => itemData.ObjectType),

            // The name rather than the number
            ["Category"] = new ItemProperty((itemData, item) => item?.getCategoryName()),
            ["Price"] = new ItemProperty((itemData, item) => item?.salePrice().ToString(), ItemPropertyKind.Number)
        };

        /// <summary>The value of one property, or null when the name isn't one Parchment knows or the item can't answer it.</summary>
        public static string? Resolve(string propertyName, ParsedItemData itemData, Item? item)
        {
            if (_properties.TryGetValue(propertyName, out ItemProperty property) is false)
            {
                return null;
            }

            return property.Resolve(itemData, item);
        }

        /// <summary>How a property compares, for a Source ordering by it. False when the name isn't one Parchment knows.</summary>
        public static bool TryGetKind(string propertyName, out ItemPropertyKind kind)
        {
            if (_properties.TryGetValue(propertyName, out ItemProperty property) is false)
            {
                kind = ItemPropertyKind.Text;
                return false;
            }

            kind = property.Kind;
            return true;
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
