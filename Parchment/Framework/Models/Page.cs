using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
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
        public IContentPack? Owner { get; }

        private Dictionary<PageElementData, Texture2D> _imageTextures = new Dictionary<PageElementData, Texture2D>();

        public Page(PageData data, IContentPack? owner)
        {
            Data = data;
            Owner = owner;
        }

        public Texture2D? GetElementTexture(PageElementData data)
        {
            if (data.ImagePath is null)
            {
                return null;
            }

            if (_imageTextures.TryGetValue(data, out Texture2D? cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = Owner is not null ? Owner.ModContent.Load<Texture2D>(data.ImagePath) : Parchment.modHelper.GameContent.Load<Texture2D>(data.ImagePath);
            _imageTextures[data] = texture;

            return texture;
        }
    }
}
