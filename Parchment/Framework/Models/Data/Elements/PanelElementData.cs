using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PanelElementData : ElementData, ISprite
    {
        public override ElementType Type => ElementType.Panel;

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }

        /// <summary>
        /// Optional. If not given, fill the entire Page's width
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Optional. If not given, increase based on total height needed by Children
        /// </summary>
        public int Height { get; set; }

        // Sub-elements
        public List<ElementData>? Children { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
