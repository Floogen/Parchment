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
    public class TitleElementData : ElementData, ITextContent, ILinkHost
    {
        public override ElementType Type => ElementType.Title;

        public string? TextColor { get; set; }

        /// <summary>The color of the drop shadow drawn behind the text, whose own alpha decides how strongly it comes through.
        /// Left unset, the game's shadow color is used and follows <see cref="TextColor"/>'s alpha instead. Ignored when the font is SpriteText, which draws its own outline.
        /// </summary>
        public string? ShadowColor { get; set; }

        public FontType FontType { get; set; } = FontType.SpriteText;

        public string? Text { get; set; }

        /// <summary>The links this element's text points at with [link=id] markup, by id. Each occurrence in the text gets its own tooltip, actions and tags from the entry it names.</summary>
        public Dictionary<string, LinkData>? Links { get; set; }

        public string? GetLinkedText()
        {
            return Text;
        }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return (false, $"\"Text\" is required.");
            }

            return base.IsValid();
        }
    }
}
