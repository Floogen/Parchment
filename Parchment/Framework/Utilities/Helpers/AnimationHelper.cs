using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
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
        /// <summary>Rebuilds an element's active frames from its per-frame conditions, and reports whether the active set changed.</summary>
        public static bool RefreshActiveFrames(Element element)
        {
            if (element.Data is not ImageElementData imageData || imageData.Frames is null || imageData.Frames.Count is 0)
            {
                if (element.ActiveFrames is null)
                {
                    return false;
                }

                element.ActiveFrames = null;

                return true;
            }

            // Frames without conditions never change, so the original list is reused rather than copied every refresh
            if (HasConditionalFrames(imageData.Frames) is false)
            {
                if (ReferenceEquals(element.ActiveFrames, imageData.Frames) is true)
                {
                    return false;
                }

                element.ActiveFrames = imageData.Frames;

                return true;
            }

            var activeFrames = new List<AnimationFrameData>();
            foreach (AnimationFrameData frame in imageData.Frames)
            {
                if (string.IsNullOrWhiteSpace(frame.Condition) is false && GameStateQuery.CheckConditions(frame.Condition) is false)
                {
                    continue;
                }

                activeFrames.Add(frame);
            }

            if (HasSameFrames(element.ActiveFrames, activeFrames) is true)
            {
                return false;
            }

            element.ActiveFrames = activeFrames;

            return true;
        }

        private static bool HasConditionalFrames(List<AnimationFrameData> frames)
        {
            foreach (AnimationFrameData frame in frames)
            {
                if (string.IsNullOrWhiteSpace(frame.Condition) is false)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSameFrames(List<AnimationFrameData>? currentFrames, List<AnimationFrameData> updatedFrames)
        {
            if (currentFrames is null || currentFrames.Count != updatedFrames.Count)
            {
                return false;
            }

            for (int index = 0; index < currentFrames.Count; index++)
            {
                if (ReferenceEquals(currentFrames[index], updatedFrames[index]) is false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Gets the source rectangle to draw. When no frames are given, or every frame was filtered out by its condition, the element's own source rectangle is used.</summary>
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
