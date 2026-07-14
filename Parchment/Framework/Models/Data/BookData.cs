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

        /// <summary>
        /// Display name of book item.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Description used by book item.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Sprite path for book item.
        /// </summary>
        public string? SpritePath { get; set; }

        /// <summary>The ordered pages. Sorted by <see cref="PageData.Order"/> (stable) at load.</summary>
        public List<PageData> Pages { get; set; } = new List<PageData>();

        public BookLayoutData Layout { get; set; } = new BookLayoutData();

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
