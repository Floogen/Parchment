using Microsoft.Xna.Framework;
using Parchment.Framework.API.Builders;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using System;

namespace Parchment.Framework.API
{
    public class ParchmentApi : IParchmentApi
    {
        private readonly string _modId;

        public ParchmentApi(IModInfo mod)
        {
            _modId = mod?.Manifest?.UniqueID ?? "an unknown mod";
        }

        public IBookBuilder CreateBook(string bookId)
        {
            return new BookBuilder(_modId, bookId);
        }

        public bool TryUnregisterBook(string bookId, out string error)
        {
            if (Parchment.bookManager.TryUnregisterBook(_modId, bookId, out error) is false)
            {
                Parchment.monitor.Log($"{_modId} failed to unregister a book, because {error}.", LogLevel.Warn);
                return false;
            }

            return true;
        }

        public bool HasBook(string bookId)
        {
            return Parchment.bookManager.HasBook(bookId);
        }

        public bool TryOpenBook(string bookId, string? chapterId = null)
        {
            if (TryCreateMenu(bookId, out BookMenu menu) is false)
            {
                return false;
            }

            if (chapterId is not null)
            {
                if (menu.TryOpenAtChapter(chapterId, out string chapterError) is false)
                {
                    LogFailure(bookId, chapterError);
                    return false;
                }
            }

            Game1.activeClickableMenu = menu;

            return true;
        }

        public bool TryOpenBookAtPage(string bookId, string? chapterId, int page)
        {
            if (TryCreateMenu(bookId, out BookMenu menu) is false)
            {
                return false;
            }

            string error = string.Empty;
            bool positioned = chapterId is null ? menu.TryOpenAtPage(page, out error) : menu.TryOpenAtChapterPage(chapterId, page, out error);
            if (positioned is false)
            {
                LogFailure(bookId, error);
                return false;
            }

            Game1.activeClickableMenu = menu;

            return true;
        }

        public bool TryOpenBookAtPageId(string bookId, string? chapterId, string pageId)
        {
            if (TryCreateMenu(bookId, out BookMenu menu) is false)
            {
                return false;
            }

            if (menu.TryOpenAtPageId(chapterId, pageId, out string pageError) is false)
            {
                LogFailure(bookId, pageError);
                return false;
            }

            Game1.activeClickableMenu = menu;

            return true;
        }

        [Obsolete("Use TryOpenBookAtPage instead. This overload is kept so mods built against Parchment 1.1.0 keep working.")]
        public bool TryOpenBook(string bookId, string chapterId, int page)
        {
            return TryOpenBookAtPage(bookId, chapterId, page);
        }

        [Obsolete("Use TryOpenBook(string, string) or TryOpenBookAtPageId instead. This overload is kept so mods built against Parchment 1.1.0 keep working.")]
        public bool TryOpenBook(string bookId, string? chapterId, string? pageId)
        {
            if (pageId is null)
            {
                return TryOpenBook(bookId, chapterId);
            }

            return TryOpenBookAtPageId(bookId, chapterId, pageId);
        }

        public bool TryGetVariable(string bookId, string variableId, out string value)
        {
            if (Parchment.variableManager.TryGet(bookId, variableId, out value, out string error) is false)
            {
                Parchment.monitor.Log($"{_modId} failed to read the variable \"{variableId}\", because {error}.", LogLevel.Warn);
                return false;
            }

            return true;
        }

        public bool TrySetVariable(string bookId, string variableId, string value, out string error)
        {
            if (Parchment.variableManager.TrySet(bookId, variableId, value, out error) is false)
            {
                Parchment.monitor.Log($"{_modId} failed to set the variable \"{variableId}\", because {error}.", LogLevel.Warn);
                return false;
            }

            return true;
        }

        public bool TryGetBookBounds(out Rectangle bounds)
        {
            return TryGetBounds(menu => menu.GetBookScreenBounds(), out bounds);
        }

        public bool TryGetLeftPageBounds(out Rectangle bounds)
        {
            return TryGetBounds(menu => menu.GetLeftPageBounds(), out bounds);
        }

        public bool TryGetRightPageBounds(out Rectangle bounds)
        {
            return TryGetBounds(menu => menu.GetRightPageBounds(), out bounds);
        }

        /// <summary>Reads a rectangle off whichever book is open, or reports that none is.</summary>
        private static bool TryGetBounds(Func<BookMenu, Rectangle> getBounds, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;

            if (Game1.activeClickableMenu is not BookMenu menu)
            {
                return false;
            }

            bounds = getBounds(menu);

            return true;
        }

        private static bool TryCreateMenu(string bookId, out BookMenu menu)
        {
            menu = null;

            if (string.IsNullOrWhiteSpace(bookId))
            {
                Parchment.monitor.Log("Failed to open a book: no book ID was given.", LogLevel.Warn);
                return false;
            }

            var book = Parchment.bookManager.CreateBook(bookId);
            if (book is null)
            {
                if (Parchment.bookManager.TryGetValidationError(bookId, out string validationError) is true)
                {
                    Parchment.monitor.Log($"Failed to open book '{bookId}': it was skipped during loading ({validationError}).", LogLevel.Warn);
                }
                else
                {
                    Parchment.monitor.Log($"Failed to open book '{bookId}': no book with that ID is loaded.", LogLevel.Warn);
                }

                return false;
            }
            menu = new BookMenu(book);

            return true;
        }

        private static void LogFailure(string bookId, string error)
        {
            Parchment.monitor.Log($"Failed to open book '{bookId}': {error}", LogLevel.Warn);
        }
    }
}
