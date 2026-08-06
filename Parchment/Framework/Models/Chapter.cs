using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models
{
    public class Chapter
    {
        public string? Id { get; }
        public int FirstPageIndex { get; }
        public int PageCount { get; }

        /// <summary>Whether the book this chapter belongs to shows one page at a time, in which case every page is a spread of its own.</summary>
        public bool IsSinglePage { get; }

        public Chapter(string? id, int firstPageIndex, int pageCount, bool isSinglePage = false)
        {
            Id = id;
            FirstPageIndex = firstPageIndex;
            PageCount = pageCount;
            IsSinglePage = isSinglePage;
        }

        public int LastPageIndex => FirstPageIndex + PageCount - 1;
        public int SpreadCount => IsSinglePage ? PageCount : (PageCount + 1) / 2;

        public bool ContainsPage(int pageIndex)
        {
            return pageIndex >= FirstPageIndex && pageIndex <= LastPageIndex;
        }
    }
}
