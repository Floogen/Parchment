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

        public const string SET_FLAG = "PeacefulEnd.Parchment_SetFlag";
        public const string CLEAR_FLAG = "PeacefulEnd.Parchment_ClearFlag";

        public const string MARK_SEEN = "PeacefulEnd.Parchment_MarkSeen";
        public const string CLEAR_SEEN = "PeacefulEnd.Parchment_ClearSeen";

        public const string SET_VARIABLE = "PeacefulEnd.Parchment_SetVariable";
        public const string CLEAR_VARIABLE = "PeacefulEnd.Parchment_ClearVariable";
        public const string TOGGLE_VARIABLE = "PeacefulEnd.Parchment_ToggleVariable";

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

            TriggerActionManager.RegisterAction(SET_FLAG, SetFlag);
            TriggerActionManager.RegisterAction(CLEAR_FLAG, ClearFlag);

            TriggerActionManager.RegisterAction(MARK_SEEN, MarkSeen);
            TriggerActionManager.RegisterAction(CLEAR_SEEN, ClearSeen);

            TriggerActionManager.RegisterAction(SET_VARIABLE, SetVariable);
            TriggerActionManager.RegisterAction(CLEAR_VARIABLE, ClearVariable);
            TriggerActionManager.RegisterAction(TOGGLE_VARIABLE, ToggleVariable);
        }

        public bool GoToStart(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetSkipAnimation(args, 1, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPage(0, out error, skipAnimation);
        }

        public bool NextPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetSkipAnimation(args, 1, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryTurnPage(forward: true, out error, skipAnimation);
        }

        public bool PreviousPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetSkipAnimation(args, 1, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryTurnPage(forward: false, out error, skipAnimation);
        }

        /// <summary>Returns to wherever the reader came from, rather than to the spread before this one. Calling it again goes back a further step.</summary>
        public bool GoBack(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetSkipAnimation(args, 1, out bool skipAnimation, out error) is false)
            {
                return false;
            }

            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryGoBack(out error, skipAnimation);
        }

        public bool JumpToPage(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGetInt(args, 1, out int pageIndex, out error, name: "int pageIndex") is false)
            {
                return false;
            }

            if (TryGetSkipAnimation(args, 2, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPage(pageIndex, out error, skipAnimation);
        }

        public bool JumpToChapter(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string chapterId, out error) is false)
            {
                return false;
            }

            if (TryGetSkipAnimation(args, 2, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToChapter(chapterId, out error, skipAnimation);
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

            if (TryGetSkipAnimation(args, 3, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToChapterPage(chapterId, pageInChapter, out error, skipAnimation);
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

            if (TryGetSkipAnimation(args, 3, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPageId(chapterId, pageId, out error, skipAnimation);
        }

        public bool FirstPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetSkipAnimation(args, 1, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToFirstPage(out error, skipAnimation);
        }

        public bool LastPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetSkipAnimation(args, 1, out bool skipAnimation, out error) is false || TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToLastPage(out error, skipAnimation);
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

        /// <summary>Sets one or more flags for the rest of the reading session. Setting a flag that is already set does nothing.</summary>
        public bool SetFlag(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string _, out error, name: "string flag") is false)
            {
                return false;
            }

            for (int index = 1; index < args.Length; index++)
            {
                Parchment.flagManager.Set(args[index]);
            }

            error = null;

            return true;
        }

        /// <summary>Clears one or more flags. Clearing a flag that was never set does nothing.</summary>
        public bool ClearFlag(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string _, out error, name: "string flag") is false)
            {
                return false;
            }

            for (int index = 1; index < args.Length; index++)
            {
                Parchment.flagManager.Clear(args[index]);
            }

            error = null;

            return true;
        }

        /// <summary>Marks a chapter as read, and a page too when one is given. Pass "" as the chapter for a page that has none.</summary>
        public bool MarkSeen(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string bookId, out error, name: "string bookId") is false)
            {
                return false;
            }

            if (ArgUtility.TryGet(args, 2, out string chapterId, out error, allowBlank: true, name: "string chapterId") is false)
            {
                return false;
            }

            if (ArgUtility.TryGetOptional(args, 3, out string pageId, out error, defaultValue: null, allowBlank: true, name: "string pageId") is false)
            {
                return false;
            }

            if (string.IsNullOrEmpty(chapterId) is false)
            {
                Parchment.bookManager.SetSeenChapter(Game1.player, bookId, chapterId);
            }

            if (string.IsNullOrEmpty(pageId) is false)
            {
                Parchment.bookManager.SetSeenPage(Game1.player, bookId, chapterId, pageId);
            }

            error = null;

            return true;
        }

        /// <summary>Forgets what the player has read, all of it or one book's worth, so the next reading counts as the first.</summary>
        public bool ClearSeen(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGetOptional(args, 1, out string bookId, out error, defaultValue: null, allowBlank: true, name: "string bookId") is false)
            {
                return false;
            }

            Parchment.bookManager.ClearSeen(Game1.player, string.IsNullOrEmpty(bookId) is true ? null : bookId);
            error = null;

            return true;
        }

        /// <summary>Sets a variable the open book declares. Everything past the ID counts as the value, so a phrase needs no quoting.</summary>
        public bool SetVariable(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string variableId, out error, name: "string variableId") is false)
            {
                return false;
            }

            if (TryGetCurrentBookId(out string bookId, out error) is false)
            {
                return false;
            }

            return Parchment.variableManager.TrySet(bookId, variableId, string.Join(" ", args.Skip(2)), out error);
        }

        /// <summary>Returns one or more variables to their declared defaults. A declared variable has no absent state, so this resets rather than removes.</summary>
        public bool ClearVariable(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string _, out error, name: "string variableId") is false || TryGetCurrentBookId(out string bookId, out error) is false)
            {
                return false;
            }

            for (int index = 1; index < args.Length; index++)
            {
                if (Parchment.variableManager.TryClear(bookId, args[index], out error) is false)
                {
                    return false;
                }
            }

            error = null;

            return true;
        }

        /// <summary>Flips one or more Boolean variables, which is what a checkbox needs rather than a pair of conditioned SetVariable buttons.</summary>
        public bool ToggleVariable(string[] args, TriggerActionContext context, out string error)
        {
            if (ArgUtility.TryGet(args, 1, out string _, out error, name: "string variableId") is false || TryGetCurrentBookId(out string bookId, out error) is false)
            {
                return false;
            }

            for (int index = 1; index < args.Length; index++)
            {
                if (Parchment.variableManager.TryToggle(bookId, args[index], out error) is false)
                {
                    return false;
                }
            }

            error = null;

            return true;
        }

        /// <summary>The book a variable action is speaking about. Variables belong to a book, so these fail with no book open rather than guessing.</summary>
        private bool TryGetCurrentBookId(out string bookId, out string error)
        {
            if (Parchment.variableManager.TryGetCurrentBookId(out bookId) is false)
            {
                error = "no book is open, and a variable belongs to the book that declares it";
                return false;
            }

            error = null;

            return true;
        }

        /// <summary>Reads the optional trailing flag every navigation action ends with, which lands the reader on the target spread without playing the turn.</summary>
        private bool TryGetSkipAnimation(string[] args, int index, out bool skipAnimation, out string error)
        {
            return ArgUtility.TryGetOptionalBool(args, index, out skipAnimation, out error, defaultValue: false, name: "bool skipAnimation");
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
