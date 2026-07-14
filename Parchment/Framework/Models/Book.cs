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
    public class Book
    {
        public BookData Data { get; }
        public IContentPack? Owner { get; }
        public List<Page> Pages { get; }

        public Book(BookData data, IContentPack? owner)
        {
            Data = data;
            Owner = owner;
            Pages = data.Pages
                .Select(page => new Page(page, owner))
                .ToList();
        }
    }
}
