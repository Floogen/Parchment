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
    public class BookEntry
    {
        public BookData Data { get; }
        public IContentPack? Owner { get; }
        public List<PageEntry> Pages { get; }

        public BookEntry(BookData data, IContentPack? owner)
        {
            Data = data;
            Owner = owner;
            Pages = data.Pages
                .OrderBy(page => page.Order)
                .Select(page => new PageEntry(page, owner))
                .ToList();
        }
    }
}
