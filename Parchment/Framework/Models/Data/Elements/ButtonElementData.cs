using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class ButtonElementData : ElementData, ISprite, ITextContent
    {
        public override ElementType Type => ElementType.Button;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        public FontType FontType { get; set; }
        public string? Text { get; set; }
        public string? TextColor { get; set; }
        public float TextScale { get; set; } = 1f;

        public int Padding { get; set; } = 0;
        public SizingMode Sizing { get; set; } = SizingMode.ShrinkToFit;
        public int? Width { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"\"TexturePath\" is required.");
            }

            if (string.IsNullOrWhiteSpace(Action))
            {
                return (false, $"\"Action\" is required.");
            }

            if (Sizing is SizingMode.Fixed && Width is null)
            {
                return (false, $"\"Width\" is required when \"Sizing\" is {nameof(SizingMode.Fixed)}.");
            }

            if (Padding < 0)
            {
                return (false, $"\"Padding\" cannot be negative.");
            }
            return base.IsValid();
        }
    }
}
