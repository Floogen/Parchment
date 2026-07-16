using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Rendering.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering
{
    public class ElementRegistry
    {
        private readonly Dictionary<string, ElementRegistration> _registrationsByKey = new Dictionary<string, ElementRegistration>(StringComparer.OrdinalIgnoreCase);

        public ElementRegistry(bool registerDefaults = false)
        {
            if (registerDefaults is true)
            {
                RegisterDefaults();
            }
        }

        public void Register(string key, IElementRenderer renderer)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Element key cannot be null or empty.", nameof(key));
            }

            if (_registrationsByKey.ContainsKey(key) is true)
            {
                throw new ArgumentException($"Element key {key} is already registered.", nameof(key));
            }

            _registrationsByKey[key] = new ElementRegistration(key, renderer);
        }

        public void Register(ElementType elementType, IElementRenderer renderer)
        {
            if (elementType is ElementType.Unknown)
            {
                throw new ArgumentException("Cannot register a renderer for ElementType.Unknown.", nameof(elementType));
            }

            this.Register(elementType.ToString(), renderer);
        }

        // When a new ElementType is created, its renderer should be registered here
        public void RegisterDefaults()
        {
            this.Register(ElementType.Title, new TitleElementRenderer());
            this.Register(ElementType.Heading, new HeadingElementRenderer());
            this.Register(ElementType.Paragraph, new ParagraphElementRenderer());
            this.Register(ElementType.Image, new ImageElementRenderer());
            this.Register(ElementType.Panel, new PanelElementRenderer());
            this.Register(ElementType.Banner, new BannerElementRenderer());
        }

        public bool TryResolve(string key, out ElementRegistration registration)
        {
            return _registrationsByKey.TryGetValue(key, out registration);
        }

        internal bool TryResolve(ElementType elementType, out ElementRegistration registration)
        {
            return this.TryResolve(elementType.ToString(), out registration);
        }
    }
}
