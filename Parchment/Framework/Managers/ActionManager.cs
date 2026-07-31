using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Managers
{
    public class ActionManager : BaseManager
    {
        public const string GO_TO_START = "PeacefulEnd.Parchment_GoToStart";
        public const string NEXT_PAGE = "PeacefulEnd.Parchment_NextPage";
        public const string PREVIOUS_PAGE = "PeacefulEnd.Parchment_PreviousPage";
        public const string GO_BACK = "PeacefulEnd.Parchment_GoBack";
        public const string JUMP_TO_PAGE = "PeacefulEnd.Parchment_JumpToPage";
        public const string JUMP_TO_CHAPTER = "PeacefulEnd.Parchment_JumpToChapter";
        public const string JUMP_TO_CHAPTER_PAGE = "PeacefulEnd.Parchment_JumpToChapterPage";
        public const string JUMP_TO_PAGE_ID = "PeacefulEnd.Parchment_JumpToPageId";
        public const string FIRST_PAGE = "PeacefulEnd.Parchment_FirstPage";
        public const string LAST_PAGE = "PeacefulEnd.Parchment_LastPage";
        public const string CLOSE_BOOK = "PeacefulEnd.Parchment_CloseBook";
        public const string VIEW_COVER = "PeacefulEnd.Parchment_ViewCover";

        public const string SET_INPUT = "PeacefulEnd.Parchment_SetInput";
        public const string CLEAR_INPUT = "PeacefulEnd.Parchment_ClearInput";

        public ActionManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            RegisterAll();
        }

        public void RegisterAll()
        {
            TriggerActionManager.RegisterAction(GO_TO_START, GoToStart);
            TriggerActionManager.RegisterAction(NEXT_PAGE, NextPage);
            TriggerActionManager.RegisterAction(PREVIOUS_PAGE, PreviousPage);
            TriggerActionManager.RegisterAction(GO_BACK, GoBack);
            TriggerActionManager.RegisterAction(JUMP_TO_PAGE, JumpToPage);
            TriggerActionManager.RegisterAction(JUMP_TO_CHAPTER, JumpToChapter);
            TriggerActionManager.RegisterAction(JUMP_TO_CHAPTER_PAGE, JumpToChapterPage);
            TriggerActionManager.RegisterAction(JUMP_TO_PAGE_ID, JumpToPageId);
            TriggerActionManager.RegisterAction(CLOSE_BOOK, CloseBook);
            TriggerActionManager.RegisterAction(VIEW_COVER, ViewCover);
            TriggerActionManager.RegisterAction(FIRST_PAGE, FirstPage);
            TriggerActionManager.RegisterAction(LAST_PAGE, LastPage);

            TriggerActionManager.RegisterAction(SET_INPUT, SetInput);
            TriggerActionManager.RegisterAction(CLEAR_INPUT, ClearInput);
        }

        public bool GoToStart(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPage(0, out error);
        }

        public bool NextPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryTurnPage(forward: true, out error);
        }

        public bool PreviousPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryTurnPage(forward: false, out error);
        }

        /// <summary>Returns to wherever the reader came from, rather than to the spread before this one. Calling it again goes back a further step.</summary>
        public bool GoBack(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryGoBack(out error);
        }

        public bool JumpToPage(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGetInt(args, 1, out int pageIndex, out error, name: "int pageIndex") is false)
            {
                return false;
            }

            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPage(pageIndex, out error);
        }

        public bool JumpToChapter(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string chapterId, out error) is false)
            {
                return false;
            }

            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToChapter(chapterId, out error);
        }

        public bool JumpToChapterPage(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string chapterId, out error, name: "string chapterId") is false)
            {
                return false;
            }

            if (ArgUtility.TryGetInt(args, 2, out int pageInChapter, out error, name: "int pageInChapter") is false)
            {
                return false;
            }

            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToChapterPage(chapterId, pageInChapter, out error);
        }

        public bool JumpToPageId(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string pageId, out error, name: "string pageId") is false)
            {
                return false;
            }

            if (ArgUtility.TryGetOptional(args, 2, out string chapterId, out error, defaultValue: null, allowBlank: false, name: "string chapterId") is false)
            {
                return false;
            }

            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPageId(chapterId, pageId, out error);
        }

        public bool FirstPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToFirstPage(out error);
        }

        public bool LastPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToLastPage(out error);
        }

        public bool CloseBook(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            bookMenu.BeginClose();
            error = null;

            return true;
        }

        public bool ViewCover(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryViewCover(out error);
        }

        /// <summary>Replaces an input's text. Everything past the input ID is taken as the text, so a phrase needs no quoting, and giving nothing empties the input.</summary>
        public bool SetInput(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string inputId, out error, name: "string inputId") is false)
            {
                return false;
            }

            Parchment.inputManager.SetText(inputId, string.Join(" ", args.Skip(2)));
            error = null;

            return true;
        }

        /// <summary>Empties an input. The same as SetInput with no text, spelled so a clear button reads as one.</summary>
        public bool ClearInput(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string inputId, out error, name: "string inputId") is false)
            {
                return false;
            }

            Parchment.inputManager.SetText(inputId, string.Empty);
            error = null;

            return true;
        }

        private bool TryGetBookMenu(out BookMenu bookMenu, out string error)
        {
            if (Game1.activeClickableMenu is BookMenu activeBookMenu)
            {
                bookMenu = activeBookMenu;
                error = null;

                return true;
            }

            bookMenu = null;
            error = "no book menu is currently open";

            return false;
        }
    }
}
