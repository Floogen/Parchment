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
    public class ParagraphElementData : ElementData, ITextContent
    {
        public override ElementType Type => ElementType.Paragraph;

        public string? TextColor { get; set; }
        public FontType FontType { get; set; } = FontType.Small;

        public string? Text { get; set; }

        /// <summary>
        /// Optional. The width the paragraph occupies, in unscaled pixels (multiplied by <see cref="ElementData.Scale"/>). Text wraps at this width and the element reserves it,
        /// so <see cref="ElementData.Alignment"/> places lines within it and hit testing covers the whole box rather than just the longest line.
        /// When null, the text wraps at the full width available and the element is only as wide as its longest line. Clamped to the width available, so this can only narrow the paragraph and never widen it.
        /// </summary>
        public int? Width { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return (false, $"\"Text\" is required.");
            }

            if (Width is int width && width <= 0)
            {
                return (false, $"\"Width\" must be positive.");
            }

            return base.IsValid();
        }
    }
}
