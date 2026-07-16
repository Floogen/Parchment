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
    public class BannerElementData : ElementData, ITextContent, ISprite
    {
        public override ElementType Type => ElementType.Banner;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }

        public string? Color { get; set; }
        public FontType FontType { get; set; }
        public string? Text { get; set; }

        /// <summary>
        /// The width of the left and right caps, in unscaled sprite pixels. The remainder of <see cref="TextureSourceRectangle"/> between them is the middle segment, stretched to fill.
        /// When null, the strip is split into equal thirds.
        /// </summary>
        public int? CapWidth { get; set; }

        public int Padding { get; set; } = 0;
        public SizingMode Sizing { get; set; } = SizingMode.ShrinkToFit;
        public int? Width { get; set; }

        /// <summary>
        /// The scale applied to the banner's text, independent of <see cref="ElementData.Scale"/>, which scales the
        /// banner sprite. The sprite is pixel art authored at 1x and typically drawn at 4x; the font is not.
        /// </summary>
        public float TextScale { get; set; } = 1f;

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"\"TexturePath\" is required.");
            }

            if (CapWidth is int capWidth && capWidth <= 0)
            {
                return (false, $"\"CapWidth\" must be positive.");
            }

            if (TextureSourceRectangle is Rectangle sourceRectangle && CapWidth is int cap && cap * 2 >= sourceRectangle.Width)
            {
                return (false, $"\"CapWidth\" ({cap}) leaves no middle segment in a source rectangle {sourceRectangle.Width}px wide.");
            }

            return (true, string.Empty);
        }
    }
}
