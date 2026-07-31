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
        /// <summary>The source point for this frame. Automatically matches the dimensions of the element's <see cref="ISprite.TextureSourceRectangle"/>, or of the item's sprite when <see cref="ImageElementData.ItemId"/> is used.
        /// When null, the frame draws whatever the element already draws, which lets a frame vary only its <see cref="Duration"/>, <see cref="Scale"/> or <see cref="Condition"/>. This is the only usable form alongside <see cref="ImageElementData.ItemId"/>, where the sprite's place in the sheet isn't the author's to know.
        /// </summary>
        public Point? SourcePoint { get; set; }

        /// <summary>How long this frame is shown, in milliseconds. When null, the element's <see cref="ImageElementData.FrameDuration"/> is used.</summary>
        public float? Duration { get; set; }

        /// <summary>A multiplier applied on top of the element's <see cref="ElementData.Scale"/> while this frame is drawn. 1 draws the frame at the element's own scale.
        /// This is a draw-time effect only. The element is measured once from <see cref="ISprite.TextureSourceRectangle"/> at the element's scale, so a frame above 1 overhangs its own bounds and its own hitbox rather than pushing anything aside.
        /// Like <see cref="ImageElementData.Rotation"/> it pivots around <see cref="ImageElementData.Origin"/> and leaves any text unscaled.
        /// </summary>
        public float Scale { get; set; } = 1f;

        /// <summary>How far this frame is shifted from where the element was laid out, in unscaled sprite pixels multiplied by the element's <see cref="ElementData.Scale"/>. Positive values move right and down.
        /// A draw-time effect only, like <see cref="Scale"/>: the element keeps the space and the hitbox it was measured with, so a moving frame slides over its own bounds rather than pushing anything aside or dragging its clickable area with it.
        /// Measured against the element's own scale rather than this frame's, so a frame that offsets and scales at once doesn't drift. Unlike <see cref="Scale"/> this carries any text along with the sprite.
        /// </summary>
        public Point? Offset { get; set; }

        /// <summary>A trigger action to run when this frame starts. Shorthand for a single-entry <see cref="Actions"/>, and when both are given this one runs first.</summary>
        public string? Action { get; set; }

        /// <summary>The trigger actions to run, in order, each time this frame starts. Combined with <see cref="Action"/> rather than replacing it.
        /// These run on every pass through the frame, so a looping animation runs them again on each cycle. Keep the whole list harmless to repeat, or condition the frames so the loop stops.
        /// </summary>
        public List<string>? Actions { get; set; }

        /// <summary>Whether this frame has at least one action, from either <see cref="Action"/> or <see cref="Actions"/>.</summary>
        internal bool HasActions => string.IsNullOrWhiteSpace(Action) is false || (Actions is not null && Actions.Any(action => string.IsNullOrWhiteSpace(action) is false));

        /// <summary>Every action on this frame, <see cref="Action"/> first and then <see cref="Actions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetActions()
        {
            if (string.IsNullOrWhiteSpace(Action) is false)
            {
                yield return Action;
            }

            if (Actions is null)
            {
                yield break;
            }

            foreach (string action in Actions)
            {
                if (string.IsNullOrWhiteSpace(action) is false)
                {
                    yield return action;
                }
            }
        }

        /// <summary>A game state query determining whether this frame plays. When null, the frame always plays. Checked periodically while the book is open, on the same interval as element conditions. Frames whose condition fails are skipped, which shortens the animation cycle rather than pausing on them.
        /// When every frame's condition fails, the element falls back to drawing <see cref="ISprite.TextureSourceRectangle"/> statically.</summary>
        public string? Condition { get; set; }
    }
}
