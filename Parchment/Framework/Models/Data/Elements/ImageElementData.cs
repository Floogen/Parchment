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

        /// <summary>The animation frames. When null or empty, the element draws <see cref="TextureSourceRectangle"/> statically.
        /// Frames take their size from <see cref="TextureSourceRectangle"/>, or from the item's own sprite when <see cref="ItemId"/> is used, in which case a frame is expected to leave <see cref="AnimationFrameData.SourcePoint"/> unset and vary only its duration, scale or condition.
        /// </summary>
        public List<AnimationFrameData>? Frames { get; set; }

        /// <summary>The animation frames played while the cursor is over the element, replacing <see cref="Frames"/> for as long as it stays there. Frames take their size from <see cref="TextureSourceRectangle"/> the same way, so this changes what is drawn rather than the element's layout.
        /// When null, empty or fully conditioned out, the element carries on with <see cref="Frames"/> rather than going still, so a hover animation can drop away without interrupting the idle one.
        /// </summary>
        public List<AnimationFrameData>? HoverFrames { get; set; }

        /// <summary>The default duration for frames that don't specify one, in milliseconds.</summary>
        public float FrameDuration { get; set; } = 100f;

        /// <summary>A qualified item ID (such as "(O)24" for Parsnip) whose sprite is drawn. When set, this ignores <see cref="TexturePath"/> and <see cref="TextureSourceRectangle"/>.</summary>
        public string? ItemId { get; set; }

        public FontType FontType { get; set; }
        public string? Text { get; set; }
        public string? TextColor { get; set; }
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

            var frameResult = ValidateFrames(Frames, nameof(Frames));
            if (frameResult.Result is false)
            {
                return frameResult;
            }

            var hoverFrameResult = ValidateFrames(HoverFrames, nameof(HoverFrames));
            if (hoverFrameResult.Result is false)
            {
                return hoverFrameResult;
            }

            if (FrameDuration <= 0f)
            {
                return (false, $"\"FrameDuration\" must be positive.");
            }

            if (string.IsNullOrWhiteSpace(TexturePath) && string.IsNullOrWhiteSpace(ItemId))
            {
                return (false, $"Either \"TexturePath\" or \"ItemId\" must be given!");
            }

            return base.IsValid();
        }

        /// <summary>Validates one frame list. Both <see cref="Frames"/> and <see cref="HoverFrames"/> are measured against <see cref="TextureSourceRectangle"/>, or against the item's sprite when <see cref="ItemId"/> is used, so they carry the same requirements.</summary>
        private (bool Result, string Error) ValidateFrames(List<AnimationFrameData>? frames, string fieldName)
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

            foreach (AnimationFrameData frame in frames)
            {
                if (frame.Duration is float duration && duration <= 0f)
                {
                    return (false, $"A frame in \"{fieldName}\" has a non-positive \"frame.Duration\"");
                }

                if (frame.Scale <= 0f)
                {
                    return (false, $"A frame in \"{fieldName}\" has a non-positive \"frame.Scale\"");
                }
            }

            return (true, string.Empty);
        }
    }
}
