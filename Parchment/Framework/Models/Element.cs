using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
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
        public Rectangle? SourceRectangle { get; set; }

        public Color TextColor { get; init; } = Game1.textColor;
        public Color TintColor { get; init; } = Color.White;
        public IAssetName? TextureAssetName { get; init; }

        /// <summary>The item this element is currently showing, when it is a Grid result cell or something inside one. Null everywhere else, and what the %Item% token resolves to.</summary>
        public string? AssignedItemId { get; set; }

        /// <summary>The parsed data behind <see cref="AssignedItemId"/>, kept so the %Item.Something% tokens read it rather than looking it up on every draw.</summary>
        public ParsedItemData? AssignedItemData { get; set; }

        /// <summary>An instance of <see cref="AssignedItemId"/>, built once when the cell is assigned. Only the properties that can't be answered without one need it, such as category name and price.</summary>
        public Item? AssignedItem { get; set; }

        /// <summary>The candidates and filter behind a Grid's cells. Only set on a Grid carrying a Source block.</summary>
        public ResultSet? Results { get; set; }

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

        /// <summary>This element's text as it last resolved, which is how a token whose value changed without any condition changing is spotted. Null until the element has been looked at once.</summary>
        public string? LastResolvedText { get; set; }

        /// <summary>The input text this element was last seen holding, which is how a change is told apart from the text sitting still. Null until the element has been looked at once, so a book doesn't count its own starting text as a change.</summary>
        public string? LastSeenInputText { get; set; }

        /// <summary>How long is left before this input's text changed actions run, in milliseconds, or null when nothing is waiting to run. Each change puts it back to the input's TextChangedDelay.</summary>
        public float? TextChangedDelayRemaining { get; set; }

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
