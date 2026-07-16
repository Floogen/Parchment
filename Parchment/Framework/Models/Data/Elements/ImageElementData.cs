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
    public class ImageElementData : ElementData, ISprite
    {
        public override ElementType Type => ElementType.Image;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"{nameof(TexturePath)} is required!");
            }

            if (TextureSourceRectangle is Rectangle sourceRectangle && (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0))
            {
                return (false, $"{nameof(TextureSourceRectangle)} must have a positive width and height!");
            }

            return (true, string.Empty);
        }
    }
}
