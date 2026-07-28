using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
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
        public abstract ElementType Type { get; }

        /// <summary>Where the element sits within the width available to it, and where each line of its text sits within the element.
        /// Only has an effect when the element is narrower than the space available. In a placed list (a page's Background or Foreground, a book's Underlay or Overlay) this anchors the element
        /// and <see cref="Position"/> is then an offset from that anchor.
        /// </summary>
        public AlignmentType Alignment { get; set; } = AlignmentType.Left;

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
        /// Measured from wherever <see cref="Alignment"/> anchors the element, so it is an absolute coordinate under the default Left and an offset from the centre or right edge otherwise.
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

        /// <summary>Whether this element has at least one click action, from either <see cref="Action"/> or <see cref="Actions"/>.</summary>
        internal bool HasActions => HasAny(Action, Actions);

        /// <summary>Whether this element has at least one hover action, from either <see cref="HoverAction"/> or <see cref="HoverActions"/>.</summary>
        internal bool HasHoverActions => HasAny(HoverAction, HoverActions);

        /// <summary>Every click action on this element, <see cref="Action"/> first and then <see cref="Actions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetActions() => Combine(Action, Actions);

        /// <summary>Every hover action on this element, <see cref="HoverAction"/> first and then <see cref="HoverActions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetHoverActions() => Combine(HoverAction, HoverActions);

        private static bool HasAny(string? single, List<string>? many) => string.IsNullOrWhiteSpace(single) is false || (many is not null && many.Any(action => string.IsNullOrWhiteSpace(action) is false));

        /// <summary>Yields the single field followed by the list, skipping empty entries.
        /// This is composed on each call rather than merged into the list at load, as the deserialized instance is shared and merging would accumulate duplicates across asset reloads.
        /// </summary>
        private static IEnumerable<string> Combine(string? single, List<string>? many)
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

            return (true, string.Empty);
        }
    }
}
