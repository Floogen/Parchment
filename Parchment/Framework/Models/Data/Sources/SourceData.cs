using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Utilities.Helpers;
using System;

namespace Parchment.Framework.Models.Data.Sources
{
    /// <summary>Fills a Grid's cells from an item query rather than from authored children, narrowing them by what the reader has typed into an Input.
    /// The number of cells never changes, only what each one shows, so a filter can never reflow the page or change how many pages the book has.
    /// </summary>
    public class SourceData : BaseModel
    {
        /// <summary>The <see cref="OrderBy"/> value that leaves the candidates in whatever order the item query returned them.</summary>
        public const string NoOrder = "None";

        /// <summary>The item query supplying the candidates, such as "ALL_ITEMS (O)". Resolved once and cached, so this cost is paid on load rather than on a keystroke.</summary>
        public string ItemQuery { get; set; } = "ALL_ITEMS (O)";

        /// <summary>A game state query each candidate must pass, evaluated with that item in context. This is where a category filter such as "ITEM_CATEGORY Target -4" belongs.</summary>
        public string? PerItemCondition { get; set; }

        /// <summary>The InputId whose text narrows the candidates. When null the results are unfiltered, which is a plain paged list of everything the query returned.</summary>
        public string? InputId { get; set; }

        /// <summary>The item property the candidates are sorted by before they are handed to the cells, named as the %Item.Something% token names it. Defaults to the item query's own order, which is what the query paid for rather than a sort on top of it.
        /// A candidate that can't answer the property sorts last, whichever direction the rest are going.
        /// </summary>
        public string OrderBy { get; set; } = NoOrder;

        /// <summary>Reverses the order, so the highest price or the last name comes first.</summary>
        public bool OrderDescending { get; set; }

        /// <summary>How many cells the candidates fill. When null the grid's Columns and Rows decide, which is the usual way to say it.</summary>
        public int? Count { get; set; }

        /// <summary>The element each cell is built from. One template makes every cell, and the item is applied to any Image inside it that has no ItemId of its own.</summary>
        public ElementData? Template { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(ItemQuery))
            {
                return (false, $"\"ItemQuery\" is required.");
            }

            if (Template is null)
            {
                return (false, $"\"Template\" is required.");
            }

            if (Count is int count && count <= 0)
            {
                return (false, $"\"Count\" must be positive.");
            }

            if (string.IsNullOrWhiteSpace(OrderBy) is false && string.Equals(OrderBy, NoOrder, StringComparison.OrdinalIgnoreCase) is false && ItemPropertyResolver.IsKnown(OrderBy) is false)
            {
                return (false, $"\"OrderBy\" of '{OrderBy}' is not an item property Parchment knows. Try one of: {string.Join(", ", ItemPropertyResolver.GetNames())}, or \"{NoOrder}\".");
            }

            var templateIsValidData = Template.IsValid();
            if (templateIsValidData.Result is false)
            {
                return (false, $"[Template] {templateIsValidData.Error}");
            }

            return (true, string.Empty);
        }
    }
}
