using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data.Animations;
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
    public class ImageElementData : ElementData, ISprite, ITextContent
    {
        public override ElementType Type => ElementType.Image;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        /// <summary>An Image is one sprite drawn as a single quad, with nothing inside it measured against its size, so a frame can pulse it with <see cref="AnimationFrameData.Scale"/>.
        /// Every sprite element can move around its sheet with <see cref="AnimationFrameData.SourcePoint"/>, which needs no such thing since it leaves the size alone.
        /// </summary>
        public override bool SupportsFrameScale => true;

        /// <summary>A qualified item ID (such as "(O)24" for Parsnip) whose sprite is drawn. When set, this ignores <see cref="TexturePath"/> and <see cref="TextureSourceRectangle"/>.</summary>
        public string? ItemId { get; set; }

        public FontType FontType { get; set; }
        public string? Text { get; set; }
        public string? TextColor { get; set; }

        /// <summary>The color of the drop shadow drawn behind the text, whose own alpha decides how strongly it comes through.
        /// Left unset, the game's shadow color is used and follows <see cref="TextColor"/>'s alpha instead. Ignored when the font is SpriteText, which draws its own outline.
        /// </summary>
        public string? ShadowColor { get; set; }

        public float TextScale { get; set; } = 1f;
        public AlignmentType TextAlignment { get; set; } = AlignmentType.Center;

        /// <summary>
        /// The area within <see cref="TextureSourceRectangle"/> that text is drawn into, in unscaled sprite pixels relative to the source rectangle's top-left. When null, text uses the whole sprite.
        /// </summary>
        public Rectangle? TextArea { get; set; }

        /// <summary>
        /// The rotation applied to the Texture (does not effect Text)
        /// </summary>
        public float Rotation { get; set; } = 0f;

        /// <summary>
        /// The pivot point the sprite rotates and scales around, in unscaled source-texture pixels relative to the source rectangle's top-left. (does not effect Text)
        /// This only changes what the sprite turns and grows about, never where it rests, so an unrotated sprite at its own scale draws in the same place whatever this is set to.
        /// </summary>
        public Vector2 Origin { get; set; } = Vector2.Zero;

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(TexturePath) && ItemId is null)
            {
                return (false, $"\"TexturePath\" is required!");
            }

            if (TextureSourceRectangle is Rectangle sourceRectangle && (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0) && ItemId is null)
            {
                return (false, $"\"TextureSourceRectangle\" must have a positive width and height!");
            }

            if (TextArea is Rectangle textArea)
            {
                if (textArea.Width <= 0 || textArea.Height <= 0)
                {
                    return (false, $"\"TextArea\" must have a positive width and height.");
                }

                if (TextureSourceRectangle is Rectangle sourceRectangleText && (textArea.Right > sourceRectangleText.Width || textArea.Bottom > sourceRectangleText.Height))
                {
                    return (false, $"\"TextArea\" extends outside the {sourceRectangleText.Width}x{sourceRectangleText.Height} \"TextureSourceRectangle\"!");
                }
            }

            var frameSizeResult = ValidateFrameSize(Frames, nameof(Frames));
            if (frameSizeResult.Result is false)
            {
                return frameSizeResult;
            }

            var hoverFrameSizeResult = ValidateFrameSize(HoverFrames, nameof(HoverFrames));
            if (hoverFrameSizeResult.Result is false)
            {
                return hoverFrameSizeResult;
            }

            if (string.IsNullOrWhiteSpace(TexturePath) && string.IsNullOrWhiteSpace(ItemId))
            {
                return (false, $"Either \"TexturePath\" or \"ItemId\" must be given!");
            }

            return base.IsValid();
        }

        /// <summary>Checks that a frame list has a size to measure against. The rest of what a frame carries is checked by the base, which every element shares.
        /// This is the Image's own requirement, since it is the element that reads a frame's source point out of a sheet.
        /// </summary>
        private (bool Result, string Error) ValidateFrameSize(List<AnimationFrameData>? frames, string fieldName)
        {
            if (frames is null || frames.Count is 0)
            {
                return (true, string.Empty);
            }

            // An item brings its own sprite rectangle from the item registry, so it stands in for TextureSourceRectangle as the layout size
            if (TextureSourceRectangle is not Rectangle && string.IsNullOrWhiteSpace(ItemId))
            {
                return (false, $"\"TextureSourceRectangle\" is required when \"{fieldName}\" is set, since it defines the layout size.");
            }

            return (true, string.Empty);
        }
    }
}
