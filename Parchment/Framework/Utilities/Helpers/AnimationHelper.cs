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

        /// <summary>Gets the frame that should be drawn right now, or null when there is nothing to play and the element should fall back to its own source rectangle.
        /// Callers that need both the rectangle and the frame's scale should hold onto this.
        /// </summary>
        public static AnimationFrameData? GetActiveFrame(List<AnimationFrameData>? frames, float defaultFrameDuration)
        {
            if (frames is null || frames.Count is 0 || Game1.currentGameTime is null)
            {
                return null;
            }

            if (frames.Count is 1)
            {
                return frames[0];
            }

            float cycleDuration = 0f;

            foreach (AnimationFrameData frame in frames)
            {
                cycleDuration += frame.Duration ?? defaultFrameDuration;
            }

            if (cycleDuration <= 0f)
            {
                return frames[0];
            }

            float cyclePosition = (float)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % cycleDuration);

            foreach (AnimationFrameData frame in frames)
            {
                float frameDuration = frame.Duration ?? defaultFrameDuration;

                if (cyclePosition < frameDuration)
                {
                    return frame;
                }

                cyclePosition -= frameDuration;
            }

            return frames[frames.Count - 1];
        }

        /// <summary>Gets the source rectangle for a frame, which takes its size from the element's own source rectangle so every frame measures the same. A null frame draws that rectangle unchanged.</summary>
        public static Rectangle GetFrameRectangle(Rectangle sourceRectangle, AnimationFrameData? frame)
        {
            if (frame is null)
            {
                return sourceRectangle;
            }

            return new Rectangle(frame.SourcePoint.X, frame.SourcePoint.Y, sourceRectangle.Width, sourceRectangle.Height);
        }

        /// <summary>Gets the multiplier a frame applies on top of the element's own scale. A null frame draws at the element's scale.</summary>
        public static float GetFrameScale(AnimationFrameData? frame)
        {
            return frame?.Scale ?? 1f;
        }

        /// <summary>Gets the source rectangle to draw. When no frames are given, or every frame was filtered out by its condition, the element's own source rectangle is used.
        /// This ignores <see cref="AnimationFrameData.Scale"/>, so a caller that draws the result should use <see cref="GetActiveFrame"/> instead.
        /// </summary>
        public static Rectangle GetFrame(Rectangle sourceRectangle, List<AnimationFrameData>? frames, float defaultFrameDuration)
        {
            return GetFrameRectangle(sourceRectangle, GetActiveFrame(frames, defaultFrameDuration));
        }
    }
}
