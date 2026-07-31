using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;

namespace Parchment.Framework.Models.Data.Sources
{
    /// <summary>Fills a Grid's cells from an item query rather than from authored children, narrowing them by what the reader has typed into an Input.
    /// The number of cells never changes, only what each one shows, so a filter can never reflow the page or change how many pages the book has.
    /// </summary>
    public class SourceData : BaseModel
    {
        /// <summary>The item query supplying the candidates, such as "ALL_ITEMS (O)". Resolved once and cached, so this cost is paid on load rather than on a keystroke.</summary>
        public string ItemQuery { get; set; } = "ALL_ITEMS (O)";

        /// <summary>A game state query each candidate must pass, evaluated with that item in context. This is where a category filter such as "ITEM_CATEGORY Target -4" belongs.</summary>
        public string? PerItemCondition { get; set; }

        /// <summary>The InputId whose text narrows the candidates. When null the results are unfiltered, which is a plain paged list of everything the query returned.</summary>
        public string? InputId { get; set; }

        /// <summary>What the candidates are sorted by before they are handed to the cells.</summary>
        public ResultOrder OrderBy { get; set; } = ResultOrder.DisplayName;

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

            var templateIsValidData = Template.IsValid();
            if (templateIsValidData.Result is false)
            {
                return (false, $"[Template] {templateIsValidData.Error}");
            }

            return (true, string.Empty);
        }
    }
}
