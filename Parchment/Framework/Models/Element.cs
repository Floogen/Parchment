using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities.Helpers;
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

        // The same for ImageElementData.HoverFrames, cached separately so hovering picks between two ready lists rather than re-running frame conditions every time the cursor moves.
        public List<AnimationFrameData>? ActiveHoverFrames { get; set; }

        // When the element's normal animation last started, on the same clock as Game1.currentGameTime. Cycles are measured from here rather than from absolute game time, so a frame set that only just became active plays from its first frame instead of joining a cycle already in progress.
        public double AnimationStartedAt { get; set; }

        // The same for the hover animation, stamped when the cursor arrives
        public double HoverAnimationStartedAt { get; set; }

        /// <summary>The frame this element was showing when frame actions were last dispatched, which is how entering a new frame is told apart from staying on the current one.
        /// Cleared whenever the animation restarts, so a set of frames that becomes active again runs its first frame's actions rather than skipping them.
        /// </summary>
        public AnimationFrameData? LastPlayedFrame { get; set; }

        internal object? LayoutState { get; set; }

        private bool _isHovered;

        /// <summary>Whether the cursor is currently over this element. Arriving restarts the hover animation and leaving restarts the normal one, so each plays from its own first frame rather than picking up where the other left off.</summary>
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered == value)
                {
                    return;
                }

                // Only a hover animation that actually replaced the normal frames needs either side restarted. Without this an element with no hover frames would visibly hitch on exit, having never stopped playing its normal animation
                bool hasHoverAnimation = this.ActiveHoverFrames is not null && this.ActiveHoverFrames.Count is not 0;

                _isHovered = value;

                if (hasHoverAnimation is false)
                {
                    return;
                }

                if (value is true)
                {
                    this.HoverAnimationStartedAt = AnimationHelper.GetAnimationTime();
                }
                else
                {
                    this.AnimationStartedAt = AnimationHelper.GetAnimationTime();
                }
            }
        }

        /// <summary>Whether this element currently has keyboard focus. Only an Input takes focus, and only one element in the book holds it at a time.</summary>
        public bool IsFocused { get; set; }

        /// <summary>Whether this element does anything when the cursor reaches it, whether that is a tooltip, an action or a swap to hover art.
        /// Absolutely positioned layers such as <see cref="PageData.Background"/> and <see cref="PageData.Foreground"/> use this so purely decorative art passes the cursor through to whatever sits under it.
        /// </summary>
        public bool IsInteractive => Data.IsAlwaysInteractive || string.IsNullOrEmpty(DisplayName) is false || string.IsNullOrEmpty(Description) is false || Data.HasActions || Data.HasHoverActions || (Data is ISprite sprite && sprite.HoverTextureSourceRectangle is not null) || (Data is ImageElementData imageElementData && imageElementData.HoverFrames is not null && imageElementData.HoverFrames.Count is not 0);

        public IReadOnlyList<Element> Children { get; init; } = Array.Empty<Element>();

        /// <summary>Placed elements drawn behind <see cref="Children"/>, from <see cref="Interfaces.ILayeredContainer.Background"/>. Empty on anything that isn't a layered container.</summary>
        public IReadOnlyList<Element> Background { get; init; } = Array.Empty<Element>();

        /// <summary>Placed elements drawn over <see cref="Children"/>, from <see cref="Interfaces.ILayeredContainer.Foreground"/>. Empty on anything that isn't a layered container.</summary>
        public IReadOnlyList<Element> Foreground { get; init; } = Array.Empty<Element>();

        public Element(ElementData data, IElementRenderer renderer)
        {
            this.Data = data;
            this.Renderer = renderer;
        }
    }
}
