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
    public class PageEntry
    {
        public PageData Data { get; }
        public IContentPack? Owner { get; }

        private Texture2D? _imageTexture;

        public PageEntry(PageData data, IContentPack? owner)
        {
            Data = data;
            Owner = owner;
        }

        public Texture2D? GetImageTexture()
        {
            if (Data.ImagePath is null || Owner is null)
            {
                return null;
            }

            return _imageTexture ??= Owner.ModContent.Load<Texture2D>(Data.ImagePath);
        }
    }
}
