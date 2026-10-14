using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Links;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class ParagraphElementData : ElementData, ITextContent, ILinkHost
    {
        public override ElementType Type => ElementType.Paragraph;

        public string? TextColor { get; set; }

        /// <summary>The color of the drop shadow drawn behind the text, whose own alpha decides how strongly it comes through.
        /// Left unset, the game's shadow color is used and follows <see cref="TextColor"/>'s alpha instead. Ignored when the font is SpriteText, which draws its own outline.
        /// </summary>
        public string? ShadowColor { get; set; }

        public FontType FontType { get; set; } = FontType.Small;

        public string? Text { get; set; }

        /// <summary>The links this element's text points at with [link=id] markup, by id. Each occurrence in the text gets its own tooltip, actions and tags from the entry it names.</summary>
        public Dictionary<string, LinkData>? Links { get; set; }

        public string? GetLinkedText()
        {
            return Text;
        }

        /// <summary>
        /// Optional. The width the paragraph occupies, in unscaled pixels (multiplied by <see cref="ElementData.Scale"/>). Text wraps at this width and the element reserves it,
        /// so <see cref="ElementData.Alignment"/> places lines within it and hit testing covers the whole box rather than just the longest line.
        /// When null, the text wraps at the full width available and the element is only as wide as its longest line. Clamped to the width available, so this can only narrow the paragraph and never widen it.
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Optional. The height the paragraph occupies, in unscaled pixels (multiplied by <see cref="ElementData.Scale"/>). The paragraph reserves it whether or not its text fills it,
        /// and text that runs past the bottom is handled by <see cref="Overflow"/>. Clamped to the height left on the page, so a paragraph near the bottom shows fewer lines rather than running off it.
        /// When null, the paragraph is as tall as all of its text.
        /// </summary>
        public int? Height { get; set; }

        /// <summary>What happens to text that runs past <see cref="Height"/>: scrolled into view or left out. Ignored without a <see cref="Height"/>, as the paragraph then grows to fit everything.</summary>
        public OverflowMode Overflow { get; set; } = OverflowMode.Scroll;

        /// <summary>How the scroll bar looks, drawn beside the text whenever <see cref="Overflow"/> is Scroll and there is more text than fits. Always present, so a single field of it can be set on its own.</summary>
        public ScrollBarData ScrollBar { get; set; } = new ScrollBarData();

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return (false, $"\"Text\" is required.");
            }

            if (Width is int width && width <= 0)
            {
                return (false, $"\"Width\" must be positive.");
            }

            if (Height is int height && height <= 0)
            {
                return (false, $"\"Height\" must be positive.");
            }

            if (ScrollBar is null)
            {
                return (false, $"\"ScrollBar\" can't be null. Leave it out to keep the default bar.");
            }

            (bool isScrollBarValid, string scrollBarError) = ScrollBar.IsValid();
            if (isScrollBarValid is false)
            {
                return (false, scrollBarError);
            }

            return base.IsValid();
        }
    }
}
