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

        public Chapter(string? id, int firstPageIndex, int pageCount)
        {
            Id = id;
            FirstPageIndex = firstPageIndex;
            PageCount = pageCount;
        }

        public int LastPageIndex => FirstPageIndex + PageCount - 1;
        public int SpreadCount => (PageCount + 1) / 2;

        public bool ContainsPage(int pageIndex)
        {
            return pageIndex >= FirstPageIndex && pageIndex <= LastPageIndex;
        }
    }
}
