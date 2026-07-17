using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Managers
{
    public class QueryManager : BaseManager
    {
        public const string IS_BOOK_OPEN = "PeacefulEnd.Parchment_IsBookOpen";
        public const string CURRENT_BOOK_STATE = "PeacefulEnd.Parchment_CurrentBookState";

        public const string IS_HOVERING_LEFT_PAGE = "PeacefulEnd.Parchment_IsHoveringLeftPage";
        public const string IS_HOVERING_RIGHT_PAGE = "PeacefulEnd.Parchment_IsHoveringRightPage";
        public const string IS_FIRST_PAGE = "PeacefulEnd.Parchment_IsFirstPage";
        public const string IS_LAST_PAGE = "PeacefulEnd.Parchment_IsLastPage";
        public const string IS_GOING_FORWARD = "PeacefulEnd.Parchment_IsPagingForward";

        public const string CURRENT_PAGE_NUMBER = "PeacefulEnd.Parchment_CurrentPageNumber";
        public const string CURRENT_PAGE_ID = "PeacefulEnd.Parchment_CurrentPageId";
        public const string CURRENT_CHAPTER_ID = "PeacefulEnd.Parchment_CurrentChapterId";
        public const string CURRENT_BOOK_ID = "PeacefulEnd.Parchment_CurrentBookId";

        public QueryManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            RegisterAll();
        }

        public void RegisterAll()
        {
            GameStateQuery.Register(IS_BOOK_OPEN, IsBookOpen);
            GameStateQuery.Register(CURRENT_BOOK_STATE, CurrentBookState);

            GameStateQuery.Register(IS_HOVERING_LEFT_PAGE, IsHoveringLeftPage);
            GameStateQuery.Register(IS_HOVERING_RIGHT_PAGE, IsHoveringRightPage);

            GameStateQuery.Register(IS_FIRST_PAGE, IsFirstPage);
            GameStateQuery.Register(IS_LAST_PAGE, IsLastPage);
            GameStateQuery.Register(IS_GOING_FORWARD, IsPagingForward);

            GameStateQuery.Register(CURRENT_PAGE_NUMBER, CurrentPageNumber);
            GameStateQuery.Register(CURRENT_PAGE_ID, CurrentPageId);
            GameStateQuery.Register(CURRENT_CHAPTER_ID, CurrentChapterId);
            GameStateQuery.Register(CURRENT_BOOK_ID, CurrentBookId);
        }

        private bool IsBookOpen(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return true;
        }        

        private bool CurrentBookState(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }


            if (ArgUtility.TryGetEnum<BookMenu.MenuState>(query, 1, out var bookState, out string error) is false)
            {
                return false;
            }

            return bookMenu.CurrentState == bookState;
        }

        private bool IsHoveringLeftPage(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.IsHoveringLeftPage();
        }

        private bool IsHoveringRightPage(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.IsHoveringRightPage();
        }

        private bool IsFirstPage(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.IsOnPage(0);
        }

        private bool IsLastPage(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.IsOnPage(bookMenu.Book.Pages.Count - 1);
        }

        private bool IsPagingForward(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.IsPagingForward();
        }

        private bool CurrentPageNumber(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGetInt(query, 1, out int pageIndex, out string error) is false)
            {
                return false;
            }

            return bookMenu.IsOnPage(pageIndex);
        }

        private bool CurrentPageId(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(query, 1, out string pageId, out string error) is false)
            {
                return false;
            }

            return bookMenu.IsOnPage(pageId);
        }

        private bool CurrentChapterId(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(query, 1, out string chapterId, out string error) is false)
            {
                return false;
            }

            return bookMenu.IsInChapter(chapterId);
        }

        private bool CurrentBookId(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(query, 1, out string bookId, out string error) is false)
            {
                return false;
            }

            return bookMenu.Book.Data.Id.EqualsIgnoreCase(bookId);
        }

        private bool TryGetBookMenu(out BookMenu bookMenu)
        {
            if (Game1.activeClickableMenu is BookMenu activeBookMenu)
            {
                bookMenu = activeBookMenu;

                return true;
            }
            bookMenu = null;

            return false;
        }
    }
}
