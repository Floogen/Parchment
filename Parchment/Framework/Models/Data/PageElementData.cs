using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PageElementData : BaseModel
    {
        public string? Id { get; set; }

        public PageElementType Type { get; set; } = PageElementType.Unknown;

        public string? Text { get; set; }

        // Image elements
        public string? ImagePath { get; set; }
        public Rectangle? ImageSourceRectangle { get; set; }
        public float ImageScale { get; set; } = 4f;

        // Optional spacing
        public int SpacingAfter { get; set; } = 8;

        public AlignmentType Alignment { get; set; } = AlignmentType.Left;

        // Panel specific properties
        /// <summary>
        /// Optional. If not given, fill the entire Page's width
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Optional. If not given, increase based on total height needed by Children
        /// </summary>
        public int Height { get; set; }
        public float PanelScale { get; set; } = 4f;

        // Sub-elements
        public List<PageElementData>? Children { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
