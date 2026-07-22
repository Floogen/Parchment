using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PageData : BaseModel
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>The chapter this page belongs to (pages sharing the same value belong in same chapter).
        /// Chapters are navigation-isolated and page turning never crosses a chapter boundary unless via button.
        /// </summary>
        public string? ChapterId { get; set; }

        public List<ElementData> Elements { get; set; } = new List<ElementData>();

        /// <summary>
        /// Elements drawn behind <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// </summary>
        public List<ElementData>? Background { get; set; }

        /// <summary>
        /// Elements drawn over <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// </summary>
        public List<ElementData>? Foreground { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
