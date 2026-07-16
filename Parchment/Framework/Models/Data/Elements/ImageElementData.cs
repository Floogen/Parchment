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
            throw new NotImplementedException();
        }
    }
}
