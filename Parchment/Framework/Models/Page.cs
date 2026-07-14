using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Texture2D? GetImageTexture(PageElementData data)
        {
            if (data.ImagePath is null || Owner is null)
            {
                return null;
            }

            if (_imageTextures.ContainsKey(data) is false)
            {
                _imageTextures[data] = Owner.ModContent.Load<Texture2D>(data.ImagePath);
            }

            return _imageTextures[data];
        }
    }
}
