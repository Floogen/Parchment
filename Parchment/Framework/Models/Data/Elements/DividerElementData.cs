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
    public class DividerElementData : ElementData, ISprite
    {
        public override ElementType Type => ElementType.Divider;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }

        /// <summary>
        /// Only applies to textureless dividers
        /// </summary>
        public int Thickness { get; set; } = 1;

        public SizingMode Sizing { get; set; } = SizingMode.Fill;
        public int? Width { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (Thickness <= 0)
            {
                return (false, $"\"Thickness\" must be positive!");
            }

            if (Sizing is SizingMode.Fixed && Width is null)
            {
                return (false, $"\"Width\" is required when \"Sizing\" is {nameof(SizingMode.Fixed)}!");
            }

            if (Sizing is SizingMode.ShrinkToFit && string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"\"Sizing\" cannot be {nameof(SizingMode.ShrinkToFit)} without a \"TexturePath\", since a plain line has no natural width!");
            }

            return (true, string.Empty);
        }
    }
}
