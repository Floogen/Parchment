using Parchment.Framework.Models.Data;
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
using System.Threading;
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
        public const string CAN_GO_BACK = "PeacefulEnd.Parchment_CanGoBack";

        public const string HAS_SEEN_PAGE_ID = "PeacefulEnd.Parchment_HasSeenPageId";
        public const string HAS_SEEN_CHAPTERLESS_PAGE_ID = "PeacefulEnd.Parchment_HasSeenChapterlessPageId";
        public const string HAS_SEEN_CHAPTER_ID = "PeacefulEnd.Parchment_HasSeenChapterId";

        public const string CURRENT_PAGE_INDEX = "PeacefulEnd.Parchment_CurrentPageIndex";
        public const string CURRENT_PAGE_ID = "PeacefulEnd.Parchment_CurrentPageId";
        public const string CURRENT_CHAPTER_ID = "PeacefulEnd.Parchment_CurrentChapterId";
        public const string CURRENT_BOOK_ID = "PeacefulEnd.Parchment_CurrentBookId";

        public const string CURRENT_PAGE_HAS_TAG = "PeacefulEnd.Parchment_CurrentPageHasTag";
        public const string PAGE_HAS_TAG = "PeacefulEnd.Parchment_PageHasTag";
        public const string PAGE_TAG_MATCHES_INPUT = "PeacefulEnd.Parchment_PageTagMatchesInput";

        public const string INPUT_MATCHES = "PeacefulEnd.Parchment_InputMatches";
        public const string INPUT_EQUALS = "PeacefulEnd.Parchment_InputEquals";
        public const string HAS_INPUT_TEXT = "PeacefulEnd.Parchment_HasInputText";

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
            GameStateQuery.Register(CAN_GO_BACK, CanGoBack);

            GameStateQuery.Register(HAS_SEEN_PAGE_ID, HasSeenPageId);
            GameStateQuery.Register(HAS_SEEN_CHAPTERLESS_PAGE_ID, HasSeenChapterlessPageId);
            GameStateQuery.Register(HAS_SEEN_CHAPTER_ID, HasSeenChapterId);

            GameStateQuery.Register(CURRENT_PAGE_INDEX, CurrentPageIndex);
            GameStateQuery.Register(CURRENT_PAGE_ID, CurrentPageId);
            GameStateQuery.Register(CURRENT_CHAPTER_ID, CurrentChapterId);
            GameStateQuery.Register(CURRENT_BOOK_ID, CurrentBookId);

            GameStateQuery.Register(CURRENT_PAGE_HAS_TAG, CurrentPageHasTag);
            GameStateQuery.Register(PAGE_HAS_TAG, PageHasTag);
            GameStateQuery.Register(PAGE_TAG_MATCHES_INPUT, PageTagMatchesInput);

            GameStateQuery.Register(INPUT_MATCHES, InputMatches);
            GameStateQuery.Register(INPUT_EQUALS, InputEquals);
            GameStateQuery.Register(HAS_INPUT_TEXT, HasInputText);
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

        private bool CanGoBack(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.CanGoBack();
        }

        private bool IsPagingForward(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            return bookMenu.IsPagingForward();
        }

        private bool HasSeenPageId(string[] query, GameStateQueryContext context)
        {
            if (ArgUtility.TryGet(query, 1, out string bookId, out string error) is false)
            {
                return false;
            }
            if (ArgUtility.TryGet(query, 2, out string chapterId, out error) is false)
            {
                return false;
            }
            if (ArgUtility.TryGet(query, 3, out string pageId, out error) is false)
            {
                return false;
            }

            return Parchment.bookManager.HasSeenPage(context.Player, bookId, chapterId, pageId);
        }

        private bool HasSeenChapterlessPageId(string[] query, GameStateQueryContext context)
        {
            if (ArgUtility.TryGet(query, 1, out string bookId, out string error) is false)
            {
                return false;
            }
            if (ArgUtility.TryGet(query, 2, out string pageId, out error) is false)
            {
                return false;
            }

            return Parchment.bookManager.HasSeenChapterlessPage(context.Player, bookId, pageId);
        }

        private bool HasSeenChapterId(string[] query, GameStateQueryContext context)
        {
            if (ArgUtility.TryGet(query, 1, out string bookId, out string error) is false)
            {
                return false;
            }
            if (ArgUtility.TryGet(query, 2, out string chapterId, out error) is false)
            {
                return false;
            }

            return Parchment.bookManager.HasSeenChapter(context.Player, bookId, chapterId);
        }

        private bool CurrentPageIndex(string[] query, GameStateQueryContext context)
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

        /// <summary>Whether either page on screen carries any of the given tags.</summary>
        private bool CurrentPageHasTag(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(query, 1, out string _, out string error, name: "string tag") is false)
            {
                return false;
            }

            for (int index = 1; index < query.Length; index++)
            {
                if (bookMenu.IsOnPageTagged(query[index]) is true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether a named page carries any of the given tags. The page is looked up across the whole book, so a contents entry can ask about a page the reader isn't on.</summary>
        private bool PageHasTag(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(query, 1, out string pageId, out string error, name: "string pageId") is false)
            {
                return false;
            }

            if (bookMenu.FindPageData(pageId) is not PageData pageData)
            {
                return false;
            }

            for (int index = 2; index < query.Length; index++)
            {
                if (pageData.HasTag(query[index]) is true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether the text typed into an input appears in any of a named page's tags, which is what a searchable contents page filters its entries with.
        /// An empty input matches every tagged page, so an untouched search box leaves the whole contents showing. A page with no tags never matches.
        /// </summary>
        private bool PageTagMatchesInput(string[] query, GameStateQueryContext context)
        {
            if (TryGetBookMenu(out BookMenu bookMenu) is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(query, 1, out string pageId, out string error, name: "string pageId") is false || ArgUtility.TryGet(query, 2, out string inputId, out error, name: "string inputId") is false)
            {
                return false;
            }

            if (bookMenu.FindPageData(pageId) is not PageData pageData)
            {
                return false;
            }

            return pageData.HasTagMatching(Parchment.inputManager.GetText(inputId));
        }

        /// <summary>Whether the text typed into an input appears in the given text, which is what a search box filters a list with.
        /// An empty input matches everything, so an untouched search box shows the whole list rather than nothing. Everything past the input ID is treated as one piece of text, so a phrase needs no quoting.
        /// </summary>
        private bool InputMatches(string[] query, GameStateQueryContext context)
        {
            if (ArgUtility.TryGet(query, 1, out string inputId, out string error, name: "string inputId") is false)
            {
                return false;
            }

            string typedText = Parchment.inputManager.GetText(inputId);
            if (string.IsNullOrEmpty(typedText) is true)
            {
                return true;
            }

            return string.Join(" ", query.Skip(2)).Contains(typedText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Whether an input's text is exactly one of the given values.</summary>
        private bool InputEquals(string[] query, GameStateQueryContext context)
        {
            if (ArgUtility.TryGet(query, 1, out string inputId, out string error, name: "string inputId") is false)
            {
                return false;
            }

            string typedText = Parchment.inputManager.GetText(inputId);

            for (int index = 2; index < query.Length; index++)
            {
                if (typedText.EqualsIgnoreCase(query[index]) is true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether an input has anything typed into it, for a "no results" line or a clear button.</summary>
        private bool HasInputText(string[] query, GameStateQueryContext context)
        {
            if (ArgUtility.TryGet(query, 1, out string inputId, out string error, name: "string inputId") is false)
            {
                return false;
            }

            return string.IsNullOrEmpty(Parchment.inputManager.GetText(inputId)) is false;
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
