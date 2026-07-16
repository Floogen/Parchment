using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering
{
    public class ElementRegistration
    {
        public string Key { get; }
        public IElementRenderer Renderer { get; }

        public ElementRegistration(string key, IElementRenderer elementRenderer)
        {
            Key = key;
            Renderer = elementRenderer;
        }
    }
}
