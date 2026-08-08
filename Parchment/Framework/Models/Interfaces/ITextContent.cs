using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Interfaces
{
    public interface ITextContent
    {
        public string? TextColor { get; }

        /// <summary>The color of the drop shadow drawn behind the text, as a name such as "Black" or a value such as "0 0 0 128". Its own alpha decides how strongly the shadow comes through.
        /// Left unset, the game's shadow color is used and follows <see cref="TextColor"/>'s alpha instead, which is how a translucent element keeps text and shadow at the same strength.
        /// </summary>
        public string? ShadowColor { get; }

        public FontType FontType { get; }
        public string? Text { get; set; }
    }
}
