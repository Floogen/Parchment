using Parchment.Framework.Models.Data.Elements;
using System.Collections.Generic;

namespace Parchment.Framework.Models.Interfaces
{
    /// <summary>A container that also holds absolutely positioned layers around its stacked <see cref="IContainer.Children"/>, the way a page holds a Background and Foreground around its Elements.
    /// Both layers are anchored to the container's content area, the same rectangle the children sit in, so a layer is inset by the container's border and padding just as a child is.
    /// </summary>
    public interface ILayeredContainer : IContainer
    {
        List<ElementData>? Background { get; set; }
        List<ElementData>? Foreground { get; set; }
    }
}
