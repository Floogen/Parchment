using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    [Newtonsoft.Json.JsonConverter(typeof(ElementJsonConverter))]
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

        public float Scale { get; set; } = 4f;

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
