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
        public int MarginLeft { get; set; } = 0;
        public int MarginRight { get; set; } = 0;

        public float Scale { get; set; } = 1f;

        /// <summary>
        /// The page-local position in screen pixels, relative to the page's content area. Unlike <see cref="SpacingAfter"/> and other spacing fields, this is not multiplied by <see cref="Scale"/>.
        /// Changing an element's scale resizes it in place rather than moving it.
        /// </summary>
        public Point Position { get; set; } = Point.Zero;

        /// <summary>A trigger action to run when this element is clicked. When null, the element is not interactive.</summary>
        public string? Action { get; set; }

        /// <summary>The sound to play when this element is clicked. Only used when <see cref="Action"/> is set.</summary>
        public string? Sound { get; set; } = "bigSelect";

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
