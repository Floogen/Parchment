using Parchment.Framework.Models.Data.Results;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Models
{
    /// <summary>The runtime half of a Grid's <see cref="ResultsData"/>: the candidates its source resolved to, and the last filter applied to them.
    /// Candidates are resolved once and reused, so narrowing them as the reader types costs a substring pass over cached strings rather than another item query.
    /// </summary>
    public class ResultSet
    {
        private readonly ResultsData _data;

        private List<ResultCandidate>? _candidates;
        private string? _appliedFilter;
        private bool _hasApplied;

        /// <summary>How many candidates the source returned in total, before any filtering.</summary>
        public int TotalCount => _candidates?.Count ?? 0;

        /// <summary>How many cells are currently showing an item.</summary>
        public int DisplayedCount { get; private set; }

        /// <summary>How many candidates matched the filter, which is larger than <see cref="DisplayedCount"/> once the matches outnumber the cells.</summary>
        public int MatchedCount { get; private set; }

        public ResultSet(ResultsData data)
        {
            _data = data;
        }

        /// <summary>Drops the cached candidates so the source is resolved again. Called when assets are invalidated, since an item query's answer can change with them.</summary>
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

            IList<ItemQueryResult>? results = ItemQueryResolver.TryResolve(_data.Source, new ItemQueryContext(), perItemCondition: _data.PerItemCondition, logError: (query, error) => Parchment.monitor.LogOnce($"A Grid's \"Source\" of '{query}' failed: {error}", LogLevel.Warn));

            if (results is null)
            {
                return;
            }

            foreach (ItemQueryResult result in results)
            {
                if (result.Item is null || string.IsNullOrWhiteSpace(result.Item.QualifiedItemId))
                {
                    continue;
                }

                _candidates.Add(new ResultCandidate(result.Item.QualifiedItemId, result.Item.DisplayName ?? string.Empty));
            }

            switch (_data.OrderBy)
            {
                case ResultOrder.DisplayName:
                    _candidates = _candidates.OrderBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                case ResultOrder.ItemId:
                    _candidates = _candidates.OrderBy(candidate => candidate.QualifiedItemId, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
            }
        }

        private readonly struct ResultCandidate
        {
            public string QualifiedItemId { get; }
            public string DisplayName { get; }

            public ResultCandidate(string qualifiedItemId, string displayName)
            {
                QualifiedItemId = qualifiedItemId;
                DisplayName = displayName;
            }
        }
    }
}
