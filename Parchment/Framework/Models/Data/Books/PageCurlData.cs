using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Books
{
    public class PageCurlData : BaseModel
    {
        /// <summary>Whether the book has curl corners at all. When false, neither corner is drawn or clickable, and the rest of this model is ignored.
        /// The corners are the only page turning Parchment offers on its own, so a book without them needs a button or a key bind running PeacefulEnd.Parchment_NextPage to be readable.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        public string TexturePath { get; set; } = "Assets/PeacefulEnd.Parchment/curlPage";

        public int FrameWidth { get; set; } = 32;
        public int FrameHeight { get; set; } = 32;
        public int FrameCount { get; set; } = 7;
        public float Scale { get; set; } = 5f;

        /// <summary>The top-left of the back-turn corner, in unscaled sprite pixels relative to the book frame's top-left. The corner's size is <see cref="FrameWidth"/> x <see cref="FrameHeight"/> multiplied by <see cref="Scale"/>, and this rect is both the drawn sprite and its hotspot.</summary>
        public Point PreviousPageOffset { get; set; } = new Point(1, 112);

        /// <summary>The top-left of the forward-turn corner, in unscaled sprite pixels relative to the book frame's top-left. The corner's size is <see cref="FrameWidth"/> x <see cref="FrameHeight"/> multiplied by <see cref="Scale"/>, and this rect is both the drawn sprite and its hotspot.</summary>
        public Point NextPageOffset { get; set; } = new Point(186, 112);

        public override (bool Result, string Error) IsValid()
        {
            // Nothing below is read once the corners are off, so a book turning them off isn't held to values it will never draw with
            if (IsEnabled is false)
            {
                return (true, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(TexturePath))
            {
                return (false, $"{nameof(TexturePath)} is required.");
            }

            if (FrameWidth <= 0 || FrameHeight <= 0)
            {
                return (false, $"{nameof(FrameWidth)} and {nameof(FrameHeight)} must be positive.");
            }

            if (FrameCount < 1)
            {
                return (false, $"{nameof(FrameCount)} must be at least 1.");
            }

            if (Scale <= 0f)
            {
                return (false, $"{nameof(Scale)} must be positive.");
            }

            return (true, string.Empty);
        }
    }
}
