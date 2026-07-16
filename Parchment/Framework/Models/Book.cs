using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Data;
using Parchment.Framework.UI.Fonts;
using Parchment.Framework.UI.Rendering;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parchment.Framework.Models
{
    public class Book
    {
        public BookData Data { get; }
        public List<Page> Pages { get; }

        public Book(BookData data, ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            Data = data;
            Pages = CreatePages(elementRegistry, fontResolver);
        }

        private List<Page> CreatePages(ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            var pages = new List<Page>();

            int pageIndex = 0;
            foreach (PageData pageData in Data.Pages ?? Enumerable.Empty<PageData>())
            {
                var page = CreatePage(pageData, $"{Data.Id}/page[{pageIndex}]", elementRegistry, fontResolver);

                pageIndex++;
                if (page is null)
                {
                    continue;
                }

                pages.Add(page);
            }

            return pages;
        }

        private Page? CreatePage(PageData pageData, string pageDescription, ElementRegistry elementRegistry, FontResolver fontResolver)
        {
            if (pageData is null)
            {
                Parchment.monitor.Log($"Skipping null page at {pageDescription}.", LogLevel.Warn);
                return null;
            }

            return new Page(pageData, elementRegistry, fontResolver);
        }

        public void RefreshTextures(IReadOnlyCollection<IAssetName> invalidatedAssetNames)
        {
            foreach (Page page in Pages)
            {
                page.RefreshTextures(invalidatedAssetNames);
            }
        }
    }
}
