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
        /// <summary>The source rectangle for this frame. Must match the dimensions of the element's <see cref="ISprite.TextureSourceRectangle"/>.</summary>
        public Rectangle SourceRectangle { get; set; }

        /// <summary>How long this frame is shown, in milliseconds. When null, the element's <see cref="ImageElementData.FrameDuration"/> is used.</summary>
        public float? Duration { get; set; }
    }
}
