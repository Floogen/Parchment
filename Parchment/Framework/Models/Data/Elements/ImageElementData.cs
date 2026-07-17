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

        /// <summary>The animation frames. When null or empty, the element draws <see cref="TextureSourceRectangle"/> statically.</summary>
        public List<AnimationFrameData>? Frames { get; set; }

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

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"\"TexturePath\" is required!");
            }

            if (TextureSourceRectangle is Rectangle sourceRectangle && (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0))
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

            if (Frames is not null && Frames.Count > 0)
            {
                if (TextureSourceRectangle is not Rectangle sourceRectangleFrames)
                {
                    return (false, $"\"TextureSourceRectangle\" is required when \"Frames\" is set, since it defines the layout size.");
                }

                foreach (AnimationFrameData frame in Frames)
                {
                    if (frame.SourceRectangle.Width != sourceRectangleFrames.Width || frame.SourceRectangle.Height != sourceRectangleFrames.Height)
                    {
                        return (false, $"Every frame in \"Frames\" must be {sourceRectangleFrames.Width}x{sourceRectangleFrames.Height} to match \"TextureSourceRectangle\"!");
                    }

                    if (frame.Duration is float duration && duration <= 0f)
                    {
                        return (false, $"A frame in \"Frames\" has a non-positive \"frame.Duration\"");
                    }
                }
            }

            if (FrameDuration <= 0f)
            {
                return (false, $"\"FrameDuration\" must be positive.");
            }

            if (string.IsNullOrWhiteSpace(TexturePath) && string.IsNullOrWhiteSpace(ItemId))
            {
                return (false, $"Either \"TexturePath\" or \"ItemId\" must be given!");
            }

            return (true, string.Empty);
        }
    }
}
