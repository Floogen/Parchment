using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Parchment.Framework.Models
{
    public class Element
    {
        public ElementData Data { get; }

        public bool IsVisible { get; set; } = true;

        public string? DisplayName { get; set; }
        public string? Description { get; set; }

        public IElementRenderer Renderer { get; }
        public Rectangle Bounds { get; set; }
        public Rectangle? SourceRectangle { get; init; }

        public Color TextColor { get; init; } = Game1.textColor;
        public Color TintColor { get; init; } = Color.White;
        public IAssetName? TextureAssetName { get; init; }

        public IFont? Font { get; set; }
        public Texture2D? Texture { get; set; }

        // The frames whose Condition currently passes, refreshed alongside element conditions. Null when the element has no frames and empty when every frame's condition failed, which makes the element draw its source rectangle statically.
        public List<AnimationFrameData>? ActiveFrames { get; set; }

        internal object? LayoutState { get; set; }
        public bool IsHovered { get; set; }

        public IReadOnlyList<Element> Children { get; init; } = Array.Empty<Element>();

        public Element(ElementData data, IElementRenderer renderer)
        {
            this.Data = data;
            this.Renderer = renderer;
        }
    }
}
