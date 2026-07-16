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
        public FontType FontType { get; }
        public string? Text { get; set; }
    }
}
