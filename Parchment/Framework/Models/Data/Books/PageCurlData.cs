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
