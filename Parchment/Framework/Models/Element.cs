using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using StardewModdingAPI;
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
        public IElementRenderer Renderer { get; }
        public Rectangle Bounds { get; set; }

        public Element(ElementData data, IElementRenderer renderer)
        {
            this.Data = data;
            this.Renderer = renderer;
        }
    }
}
