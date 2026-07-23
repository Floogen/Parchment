using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class HeadingElementData : ElementData, ITextContent
    {
        public override ElementType Type => ElementType.Heading;

        public string? TextColor { get; set; }
        public FontType FontType { get; set; } = FontType.Dialogue;

        public string? Text { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return (false, $"\"Text\" is required.");
            }

            return base.IsValid();
        }
    }
}
