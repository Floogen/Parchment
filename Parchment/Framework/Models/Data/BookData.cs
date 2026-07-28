using Parchment.Framework.Models.Data.Books;
using Parchment.Framework.Models.Data.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class BookData : BaseModel
    {
        /// <summary>The schema version this book was authored against.</summary>
        public string Format { get; set; } = "1.0.0";


        public string Id { get; set; } = string.Empty;

        public BookAppearanceData Appearance { get; set; } = new BookAppearanceData();
        public PageCurlData PageCurl { get; set; } = new PageCurlData();
        public BookAnimationData Animation { get; set; } = new BookAnimationData();

        /// <summary>
        /// Sprite path for book item.
        /// </summary>
        public string? SpritePath { get; set; }

        /// <summary>The book's pages, in reading order. Pages are shown in the order they appear here,
        /// and pages sharing a <see cref="PageData.ChapterId"/> must be kept contiguous or they will be treated as separate chapters.</summary>
        public List<PageData> Pages { get; set; } = new List<PageData>();

        /// <summary>Elements drawn behind the book sprite, positioned via <see cref="ElementData.Position"/> relative to the book's top-left. Negative coordinates place content outside the book's edges.</summary>
        public List<ElementData>? Underlay { get; set; }

        /// <summary>Elements drawn in front of the book sprite and its pages, positioned via <see cref="ElementData.Position"/> relative to the book's top-left.</summary>
        public List<ElementData>? Overlay { get; set; }

        public BookLayoutData Layout { get; set; } = new BookLayoutData();

        /// <summary>Whether the book arrives shut, holding on its cover until the reader clicks it open, rather than opening itself once it
        /// has slid into place. Independent of <see cref="ExitToCover"/>, which governs the other end of the reading.</summary>
        public bool StartOnCover { get; set; } = false;

        /// <summary>Whether asking to close the book shuts it in place first, leaving its cover on screen, rather than leaving the menu.
        /// A second close request then leaves, and clicking the cover reopens to the page the reader left off on.
        /// This governs only the reader's own close request. The cover stays reachable from the ViewCover action either way.
        /// Decorate the cover by giving <see cref="Overlay"/> or <see cref="Underlay"/> elements a condition on the Cover book state.
        /// </summary>
        public bool ExitToCover { get; set; } = false;

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return (false, $"\"Id\" is required.");
            }

            if (string.IsNullOrWhiteSpace(Format))
            {
                return (false, $"\"Format\" is required.");
            }

            if (Pages.Count == 0)
            {
                return (false, $"\"Pages\" must contain at least one page.");
            }

            foreach (PageData page in Pages)
            {
                var isValidData = page.IsValid();
                if (isValidData.Result is false)
                {
                    return (false, $"Page \"{page.Id}\": {isValidData.Error}");
                }
            }

            if (Overlay is not null)
            {
                foreach (var element in Overlay)
                {
                    var isValidData = element.IsValid();
                    if (isValidData.Result is false)
                    {
                        return (false, $"Element \"{element.Id}\" ({element.Type}): {isValidData.Error}");
                    }
                }
            }

            if (Underlay is not null)
            {
                foreach (var element in Underlay)
                {
                    var isValidData = element.IsValid();
                    if (isValidData.Result is false)
                    {
                        return (false, $"Element \"{element.Id}\" ({element.Type}): {isValidData.Error}");
                    }
                }
            }

            return (true, string.Empty);
        }
    }
}
