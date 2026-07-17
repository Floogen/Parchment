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

        /// <summary>The ordered pages. Sorted by <see cref="PageData.Order"/> (stable) at load.</summary>
        public List<PageData> Pages { get; set; } = new List<PageData>();

        /// <summary>Elements drawn behind the book sprite, positioned via <see cref="ElementData.Position"/> relative to the book's top-left. Negative coordinates place content outside the book's edges.</summary>
        public List<ElementData>? Underlay { get; set; }

        /// <summary>Elements drawn in front of the book sprite and its pages, positioned via <see cref="ElementData.Position"/> relative to the book's top-left.</summary>
        public List<ElementData>? Overlay { get; set; }

        public BookLayoutData Layout { get; set; } = new BookLayoutData();
        /// <summary>The tint applied to the book sprite. Defaults to white / untinted.</summary>
        public string? TintColor { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
