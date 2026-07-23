using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PanelElementData : ElementData, ISprite, IContainer
    {
        public override ElementType Type => ElementType.Panel;

        /// <summary>
        /// Optional. If not given, the panel will have no background.
        /// Note: Texture must support 9-slice scaling
        /// </summary>
        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        public List<ElementData>? Children { get; set; }

        /// <summary>
        /// Increases space between children and panel's border.
        /// </summary>
        public int Padding { get; set; } = 0;

        public SizingMode Sizing { get; set; } = SizingMode.Fill;
        public int? Width { get; set; }

        /// <summary>
        /// The height of the panel's content area, in unscaled sprite pixels (multiplied by <see cref="ElementData.Scale"/>).
        /// When null, the panel is as tall as its stacked children need. When set, the content area is exactly this tall
        /// and children that would stack past it are dropped. Independent of <see cref="Sizing"/>, which controls width only.
        /// </summary>
        public int? Height { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (Sizing is SizingMode.Fixed && Width is null)
            {
                return (false, $"\"Width\" is required when \"Sizing\" is {nameof(SizingMode.Fixed)}!");
            }

            if (Width is int width && width <= 0)
            {
                return (false, $"\"Width\" must be positive!");
            }

            if (Height is int height && height <= 0)
            {
                return (false, $"\"Height\" must be positive!");
            }

            return base.IsValid();
        }
    }
}
