using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Fonts;
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

        public List<Element> Elements { get; }
        public ElementRenderContext? LastLayoutContext;

        private Dictionary<ElementData, Texture2D> _imageTextures = new Dictionary<ElementData, Texture2D>();

        public Page(PageData data, ElementRegistry registry, FontResolver fontResolver)
        {
            Data = data;
            Elements = CreateElements(registry, fontResolver);
        }

        private List<Element> CreateElements(ElementRegistry registry, FontResolver fontResolver)
        {
            var elements = new List<Element>();
            foreach (var elementData in Data.Elements)
            {
                var element = Create(elementData, registry, fontResolver);
                if (element is not null)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }

        private Element? Create(ElementData data, ElementRegistry registry, FontResolver fontResolver)
        {
            if (registry.TryResolve(data.Type, out ElementRegistration registration) is false)
            {
                Parchment.monitor.Log($"No renderer registered for element type {data.Type}; skipping element.", LogLevel.Warn);
                return null;
            }

            IFont? font = null;
            if (data is ITextContent textContent)
            {
                font = fontResolver.Resolve(textContent.FontType);
            }

            return new Element(data, registration.Renderer) { Font = font };
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
    }
}
