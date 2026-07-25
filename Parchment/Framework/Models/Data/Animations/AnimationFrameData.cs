using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Animations
{
    public class AnimationFrameData
    {
        /// <summary>The source point for this frame. Automatically matches the dimensions of the element's <see cref="ISprite.TextureSourceRectangle"/>.</summary>
        public Point SourcePoint { get; set; }

        /// <summary>How long this frame is shown, in milliseconds. When null, the element's <see cref="ImageElementData.FrameDuration"/> is used.</summary>
        public float? Duration { get; set; }

        /// <summary>A game state query determining whether this frame plays. When null, the frame always plays. Checked periodically while the book is open, on the same interval as element conditions. Frames whose condition fails are skipped, which shortens the animation cycle rather than pausing on them.
        /// When every frame's condition fails, the element falls back to drawing <see cref="ISprite.TextureSourceRectangle"/> statically.</summary>
        public string? Condition { get; set; }
    }
}
