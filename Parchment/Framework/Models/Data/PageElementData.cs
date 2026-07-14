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

        // Text-ish elements
        public string? Text { get; set; }

        // Image elements
        public string? ImagePath { get; set; }
        public Rectangle? ImageSourceRectangle { get; set; }
        public float ImageScale { get; set; } = 4f;

        // Layout knobs, all optional
        public int SpacingAfter { get; set; } = 8;

        public AlignmentType Alignment { get; set; } = AlignmentType.Left;

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
