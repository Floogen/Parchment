using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Links;
using Parchment.Framework.Models.Enums;
using System.Linq;

namespace Parchment.Framework.Models.Data
{
    /// <summary>The element data behind one [link] in a text element, built from its <see cref="LinkData"/> rather than authored as an element.
    /// It lets a link run through everything that already works on an element (hit testing, controller snapping, tooltips, actions and tags) instead of each of those learning about links.
    /// It is never laid out or drawn by itself, as the element whose text it sits in places it and draws its text.
    /// </summary>
    public class LinkElementData : ElementData
    {
        public override ElementType Type => ElementType.Link;

        /// <summary>The id the link was defined under, which is also what the markup named.</summary>
        public string LinkId { get; }

        public LinkData Link { get; }

        /// <summary>Whether the link carries any hover effects, which is what has the cursor's arrival time recorded for it so they can ease in from rest.</summary>
        public bool HasHoverEffects { get; }

        public LinkElementData(string linkId, LinkData link, ElementData host)
        {
            LinkId = linkId;
            Link = link;

            DisplayName = link.DisplayName;
            Description = link.Description;
            Action = link.Action;
            Actions = link.Actions;
            HoverAction = link.HoverAction;
            HoverActions = link.HoverActions;
            Tags = link.Tags;

            // Taken from the element the text belongs to, so a link's tooltip and actions read square brackets the way the rest of that element does
            ParseTokenizableStrings = host.ParseTokenizableStrings;
            Sound = host.Sound;
            HasHoverEffects = link.GetHoverEffects().Any();

            // A link that only colors its text gives the cursor nothing to do, so it passes straight through to the element it sits in and that element's own tooltip and actions
            IgnoreCursor = HasCursorContent(link) is false;
        }

        private static bool HasCursorContent(LinkData link)
        {
            bool hasTooltip = string.IsNullOrEmpty(link.DisplayName) is false || string.IsNullOrEmpty(link.Description) is false;
            bool hasActions = string.IsNullOrWhiteSpace(link.Action) is false || (link.Actions is not null && link.Actions.Count is not 0);
            bool hasHoverActions = string.IsNullOrWhiteSpace(link.HoverAction) is false || (link.HoverActions is not null && link.HoverActions.Count is not 0);
            bool hasTags = link.Tags is not null && link.Tags.Count is not 0;

            return hasTooltip || hasActions || hasHoverActions || hasTags || string.IsNullOrWhiteSpace(link.HoverTextColor) is false || link.GetHoverEffects().Any();
        }
    }
}
