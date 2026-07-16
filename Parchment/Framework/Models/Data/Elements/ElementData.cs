using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Elements
{
    [Newtonsoft.Json.JsonConverter(typeof(ElementJsonConverter))]
    public abstract class ElementData : BaseModel
    {
        public string? Id { get; set; }
        public abstract ElementType Type { get; }

        public AlignmentType Alignment { get; set; } = AlignmentType.Left;

        /// <summary>
        /// Optional. If given, increases buffer between elements.
        /// </summary>
        public virtual int SpacingAfter { get; set; } = 8;
        public float Scale { get; set; } = 4f;

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
