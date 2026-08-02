using Parchment.Framework.Models.Data.Sources;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Parchment.Framework.Models
{
    /// <summary>The runtime half of a Grid's <see cref="SourceData"/>: the candidates its item query resolved to, and the last filter applied to them.
    /// Candidates are resolved once and reused, so narrowing them as the reader types costs a substring pass over cached strings rather than another item query.
    /// </summary>
    public class ResultSet
    {
        private readonly SourceData _data;

        private List<ResultCandidate>? _candidates;
        private string? _appliedFilter;
        private bool _hasApplied;

        /// <summary>How many candidates the item query returned in total, before any filtering.</summary>
        public int TotalCount => _candidates?.Count ?? 0;

        /// <summary>How many cells are currently showing an item.</summary>
        public int DisplayedCount { get; private set; }

        /// <summary>How many candidates matched the filter, which is larger than <see cref="DisplayedCount"/> once the matches outnumber the cells.</summary>
        public int MatchedCount { get; private set; }

        public ResultSet(SourceData data)
        {
            _data = data;
        }

        /// <summary>Drops the cached candidates so the item query is resolved again. Called when assets are invalidated, since an item query's answer can change with them.</summary>
        public void Invalidate()
        {
            _candidates = null;
            _hasApplied = false;
        }

        /// <summary>Assigns items to the cells if anything has changed since the last pass, and reports whether it did.
        /// A true answer means the layout has to be invalidated, as cells have gained or lost their contents.
        /// </summary>
        public bool TryRefresh(IReadOnlyList<Element> slots)
        {
            string filter = string.IsNullOrWhiteSpace(_data.InputId) ? string.Empty : Parchment.inputManager.GetText(_data.InputId);

            if (_hasApplied is true && string.Equals(_appliedFilter, filter, StringComparison.Ordinal) is true)
            {
                return false;
            }

            EnsureCandidates();

            _appliedFilter = filter;
            _hasApplied = true;

            return Assign(slots, filter);
        }

        private bool Assign(IReadOnlyList<Element> slots, string filter)
        {
            int slotIndex = 0;
            int matchedCount = 0;

            if (_candidates is not null)
            {
                foreach (ResultCandidate candidate in _candidates)
                {
                    if (Matches(candidate, filter) is false)
                    {
                        continue;
                    }

                    matchedCount++;

                    if (slotIndex >= slots.Count)
                    {
                        continue;
                    }

                    ItemSlotHelper.ApplyItem(slots[slotIndex], candidate.QualifiedItemId);
                    slots[slotIndex].IsVisible = true;

                    slotIndex++;
                }
            }

            // Cells past the last match are emptied rather than left showing whatever they held before the filter narrowed
            for (int index = slotIndex; index < slots.Count; index++)
            {
                ItemSlotHelper.ApplyItem(slots[index], null);
                slots[index].IsVisible = false;
            }

            MatchedCount = matchedCount;
            DisplayedCount = slotIndex;

            return true;
        }

        private bool Matches(ResultCandidate candidate, string filter)
        {
            if (string.IsNullOrEmpty(filter) is true)
            {
                return true;
            }

            return candidate.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) || candidate.QualifiedItemId.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureCandidates()
        {
            if (_candidates is not null)
            {
                return;
            }

            _candidates = new List<ResultCandidate>();

            IList<ItemQueryResult>? results = ItemQueryResolver.TryResolve(_data.ItemQuery, new ItemQueryContext(), perItemCondition: _data.PerItemCondition, logError: (query, error) => Parchment.monitor.LogOnce($"A Grid's \"ItemQuery\" of '{query}' failed: {error}", LogLevel.Warn));

            if (results is null)
            {
                return;
            }

            string? orderProperty = GetOrderProperty(out ItemPropertyKind orderKind);

            foreach (ItemQueryResult result in results)
            {
                if (result.Item is null || string.IsNullOrWhiteSpace(result.Item.QualifiedItemId))
                {
                    continue;
                }

                var sortKey = ResolveSortKey(result.Item, orderProperty, orderKind);

                _candidates.Add(new ResultCandidate(result.Item.QualifiedItemId, result.Item.DisplayName ?? string.Empty, sortKey.Text, sortKey.Number));
            }

            Sort(orderProperty, orderKind);
        }

        /// <summary>The property the candidates are ordered by, or null when they are left in the item query's order. An unknown name orders nothing, as validation has already rejected the book that asked for it.</summary>
        private string? GetOrderProperty(out ItemPropertyKind kind)
        {
            kind = ItemPropertyKind.Text;

            if (string.IsNullOrWhiteSpace(_data.OrderBy) is true || string.Equals(_data.OrderBy, SourceData.NoOrder, StringComparison.OrdinalIgnoreCase) is true)
            {
                return null;
            }

            return ItemPropertyResolver.TryGetKind(_data.OrderBy, out kind) is true ? _data.OrderBy : null;
        }

        /// <summary>What one candidate sorts by, read off the item while the query still has it in hand. Both halves are null when the property had nothing to give, which is what sends the candidate to the end.</summary>
        private static (string? Text, double? Number) ResolveSortKey(ISalable salable, string? orderProperty, ItemPropertyKind kind)
        {
            if (orderProperty is null)
            {
                return (null, null);
            }

            ParsedItemData? itemData = ItemRegistry.GetData(salable.QualifiedItemId);
            if (itemData is null)
            {
                return (null, null);
            }

            // A query can hand back something salable that isn't an Item, which leaves the properties needing one unanswered rather than throwing
            string? value = ItemPropertyResolver.Resolve(orderProperty, itemData, salable as Item);
            if (string.IsNullOrEmpty(value) is true)
            {
                return (null, null);
            }

            if (kind is ItemPropertyKind.Number)
            {
                return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double number) is true ? (null, number) : (null, null);
            }

            return (value, null);
        }

        private void Sort(string? orderProperty, ItemPropertyKind kind)
        {
            if (orderProperty is null || _candidates is null)
            {
                return;
            }

            // Candidates that couldn't answer the property go last whichever direction the rest are going, so reversing the order doesn't bring a wall of blanks to the front
            IOrderedEnumerable<ResultCandidate> ordered = _candidates.OrderBy(candidate => candidate.HasSortKey is true ? 0 : 1);

            if (kind is ItemPropertyKind.Number)
            {
                ordered = _data.OrderDescending is true ? ordered.ThenByDescending(candidate => candidate.SortNumber) : ordered.ThenBy(candidate => candidate.SortNumber);
            }
            else
            {
                ordered = _data.OrderDescending is true ? ordered.ThenByDescending(candidate => candidate.SortText, StringComparer.OrdinalIgnoreCase) : ordered.ThenBy(candidate => candidate.SortText, StringComparer.OrdinalIgnoreCase);
            }

            _candidates = ordered.ToList();
        }

        private readonly struct ResultCandidate
        {
            public string QualifiedItemId { get; }
            public string DisplayName { get; }

            /// <summary>What this candidate sorts by, resolved once alongside the item query rather than on every comparison. Only one of the two is ever set, and neither is when the item couldn't answer the property.</summary>
            public string? SortText { get; }
            public double? SortNumber { get; }

            public bool HasSortKey => SortText is not null || SortNumber is not null;

            public ResultCandidate(string qualifiedItemId, string displayName, string? sortText, double? sortNumber)
            {
                QualifiedItemId = qualifiedItemId;
                DisplayName = displayName;
                SortText = sortText;
                SortNumber = sortNumber;
            }
        }
    }
}
