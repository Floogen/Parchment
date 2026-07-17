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
        public const string NEXT_PAGE = "PeacefulEnd.Parchment_NextPage";
        public const string PREVIOUS_PAGE = "PeacefulEnd.Parchment_PreviousPage";
        public const string JUMP_TO_PAGE = "PeacefulEnd.Parchment_JumpToPage";
        public const string FIRST_PAGE = "PeacefulEnd.Parchment_FirstPage";
        public const string LAST_PAGE = "PeacefulEnd.Parchment_LastPage";
        public const string CLOSE_BOOK = "PeacefulEnd.Parchment_CloseBook";

        public ActionManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            RegisterAll();
        }

        public void RegisterAll()
        {
            TriggerActionManager.RegisterAction(NEXT_PAGE, NextPage);
            TriggerActionManager.RegisterAction(PREVIOUS_PAGE, PreviousPage);
            TriggerActionManager.RegisterAction(JUMP_TO_PAGE, JumpToPage);
            TriggerActionManager.RegisterAction(CLOSE_BOOK, CloseBook);
            TriggerActionManager.RegisterAction(FIRST_PAGE, FirstPage);
            TriggerActionManager.RegisterAction(LAST_PAGE, LastPage);
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

        public bool FirstPage(string[] args, TriggerActionContext context, out string error)
        {
            if (TryGetBookMenu(out BookMenu bookMenu, out error) is false)
            {
                return false;
            }

            return bookMenu.TryJumpToPage(0, out error);
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

        private static bool TryGetBookMenu(out BookMenu bookMenu, out string error)
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
