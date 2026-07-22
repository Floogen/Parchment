using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Books
{
    public class BookLayoutData : BaseModel
    {
        /// <summary>The gap between the book frame's outer edge and the page content, in unscaled sprite pixels.</summary>
        public int MarginOuter { get; set; } = 12;

        /// <summary>The gap between the spine and the page content on each side, in unscaled sprite pixels.</summary>
        public int MarginSpine { get; set; } = 6;

        /// <summary>The gap between the book frame's top edge and the page content, in unscaled sprite pixels.</summary>
        public int MarginTop { get; set; } = 27;

        /// <summary>The gap between the book frame's bottom edge and the page content, in unscaled sprite pixels.</summary>
        public int MarginBottom { get; set; } = 28;

        public override (bool Result, string Error) IsValid()
        {
            if (MarginOuter < 0 || MarginSpine < 0 || MarginTop < 0 || MarginBottom < 0)
            {
                return (false, "Margins cannot be negative.");
            }

            return (true, string.Empty);
        }
    }
}
