using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Books
{
    public class BookAppearanceData : BaseModel
    {
        /// <summary>The book sprite sheet. Frames are laid out horizontally, each <see cref="FrameWidth"/> x <see cref="FrameHeight"/>: first the open frames (index 0 is fully closed, the last is fully open),
        /// then the page-turn frames, which a book with a <see cref="TurnFrameCount"/> of 0 has none of. The close animation is the open frames reversed.
        /// </summary>
        public string TexturePath { get; set; } = "Assets/PeacefulEnd.Parchment/smallBook";

        /// <summary>An optional grayscale layer drawn beneath <see cref="TexturePath"/> and tinted by <see cref="TintColor"/>. When null, the book is drawn untinted.</summary>
        public string? GrayscaleTexturePath { get; set; } = "Assets/PeacefulEnd.Parchment/smallBookGrayscale";

        /// <summary>The tint applied to the book sprite. Defaults to white / untinted.</summary>
        public string? TintColor { get; set; }

        public int FrameWidth { get; set; } = 219;
        public int FrameHeight { get; set; } = 158;

        /// <summary>How many open frames the sheet starts with. At least 1, since frame 0 is the book itself rather than a step in an animation:
        /// it is what the book is drawn as while it sits open or shut on its cover, and what its size on screen is measured from.
        /// </summary>
        public int OpenFrameCount { get; set; } = 4;

        /// <summary>How many page-turn frames follow the open frames. 0 for a book with no turn animation, which lands on the target spread at once rather than playing anything over the book.</summary>
        public int TurnFrameCount { get; set; } = 6;

        /// <summary>A nudge applied to the book's centered position, in unscaled sprite pixels. Use this when the frame has empty space around the art and frame-centering doesn't look centered.</summary>
        public Point Offset { get; set; } = Point.Zero;

        public float Scale { get; set; } = 5f;

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"{nameof(TexturePath)} is required.");
            }

            if (FrameWidth <= 0 || FrameHeight <= 0)
            {
                return (false, $"{nameof(FrameWidth)} and {nameof(FrameHeight)} must be positive.");
            }

            if (OpenFrameCount < 1)
            {
                return (false, $"{nameof(OpenFrameCount)} must be at least 1, as frame 0 is the book itself rather than a step in an animation. Use 1 for a book that doesn't animate open.");
            }

            if (TurnFrameCount < 0)
            {
                return (false, $"{nameof(TurnFrameCount)} cannot be negative. Use 0 for a book with no turn animation.");
            }

            if (Scale <= 0f)
            {
                return (false, $"{nameof(Scale)} must be positive.");
            }

            return (true, string.Empty);
        }
    }
}
