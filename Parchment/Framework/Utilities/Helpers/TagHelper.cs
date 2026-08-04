using Parchment.Framework.Models;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Reads the tags an element carries. Most are left alone, being markers for other mods to read off whatever the cursor is over,
    /// while the handful Parchment recognises name a piece of game content the element is about.
    /// </summary>
    public static class TagHelper
    {
        /// <summary>Marks a tag as naming an NPC by internal name, such as "NpcId.Abigail". Written by the author, as Parchment has no other way to know an element is about an NPC</summary>
        public const string NPC_PREFIX = "NpcId.";

        /// <summary>Marks a tag as naming an item by qualified ID, such as "ItemId.(O)145". Derived from whatever item the element is currently holding rather than written by the author</summary>
        public const string ITEM_PREFIX = "ItemId.";

        // Every prefix Parchment acts on, which is what a prefix with nothing behind it is reported against at load
        private static readonly string[] KnownPrefixes = new string[] { NPC_PREFIX, ITEM_PREFIX };

        /// <summary>What the %Tags% token joins an element's tags with, and what the queries reading that token split them back on.
        /// A tag holding this character can't survive the round trip, which is why the queries taking tags directly are the ones to reach for when a tag has to carry punctuation.
        /// </summary>
        public const char LIST_SEPARATOR = ',';

        /// <summary>Joins an element's tags into the single value the %Tags% token stands for.</summary>
        public static string Join(IEnumerable<string> tags)
        {
            return string.Join(LIST_SEPARATOR, tags);
        }

        /// <summary>The tags in a joined list, with the empty entries a trailing separator would leave dropped.</summary>
        public static IEnumerable<string> Split(string? tagList)
        {
            if (string.IsNullOrWhiteSpace(tagList) is true)
            {
                return Enumerable.Empty<string>();
            }

            return tagList.Split(LIST_SEPARATOR, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>Whether a joined tag list holds a tag, ignoring case.</summary>
        public static bool ListHasTag(string? tagList, string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag) is true)
            {
                return false;
            }

            return Split(tagList).Any(listTag => string.Equals(listTag, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Whether any tag in a joined list contains the given text, ignoring case.
        /// Empty text matches a list with anything at all in it, which is what leaves an untouched search box showing everything.
        /// </summary>
        public static bool ListHasTagMatching(string? tagList, string? text)
        {
            var tags = Split(tagList).ToList();

            if (tags.Count is 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(text) is true)
            {
                return true;
            }

            return tags.Any(listTag => listTag.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The value behind a tag's prefix, or null when the tag doesn't use that prefix.
        /// A prefix with nothing behind it also gives null, since it names nothing.
        /// </summary>
        public static string? GetValue(string? tag, string prefix)
        {
            if (string.IsNullOrWhiteSpace(tag) is true || tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is false)
            {
                return null;
            }

            string value = tag.Substring(prefix.Length).Trim();

            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>Whether a tag uses one of the prefixes Parchment acts on but names nothing after it, which is a mistyped tag rather than a marker meant for another mod.
        /// A prefix Parchment doesn't recognise passes, as a tag is free-form by default and only the author knows what reads it.
        /// </summary>
        public static bool IsEmptyKnownPrefix(string tag)
        {
            string trimmed = tag.Trim();

            foreach (string prefix in KnownPrefixes)
            {
                // The bare prefix without its separator, which is the likelier way to write it by hand
                if (string.Equals(trimmed, prefix.TrimEnd('.'), StringComparison.OrdinalIgnoreCase) is true)
                {
                    return true;
                }

                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is true && GetValue(trimmed, prefix) is null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The item an element is about, taken from the element itself or from the nearest container holding one, so a label inside a Grid cell answers with the cell's item.
        /// This reads the runtime item rather than parsing the derived tag back out, as the instance is already built and a cell's changes as its filter narrows.
        /// </summary>
        public static Item? ResolveItem(Element? element)
        {
            for (Element? current = element; current is not null; current = current.Parent)
            {
                if (current.AssignedItem is Item item)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>The NPC an element is about, named by an <see cref="NPC_PREFIX"/> tag on the element or on the nearest container carrying one.
        /// Null when nothing in that chain names an NPC, and also when the named one isn't loaded, which is a save without that character rather than an authoring mistake.
        /// </summary>
        public static NPC? ResolveNpc(Element? element)
        {
            for (Element? current = element; current is not null; current = current.Parent)
            {
                if (current.Data.Tags is null)
                {
                    continue;
                }

                foreach (string tag in current.Data.Tags)
                {
                    if (GetValue(tag, NPC_PREFIX) is not string name)
                    {
                        continue;
                    }

                    if (Game1.getCharacterFromName(name, mustBeVillager: false) is NPC npc)
                    {
                        return npc;
                    }
                }
            }

            return null;
        }
    }
}
