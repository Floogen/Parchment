using Parchment.Framework.Models.Data.Links;
using System.Collections.Generic;

namespace Parchment.Framework.Models.Interfaces
{
    /// <summary>A text element whose text can carry [link=id] markup, pointing at the links it defines.</summary>
    public interface ILinkHost
    {
        /// <summary>The links this element's text can point at, by id. Ids are matched ignoring case.</summary>
        public Dictionary<string, LinkData>? Links { get; set; }

        /// <summary>The authored string the [link] markup is written in, read when the element is built so each link can be given an element of its own.</summary>
        public string? GetLinkedText();
    }
}
