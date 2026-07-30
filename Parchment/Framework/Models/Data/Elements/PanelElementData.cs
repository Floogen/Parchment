using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PanelElementData : ElementData, ISprite, ILayeredContainer
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
        /// Elements drawn behind <see cref="Children"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the panel's size.
        /// They are anchored to the panel's content area, so the panel's border and <see cref="Padding"/> inset them exactly as they inset a child.
        /// An element here is only reachable by the cursor when it has something to offer, such as <see cref="ElementData.Description"/>, <see cref="ElementData.DisplayName"/> or <see cref="ElementData.Action"/> / <see cref="ElementData.Actions"/>.
        /// </summary>
        public List<ElementData>? Background { get; set; }

        /// <summary>
        /// Elements drawn over <see cref="Children"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the panel's size.
        /// They are anchored to the panel's content area, so the panel's border and <see cref="Padding"/> inset them exactly as they inset a child.
        /// An element here is only reachable by the cursor when it has something to offer, such as <see cref="ElementData.Description"/>, <see cref="ElementData.DisplayName"/> or <see cref="ElementData.Action"/> / <see cref="ElementData.Actions"/>.
        /// </summary>
        public List<ElementData>? Foreground { get; set; }

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

            var childrenIsValidData = ElementValidationHelper.ValidateElements(Children);
            if (childrenIsValidData.Result is false)
            {
                return (false, $"[Children] {childrenIsValidData.Error}");
            }

            var backgroundIsValidData = ElementValidationHelper.ValidateElements(Background);
            if (backgroundIsValidData.Result is false)
            {
                return (false, $"[Background] {backgroundIsValidData.Error}");
            }

            var foregroundIsValidData = ElementValidationHelper.ValidateElements(Foreground);
            if (foregroundIsValidData.Result is false)
            {
                return (false, $"[Foreground] {foregroundIsValidData.Error}");
            }

            return base.IsValid();
        }
    }
}
