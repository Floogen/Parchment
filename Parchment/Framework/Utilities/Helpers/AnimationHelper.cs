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
                var frame = frames[0];
                return new Rectangle(frame.SourcePoint.X, frame.SourcePoint.Y, sourceRectangle.Width, sourceRectangle.Height);
            }

            float cycleDuration = 0f;

            foreach (AnimationFrameData frame in frames)
            {
                cycleDuration += frame.Duration ?? defaultFrameDuration;
            }

            if (cycleDuration <= 0f)
            {
                var frame = frames[0];
                return new Rectangle(frame.SourcePoint.X, frame.SourcePoint.Y, sourceRectangle.Width, sourceRectangle.Height);
            }

            float cyclePosition = (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % cycleDuration);

            foreach (AnimationFrameData frame in frames)
            {
                float frameDuration = frame.Duration ?? defaultFrameDuration;

                if (cyclePosition < frameDuration)
                {
                    return new Rectangle(frame.SourcePoint.X, frame.SourcePoint.Y, sourceRectangle.Width, sourceRectangle.Height);
                }

                cyclePosition -= frameDuration;
            }

            return new Rectangle(frames[frames.Count - 1].SourcePoint.X, frames[frames.Count - 1].SourcePoint.Y, sourceRectangle.Width, sourceRectangle.Height);
        }
    }
}
