using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Animations;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class AnimationHelper
    {
        public static Rectangle GetFrame(Rectangle sourceRectangle, List<AnimationFrameData>? frames, float defaultFrameDuration)
        {
            if (frames is null || frames.Count is 0 || Game1.currentGameTime is null)
            {
                return sourceRectangle;
            }

            if (frames.Count is 1)
            {
                return frames[0].SourceRectangle;
            }

            float cycleDuration = 0f;

            foreach (AnimationFrameData frame in frames)
            {
                cycleDuration += frame.Duration ?? defaultFrameDuration;
            }

            if (cycleDuration <= 0f)
            {
                return frames[0].SourceRectangle;
            }

            float cyclePosition = (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % cycleDuration);

            foreach (AnimationFrameData frame in frames)
            {
                float frameDuration = frame.Duration ?? defaultFrameDuration;

                if (cyclePosition < frameDuration)
                {
                    return frame.SourceRectangle;
                }

                cyclePosition -= frameDuration;
            }

            return frames[frames.Count - 1].SourceRectangle;
        }
    }
}
