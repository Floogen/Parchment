using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
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

        private bool _isVisible = true;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                // Stamped when the element comes into view, so a typewriter in it starts from its appearance rather than from when the page did
                if (value is true && _isVisible is false)
                {
                    VisibleSince = AnimationHelper.GetAnimationTime();
                }

                _isVisible = value;
            }
        }

        /// <summary>When the element last came into view after being hidden, on the animation clock. Null for an element that has been visible since it was built.</summary>
        public double? VisibleSince { get; private set; }

        /// <summary>The [typewriter] effects in this element's text as it was last laid out, in the order they open. Empty for an element with none.</summary>
        public IReadOnlyList<TextEffect> TypewriterEffects { get; set; } = Array.Empty<TextEffect>();

        /// <summary>How far along each of this element's typewriters is, by <see cref="TextEffect.TypingOrdinal"/>. Carried across a refresh, so rebuilding the book doesn't type the text out again.</summary>
        public Dictionary<int, TypingState> TypingStates { get; set; } = new Dictionary<int, TypingState>();

        /// <summary>Where a link's text ends in its host's text, used to tell whether a typewriter has revealed all of it yet. -1 for anything that isn't a link.</summary>
        public int LinkTextEnd { get; set; } = -1;

        /// <summary>Whether a typewriter is still revealing this link's text, which keeps the cursor and a controller off it until all of it is showing.</summary>
        public bool IsAwaitingReveal { get; set; }

        /// <summary>Whether all of this link's text sits under a [redact], which keeps the cursor and a controller off it so its tooltip can't give away what the bar hides.</summary>
        public bool IsRedacted { get; set; }

        /// <summary>Whether this element's typed actions have run this reading, which is what keeps them to once however often its page comes back into view.</summary>
        public bool HasRunTypedActions { get; set; }

        /// <summary>The conditions written into this element's tags with a "condition=" part, in the order the tags appear, leaving out a typewriter's. Read once from the authored text.</summary>
        public IReadOnlyList<string> InlineConditions { get; init; } = Array.Empty<string>();

        /// <summary>Whether each of <see cref="InlineConditions"/> passes, refreshed alongside the element's own Condition. A change lays the text out again so the tags follow it.</summary>
        public List<bool> InlineConditionResults { get; } = new List<bool>();

        /// <summary>When each of <see cref="InlineConditionResults"/> last changed, on the animation clock, which is what a [scramble] settles from. Null when it hasn't changed since it was first checked,
        /// or changed while the element wasn't on screen, so text the reader never saw scrambled doesn't settle in front of them.
        /// </summary>
        public List<double?> InlineConditionChangedAt { get; } = new List<double?>();

        /// <summary>When this element's text was last drawn, on the animation clock, which is how a change to one of its conditions is told to have happened in front of the reader.</summary>
        public double LastDrawnAt { get; set; } = double.MinValue;

        /// <summary>The container this element sits inside, whether as a child or in one of its layers. Null for anything at the top of a page or a book's Underlay and Overlay.
        /// Set once when the element is created, so it always points into the same book rather than following an element that was carried across a refresh.
        /// </summary>
        public Element? Parent { get; set; }

        /// <summary>When a ShowElement action last put this element up, in the same clock the animations run on. Null until something does,
        /// which is what keeps an element with a <see cref="ElementData.Lifetime"/> out of the way until it's wanted.
        /// </summary>
        public double? ShownAt { get; set; }

        public string? DisplayName { get; set; }
        public string? Description { get; set; }

        public IElementRenderer Renderer { get; }
        public Rectangle Bounds { get; set; }
        public Rectangle? SourceRectangle { get; set; }

        public Color TextColor { get; init; } = Game1.textColor;

        /// <summary>The color of the drop shadow drawn behind this element's text, or null when the element leaves it to the game's own.
        /// Read through <see cref="GetShadowColor"/> rather than directly, since a given color and the default answer to a fade differently.
        /// </summary>
        public Color? ShadowColor { get; init; }

        public Color TintColor { get; init; } = Color.White;

        /// <summary>The color this element's text takes while the cursor is over it (null to keep its usual color). Only a link has one, which its host draws the linked text in.</summary>
        public Color? HoverTextColor { get; init; }

        /// <summary>The rectangles the cursor reaches this element through, measured from the same origin as <see cref="Bounds"/>. Null when the whole of <see cref="Bounds"/> counts.
        /// A link that wraps covers a stretch of two lines rather than the box around them, so it is reached through one rectangle per line while <see cref="Bounds"/> holds their union.
        /// </summary>
        public IReadOnlyList<Rectangle>? HitRegions { get; set; }
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

        // The same for ElementData.HoverFrames, cached separately so hovering picks between two ready lists rather than re-running frame conditions every time the cursor moves.
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

                // A link's hover effects are timed from the cursor's arrival rather than the shared clock, so they ease in from rest instead of appearing mid-cycle
                if (value is true && Data is LinkElementData { HasHoverEffects: true })
                {
                    this.HoverAnimationStartedAt = AnimationHelper.GetAnimationTime();
                }

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

        /// <summary>Whether an element on a timer is still within it. Always true for one without a <see cref="ElementData.Lifetime"/>, which is every element that stays put.</summary>
        public bool IsWithinLifetime
        {
            get
            {
                if (Data.Lifetime is not float lifetime)
                {
                    return true;
                }

                // Never shown, so it's waiting rather than expired
                if (ShownAt is not double shownAt)
                {
                    return false;
                }

                return AnimationHelper.GetAnimationTime() - shownAt < lifetime * 1000d;
            }
        }

        /// <summary>How strongly to draw the element, being one for everything that isn't partway through fading out.
        /// Read at draw time rather than stored, so the fade runs at the frame rate instead of stepping on the condition refresh.
        /// A container's fade carries down, so everything inside a panel that's on its way out goes with it rather than staying solid until the panel vanishes.
        /// </summary>
        public float DrawAlpha { get { return OwnDrawAlpha * (Parent?.DrawAlpha ?? 1f); } }

        /// <summary>How strongly to draw this element on its own account, before anything it sits inside has its say.</summary>
        private float OwnDrawAlpha
        {
            get
            {
                if (Data.Lifetime is not float lifetime || Data.FadeAfter is not float fadeAfter || ShownAt is not double shownAt)
                {
                    return 1f;
                }

                double elapsed = (AnimationHelper.GetAnimationTime() - shownAt) / 1000d;

                if (elapsed <= fadeAfter)
                {
                    return 1f;
                }

                // IsValid keeps FadeAfter below Lifetime, so this can't divide by zero
                float remaining = (float)(1d - ((elapsed - fadeAfter) / (lifetime - fadeAfter)));

                return Math.Clamp(remaining, 0f, 1f);
            }
        }

        /// <summary>Whether this element does anything when the cursor reaches it, whether that is a tooltip, an action, a swap to hover art or a hover text color.
        /// Absolutely positioned layers such as <see cref="PageData.Background"/> and <see cref="PageData.Foreground"/> use this so purely decorative art passes the cursor through to whatever sits under it.
        /// Always false when <see cref="ElementData.IgnoreCursor"/> is set, since that element is stepped over wherever it sits.
        /// </summary>
        public bool IsInteractive => Data.IgnoreCursor is false && (Data.IsAlwaysInteractive || string.IsNullOrEmpty(DisplayName) is false || string.IsNullOrEmpty(Description) is false || Data.HasActions || Data.HasHoverActions || (Data.Tags is not null && Data.Tags.Count is not 0) || (Data is ISprite sprite && sprite.HoverTextureSourceRectangle is not null) || (Data.HoverFrames is not null && Data.HoverFrames.Count is not 0) || HoverTextColor is not null);

        /// <summary>The shadow to draw behind text of the given color, where <paramref name="drawnTextColor"/> is the text color as it will actually be drawn, after any fade.
        /// Without a given <see cref="ShadowColor"/> the game's own follows the text's alpha, which is what keeps a translucent or fading element from leaving its shadow behind.
        /// A given value sets its own strength instead, so only the element's fade is applied on top of it.
        /// </summary>
        public Color GetShadowColor(Color drawnTextColor)
        {
            if (ShadowColor is not Color shadowColor)
            {
                return Game1.textShadowColor * (drawnTextColor.A / 255f);
            }

            return shadowColor * DrawAlpha;
        }

        /// <summary>This element's authored tags, followed by the ones Parchment derives from what it is currently holding, being the item a Grid cell or an item Image is showing.
        /// Composed on each call rather than merged into <see cref="ElementData.Tags"/>, as the deserialized data is shared across the books using it and a cell's item changes as its filter narrows.
        /// </summary>
        public IEnumerable<string> GetTags()
        {
            if (Data.Tags is not null)
            {
                foreach (string tag in Data.Tags)
                {
                    if (string.IsNullOrWhiteSpace(tag) is false)
                    {
                        yield return tag;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(AssignedItemId) is false)
            {
                yield return $"{TagHelper.ITEM_PREFIX}{AssignedItemId}";
            }
        }

        /// <summary>Whether this element carries a tag, ignoring case. Reads <see cref="GetTags"/>, so a derived item tag counts alongside the authored ones.</summary>
        public bool HasTag(string? tag)
        {
            return string.IsNullOrWhiteSpace(tag) is false && GetTags().Any(elementTag => string.Equals(elementTag, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Whether any of this element's tags contains the given text, ignoring case.
        /// Empty text matches an element with any tag at all, which is what leaves an untouched search box showing everything. An element with no tags never matches.
        /// </summary>
        public bool HasTagMatching(string? text)
        {
            var tags = GetTags().ToList();

            if (tags.Count is 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(text) is true)
            {
                return true;
            }

            return tags.Any(elementTag => elementTag.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

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
