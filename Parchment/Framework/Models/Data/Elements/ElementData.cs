using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Elements
{
    [Newtonsoft.Json.JsonConverter(typeof(ElementJsonConverter))]
    public abstract class ElementData : BaseModel
    {
        public string? Id { get; set; }
        /// <summary>
        /// If omitted and <see cref="ImageElementData.ItemId"/> is given, this will be automatically populated with the item's name.
        /// To override these behavior, simply set Description to an empty string (not null / not omitted)
        /// </summary>
        public string? DisplayName { get; set; }
        /// <summary>
        /// If omitted and <see cref="ImageElementData.ItemId"/> is given, this will be automatically populated with the item's description.
        /// To override these behavior, simply set Description to an empty string (not null / not omitted)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>Whether the game's [Token] tokenizable strings are resolved in this element's text, alongside Parchment's own %Token% forms.
        /// Set it to false when the text carries square brackets meant as ordinary punctuation, which the game would otherwise try to read as a token.
        /// </summary>
        public bool ParseTokenizableStrings { get; set; } = true;

        public abstract ElementType Type { get; }

        /// <summary>Where the element sits within the width available to it, and where each line of its text sits within the element.
        /// Only has an effect when the element is narrower than the space available. In a placed list (a page's Background or Foreground, a book's Underlay or Overlay) this anchors the element
        /// and <see cref="Position"/> is then an offset from that anchor.
        /// </summary>
        public AlignmentType Alignment { get; set; } = AlignmentType.Left;

        /// <summary>Where the element sits within the height available to it, anchoring it before <see cref="Position"/> offsets it from there.
        /// Only used in a placed list (a page's Background or Foreground, a book's Underlay or Overlay), as a stacked element takes its vertical position from the elements above it.
        /// </summary>
        public VerticalAlignmentType VerticalAlignment { get; set; } = VerticalAlignmentType.Top;

        /// <summary>
        /// Optional. If given, increases buffer between elements.
        /// </summary>
        public virtual int SpacingAfter { get; set; } = 8;
        public int MarginLeft { get; set; } = 0;
        public int MarginRight { get; set; } = 0;

        public float Scale { get; set; } = 1f;

        /// <summary>
        /// The page-local position in screen pixels, relative to the page's content area. Unlike <see cref="SpacingAfter"/> and other spacing fields, this is not multiplied by <see cref="Scale"/>.
        /// Changing an element's scale resizes it in place rather than moving it.
        /// Measured from wherever <see cref="Alignment"/> and <see cref="VerticalAlignment"/> anchor the element, so it is an absolute coordinate under the default Left and Top and an offset from the anchor otherwise.
        /// </summary>
        public Point Position { get; set; } = Point.Zero;

        /// <summary>A trigger action to run when this element is clicked. Shorthand for a single-entry <see cref="Actions"/>, and when both are given this one runs first.
        /// When neither is set, the element is not interactive.
        /// </summary>
        public string? Action { get; set; }

        /// <summary>The trigger actions to run, in order, when this element is clicked. Combined with <see cref="Action"/> rather than replacing it.</summary>
        public List<string>? Actions { get; set; }

        /// <summary>A trigger action to run when the cursor moves onto this element. It runs once on entry rather than repeatedly while the cursor rests there, though moving away and back runs it again.
        /// Gate it with <see cref="Condition"/> when it should only happen once, since a hidden element can't be hovered.
        /// Shorthand for a single-entry <see cref="HoverActions"/>, and when both are given this one runs first.
        /// </summary>
        public string? HoverAction { get; set; }

        /// <summary>The trigger actions to run, in order, when the cursor moves onto this element. Combined with <see cref="HoverAction"/> rather than replacing it.</summary>
        public List<string>? HoverActions { get; set; }

        /// <summary>The sound to play when this element is clicked. Only used when <see cref="Action"/> or <see cref="Actions"/> is set, and played once regardless of how many actions run.</summary>
        public string? Sound { get; set; } = "bigSelect";

        /// <summary>A game state query determining whether this element appears. When null, the element always appears. Checked periodically while the book is open.</summary>
        public string? Condition { get; set; }

        /// <summary>How long the element stays up once shown, in seconds. An element with this set starts hidden and only appears when a ShowElement action names it, so it comes and goes rather than being part of the page.
        /// Showing it again while it's still up restarts the count rather than stacking another one on.
        /// </summary>
        public float? Lifetime { get; set; }

        /// <summary>How long the element holds at full strength before it starts fading, in seconds, reaching nothing at the end of its <see cref="Lifetime"/>.
        /// Left out, it holds for the whole lifetime and then goes at once.
        /// </summary>
        public float? FadeAfter { get; set; }

        /// <summary>Whether the cursor passes straight through this element to whatever sits beneath it, leaving it unhoverable and unclickable.
        /// It doesn't carry down, so a decorative container can let the cursor through while its children and its own layers stay reachable.
        /// This is what makes an element transparent in a list that is hit-tested whatever it holds, such as <see cref="PageData.Elements"/> or a book's Underlay and Overlay.
        /// </summary>
        public bool IgnoreCursor { get; set; } = false;

        /// <summary>The animation frames. When null or empty, the element draws as it was laid out.
        /// Every element understands these, though only the ones that draw a sprite of their own act on a frame's <see cref="AnimationFrameData.SourcePoint"/> and <see cref="AnimationFrameData.Scale"/>.
        /// Everywhere else a frame is a way to move the element with <see cref="AnimationFrameData.Offset"/>, to time something with <see cref="AnimationFrameData.Actions"/>, or both.
        /// </summary>
        public List<AnimationFrameData>? Frames { get; set; }

        /// <summary>The animation frames played while the cursor is over the element, replacing <see cref="Frames"/> for as long as it stays there.
        /// When null, empty or fully conditioned out, the element carries on with <see cref="Frames"/> rather than going still, so a hover animation can drop away without interrupting the idle one.
        /// </summary>
        public List<AnimationFrameData>? HoverFrames { get; set; }

        /// <summary>The default duration for frames that don't specify one, in milliseconds.</summary>
        public float FrameDuration { get; set; } = 100f;

        /// <summary>Whether a frame's <see cref="AnimationFrameData.Scale"/> means anything on this element, which needs a sprite drawn as a single quad it can grow without anything inside it coming loose.
        /// False everywhere else, including the other sprite types, where the element is nine-sliced or holds children measured against a size a frame has no way to change.
        /// </summary>
        public virtual bool SupportsFrameScale => false;

        /// <summary>Whether this element claims the cursor whatever else it carries. Overridden by types that must be clickable to work at all, such as Input.</summary>
        public virtual bool IsAlwaysInteractive => false;

        /// <summary>Whether this element has at least one click action, from either <see cref="Action"/> or <see cref="Actions"/>.</summary>
        internal bool HasActions => HasAny(Action, Actions);

        /// <summary>Whether this element has at least one hover action, from either <see cref="HoverAction"/> or <see cref="HoverActions"/>.</summary>
        internal bool HasHoverActions => HasAny(HoverAction, HoverActions);

        /// <summary>Every click action on this element, <see cref="Action"/> first and then <see cref="Actions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetActions() => Combine(Action, Actions);

        /// <summary>Every hover action on this element, <see cref="HoverAction"/> first and then <see cref="HoverActions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetHoverActions() => Combine(HoverAction, HoverActions);

        protected static bool HasAny(string? single, List<string>? many) => string.IsNullOrWhiteSpace(single) is false || (many is not null && many.Any(action => string.IsNullOrWhiteSpace(action) is false));

        /// <summary>Yields the single field followed by the list, skipping empty entries.
        /// This is composed on each call rather than merged into the list at load, as the deserialized instance is shared and merging would accumulate duplicates across asset reloads.
        /// </summary>
        protected static IEnumerable<string> Combine(string? single, List<string>? many)
        {
            if (string.IsNullOrWhiteSpace(single) is false)
            {
                yield return single;
            }

            if (many is null)
            {
                yield break;
            }

            foreach (string action in many)
            {
                if (string.IsNullOrWhiteSpace(action) is false)
                {
                    yield return action;
                }
            }
        }

        public override (bool Result, string Error) IsValid()
        {
            if (Actions is not null && Actions.Any(string.IsNullOrWhiteSpace))
            {
                return (false, $"\"Actions\" contains an empty entry.");
            }

            if (HoverActions is not null && HoverActions.Any(string.IsNullOrWhiteSpace))
            {
                return (false, $"\"HoverActions\" contains an empty entry.");
            }

            if (Scale <= 0f)
            {
                return (false, $"\"Scale\" must be positive.");
            }

            if (MarginLeft < 0 || MarginRight < 0)
            {
                return (false, $"\"MarginLeft\" and \"MarginRight\" cannot be negative.");
            }

            if (SpacingAfter < 0)
            {
                return (false, $"\"SpacingAfter\" cannot be negative.");
            }

            if (Lifetime is float lifetime && lifetime <= 0f)
            {
                return (false, $"\"Lifetime\" must be above zero, as an element that expires the moment it appears would never be seen.");
            }

            if (FadeAfter is float fadeAfter)
            {
                if (Lifetime is not float fadeLifetime)
                {
                    return (false, $"\"FadeAfter\" needs a \"Lifetime\" to fade towards.");
                }

                if (fadeAfter < 0f)
                {
                    return (false, $"\"FadeAfter\" cannot be negative.");
                }

                if (fadeAfter >= fadeLifetime)
                {
                    return (false, $"\"FadeAfter\" of {fadeAfter} must be below \"Lifetime\" of {fadeLifetime}, or the fade would have no time to run.");
                }
            }

            if (Lifetime is not null && string.IsNullOrWhiteSpace(Id) is true)
            {
                return (false, $"\"Lifetime\" needs an \"Id\" (set through WithId in code), as a ShowElement action has no other way to name the element.");
            }

            if (IgnoreCursor && IsAlwaysInteractive)
            {
                return (false, $"\"IgnoreCursor\" cannot be used on a {Type} element, which has to be clickable to work at all.");
            }

            if (IgnoreCursor && (HasActions || HasHoverActions))
            {
                return (false, $"\"IgnoreCursor\" cannot be combined with \"Action\", \"Actions\", \"HoverAction\" or \"HoverActions\", as the cursor never reaches the element to run them.");
            }

            var frameResult = ValidateFrames(Frames, nameof(Frames));
            if (frameResult.Result is false)
            {
                return frameResult;
            }

            var hoverFrameResult = ValidateFrames(HoverFrames, nameof(HoverFrames));
            if (hoverFrameResult.Result is false)
            {
                return hoverFrameResult;
            }

            if (FrameDuration <= 0f)
            {
                return (false, $"\"FrameDuration\" must be positive.");
            }

            return (true, string.Empty);
        }

        /// <summary>Validates one frame list against what this element can act on. The sprite fields are rejected rather than ignored on an element that has no sprite to move around a sheet,
        /// so a frame that would quietly do nothing fails at load instead.
        /// </summary>
        protected (bool Result, string Error) ValidateFrames(List<AnimationFrameData>? frames, string fieldName)
        {
            if (frames is null || frames.Count is 0)
            {
                return (true, string.Empty);
            }

            foreach (AnimationFrameData frame in frames)
            {
                if (frame.Duration is float duration && duration <= 0f)
                {
                    return (false, $"A frame in \"{fieldName}\" has a non-positive \"frame.Duration\"");
                }

                if (frame.Scale <= 0f)
                {
                    return (false, $"A frame in \"{fieldName}\" has a non-positive \"frame.Scale\"");
                }

                if (frame.Actions is not null && frame.Actions.Any(string.IsNullOrWhiteSpace))
                {
                    return (false, $"A frame in \"{fieldName}\" has an empty entry in \"frame.Actions\"");
                }

                if (frame.SourcePoint is not null && this is not ISprite)
                {
                    return (false, $"A frame in \"{fieldName}\" sets \"frame.SourcePoint\", which a {Type} element has no sprite sheet to read it from. Use \"frame.Offset\" to move the element instead.");
                }

                if (frame.Scale is not 1f && SupportsFrameScale is false)
                {
                    return (false, $"A frame in \"{fieldName}\" sets \"frame.Scale\", which a {Type} element cannot apply, as it is drawn from a size its own layout settled on rather than as one sprite. Use \"frame.SourcePoint\" to swap the art instead.");
                }
            }

            return (true, string.Empty);
        }
    }
}
