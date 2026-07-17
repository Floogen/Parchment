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
    public class ImageElementData : ElementData, ISprite, ITextContent
    {
        public override ElementType Type => ElementType.Image;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

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
                return (false, $"{nameof(TexturePath)} is required!");
            }

            if (TextureSourceRectangle is Rectangle sourceRectangle && (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0))
            {
                return (false, $"{nameof(TextureSourceRectangle)} must have a positive width and height!");
            }

            if (TextArea is Rectangle textArea)
            {
                if (textArea.Width <= 0 || textArea.Height <= 0)
                {
                    return (false, $"{nameof(TextArea)} must have a positive width and height.");
                }

                if (TextureSourceRectangle is Rectangle sourceRectangleAlt && (textArea.Right > sourceRectangleAlt.Width || textArea.Bottom > sourceRectangleAlt.Height))
                {
                    return (false, $"{nameof(TextArea)} extends outside the {sourceRectangleAlt.Width}x{sourceRectangleAlt.Height} {nameof(TextureSourceRectangle)}.");
                }
            }

            return (true, string.Empty);
        }
    }
}
