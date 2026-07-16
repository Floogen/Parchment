using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.UI.Rendering;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Parchment.Framework.Models
{
    public class Page
    {
        public PageData Data { get; }

        public List<Element> Elements { get; } =  new List<Element>();
        public ElementRenderContext LastLayoutContext;

        private Dictionary<ElementData, Texture2D> _imageTextures = new Dictionary<ElementData, Texture2D>();

        public Page(PageData data, ElementRegistry registry)
        {
            Data = data;

            foreach (var elementData in data.Elements)
            {
                var element = Create(elementData, registry);
                if (element is not null)
                {
                    Elements.Add(element);
                }
            }
        }

        public Texture2D? GetElementTexture(ElementData data)
        {
            /*
            if (data.TexturePath is null)
            {
                return null;
            }

            if (_imageTextures.TryGetValue(data, out Texture2D? cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = Owner is not null ? Owner.ModContent.Load<Texture2D>(data.TexturePath) : Parchment.modHelper.GameContent.Load<Texture2D>(data.TexturePath);
            _imageTextures[data] = texture;

            return texture;
            */

            return null;
        }

        /// <summary>
        /// This should be called anytime the UI changes for scaling / width
        /// </summary>
        public void PerformLayout(ElementRenderContext context)
        {
            float currentY = 0f;

            foreach (Element element in this.Elements)
            {
                Vector2 elementSize = element.Renderer.Measure(element, context);
                element.Bounds = new Rectangle(0, (int)currentY, (int)elementSize.X, (int)elementSize.Y);
                currentY += elementSize.Y + element.Data.SpacingAfter * element.Data.Scale;
            }

            LastLayoutContext = context;
        }

        private Element? Create(ElementData data, ElementRegistry registry)
        {
            if (registry.TryResolve(data.Type, out ElementRegistration registration) is false)
            {
                Parchment.monitor.Log($"No renderer registered for element type {data.Type}; skipping element.", LogLevel.Warn);
                return null;
            }

            return new Element(data, registration.Renderer);
        }
    }
}
