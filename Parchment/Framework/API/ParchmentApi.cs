using Parchment.Framework.UI.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.API
{
    public class ParchmentApi : IParchmentApi
    {
        public bool TryOpenBook(string bookId, string chapterId, int page)
        {
            if (TryCreateMenu(bookId, out BookMenu menu) is false)
            {
                return false;
            }

            if (chapterId is not null)
            {
                bool positioned = page > 0 ? menu.TryOpenAtChapterPage(chapterId, page, out _) : menu.TryOpenAtChapter(chapterId, out _);
                if (positioned is false)
                {
                    return false;
                }
            }
            Game1.activeClickableMenu = menu;

            return true;
        }

        public bool TryOpenBook(string bookId, string? chapterId = null, string? pageId = null)
        {
            if (TryCreateMenu(bookId, out BookMenu menu) is false)
            {
                return false;
            }

            if (pageId is not null)
            {
                if (menu.TryOpenAtPageId(chapterId, pageId, out _) is false)
                {
                    return false;
                }
            }
            else if (chapterId is not null)
            {
                if (menu.TryOpenAtChapter(chapterId, out _) is false)
                {
                    return false;
                }
            }
            Game1.activeClickableMenu = menu;

            return true;
        }

        private static bool TryCreateMenu(string bookId, out BookMenu menu)
        {
            menu = null;

            if (string.IsNullOrWhiteSpace(bookId))
            {
                return false;
            }

            var book = Parchment.bookManager.CreateBook(bookId);
            if (book is null)
            {
                return false;
            }
            menu = new BookMenu(book);

            return true;
        }
    }
}
