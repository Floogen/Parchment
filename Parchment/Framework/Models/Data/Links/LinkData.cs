using System.Collections.Generic;

namespace Parchment.Framework.Models.Data.Links
{
    /// <summary>What a stretch of text wrapped in [link=id] markup does, defined by id in an element's Links.
    /// Each occurrence in the text becomes an element of its own at runtime, so its tooltip, actions and tags behave exactly as they do on any other element.
    /// </summary>
    public class LinkData
    {
        /// <summary>The color the linked text is drawn in. Left unset, the text keeps the color around it.</summary>
        public string? TextColor { get; set; }

        /// <summary>The color the linked text is drawn in while the cursor is over it, covering any [color] markup inside the link.</summary>
        public string? HoverTextColor { get; set; }

        /// <summary>The bold title of the link's hover tooltip.</summary>
        public string? DisplayName { get; set; }

        /// <summary>The body of the link's hover tooltip.</summary>
        public string? Description { get; set; }

        /// <summary>Shorthand for a single-entry <see cref="Actions"/>. When both are given this one runs first.</summary>
        public string? Action { get; set; }

        /// <summary>The trigger actions to run, in order, when the link is clicked.</summary>
        public List<string>? Actions { get; set; }

        /// <summary>Shorthand for a single-entry <see cref="HoverActions"/>. When both are given this one runs first.</summary>
        public string? HoverAction { get; set; }

        /// <summary>The trigger actions to run, in order, when the cursor moves onto the link.</summary>
        public List<string>? HoverActions { get; set; }

        /// <summary>Tags carried by the link, read the same way as an element's own.</summary>
        public List<string>? Tags { get; set; }
    }
}
