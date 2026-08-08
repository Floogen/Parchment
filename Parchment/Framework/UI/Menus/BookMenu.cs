using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Books;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Pages;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Rendering;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.UI.Menus
{
    public class BookMenu : IClickableMenu
    {
        public Book Book { get; private set; }

        /// <summary>Covering shuts the book but stays in the menu, landing in Cover. Closing shuts it and leaves.</summary>
        public enum MenuState { Sliding, Opening, Ready, Turning, Covering, Cover, Closing }
        public MenuState CurrentState { get; private set; } = MenuState.Sliding;

        private float _animationTimer = 0f;
        private int _animationFrame = 0;

        // Curl corners animation state
        private float _previousCornerAnimationTimer = 0f;
        private float _nextCornerAnimationTimer = 0f;
        private int _previousCornerFrame = 0;
        private int _nextCornerFrame = 0;

        private const float BOOK_LAYER_DEPTH = 0.86f;
        private const float CURL_LAYER_DEPTH = 0.99f;

        private BookAppearanceData _appearance;
        private PageCurlData _pageCurl;
        private BookAnimationData _animation;

        // Adjust this for GSQ refresh rate check
        private const int CONDITION_REFRESH_INTERVAL = 6;
        private int _conditionRefreshTimer = CONDITION_REFRESH_INTERVAL;

        // How long the exit button has to be held to leave a page that has taken the button over
        private const float FORCE_CLOSE_HOLD_DURATION = 3000f;
        private float _forceCloseHoldTimer = 0f;
        private bool _isExitButtonSuppressed = false;

        private readonly List<Rectangle> _openFrames = new List<Rectangle>();
        private List<Rectangle> _closeFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageCurlFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageTurnFrames = new List<Rectangle>();
        private List<Rectangle> _pageTurnFramesReversed = new List<Rectangle>();

        private Vector2 _currentPosition;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;

        private Rectangle _previousPageHotspot;
        private Rectangle _nextPageHotspot;

        private Color _bookTintColor;
        private List<Page> _pages;

        private int _currentChapterIndex = 0;
        private int _pendingChapterIndex;

        private int _currentSpread = 0;
        private int _pendingSpread;
        private bool _isTurningForward;

        // Where the reader has been this session, most recent last, so GoBack can retrace its way out of a chain of jumps
        private const int HISTORY_LIMIT = 64;
        private readonly List<(int ChapterIndex, int Spread)> _history = new List<(int ChapterIndex, int Spread)>();

        private Element? _hoveredElement;

        /// <summary>The item the cursor is over, or null when whatever it is over isn't about an item.
        /// Declared as a plain field rather than a property because that is what Lookup Anything looks for when it reflects over a custom menu.
        /// </summary>
        public Item? HoveredItem;

        /// <summary>The NPC the cursor is over, named by an "NpcId." tag. Read the same way <see cref="HoveredItem"/> is, and null when nothing under the cursor names one.</summary>
        public NPC? HoveredNpc;

        /// <summary>Every tag on the element the cursor is over, for any mod that wants to read them. Empty when nothing is hovered.</summary>
        public IReadOnlyList<string> HoveredTags = Array.Empty<string>();

        // The hovered element's tooltip with its tokens resolved. Held here rather than resolved in the draw, which runs every frame while a tooltip is only worth resolving when something about it could have moved
        private string? _hoveredDisplayName;
        private string? _hoveredDescription;

        private Element? _focusedElement;
        private InputTextSubscriber? _focusedInput;

        // The throwaway box the on-screen keyboard writes into, alive only while that keyboard is up, and the text it was last seen holding
        private TextBox? _textEntryBox;
        private string _lastTextEntryText = string.Empty;

        private bool _isHoveringLeftPage;
        private bool _isHoveringRightPage;

        private bool _isHoveringPreviousPage;
        private bool _isHoveringNextPage;

        // Everywhere the cursor can be sent, rebuilt whenever it is asked for rather than held between passes, since a condition can take an element away between one step and the next
        private readonly List<SnapTarget> _snapTargets = new List<SnapTarget>();
        private SnapTarget? _snappedTarget;

        private Texture2D? _pageCurlTexture;
        private Texture2D _bookTexture;
        private Texture2D? _bookGrayscaleTexture;

        private readonly bool _previousHudState;

        // The game replaces the active menu by assignment in places such as createQuestionDialogue, so the session can be closed from either the menu's exit or the manager watching for that. This keeps it to once
        private bool _hasEndedSession = false;

        /// <summary>What the owning mod runs when something asks the book to rebuild itself, handed over by the builder that opened this menu.
        /// Null for a book the menu wasn't opened from a builder for, being a registered book (whose callback is found through its registration instead) or one out of a content pack (which has nothing to rebuild from).
        /// </summary>
        private Action? _onRefresh;

        // Guards against a refresh callback whose own actions ask for another refresh, which would otherwise recurse until the stack ran out
        private bool _isRefreshing = false;

        public BookMenu(Book book) : base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: false)
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
            base.xPositionOnScreen = (int)topLeft.X;
            base.yPositionOnScreen = (int)topLeft.Y;

            ApplyBook(book);

            // Only a book being opened starts off screen, as a refresh leaves the reader's book where it already sits
            _currentPosition = _startPosition;

            // Cache HUD state. A book opened straight over another takes on what the first one found, so whichever closes last restores the reader's own setting rather than the hidden one
            _previousHudState = Game1.activeClickableMenu is BookMenu outgoingBook ? outgoingBook._previousHudState : Game1.displayHUD;
            Game1.displayHUD = false;
        }

        /// <summary>Takes on a book, being everything the menu reads out of <see cref="Models.Data.BookData"/> rather than out of the reader's session.
        /// Shared by the constructor and <see cref="TryRefreshBook"/>, so a refreshed book is set up exactly as a freshly opened one is.
        /// Where the book sits is left alone, since that belongs to the reading rather than to the book, and a refresh would otherwise drop it back to the start of its slide.
        /// </summary>
        private void ApplyBook(Book book)
        {
            Book = book;
            _bookTintColor = ResolveBookTintColor(book.Data);
            _pages = book.Pages;

            _appearance = book.Data.Appearance;
            _pageCurl = book.Data.PageCurl;
            _animation = book.Data.Animation;

            _bookTexture = Parchment.modHelper.GameContent.Load<Texture2D>(_appearance.TexturePath);
            _bookGrayscaleTexture = string.IsNullOrWhiteSpace(_appearance.GrayscaleTexturePath) ? null : Parchment.modHelper.GameContent.Load<Texture2D>(_appearance.GrayscaleTexturePath);
            _pageCurlTexture = _pageCurl.IsEnabled ? Parchment.modHelper.GameContent.Load<Texture2D>(_pageCurl.TexturePath) : null;

            _openFrames.Clear();
            for (int frameIndex = 0; frameIndex < _appearance.OpenFrameCount; frameIndex++)
            {
                _openFrames.Add(new Rectangle(_appearance.FrameWidth * frameIndex, 0, _appearance.FrameWidth, _appearance.FrameHeight));
            }
            _closeFrames = Enumerable.Reverse(_openFrames).ToList();

            _pageCurlFrames.Clear();
            if (_pageCurl.IsEnabled is true)
            {
                for (int frameIndex = 0; frameIndex < _pageCurl.FrameCount; frameIndex++)
                {
                    _pageCurlFrames.Add(new Rectangle(_pageCurl.FrameWidth * frameIndex, 0, _pageCurl.FrameWidth, _pageCurl.FrameHeight));
                }
            }

            _pageTurnFrames.Clear();
            for (int frameIndex = _appearance.OpenFrameCount; frameIndex < _appearance.OpenFrameCount + _appearance.TurnFrameCount; frameIndex++)
            {
                _pageTurnFrames.Add(new Rectangle(_appearance.FrameWidth * frameIndex, 0, _appearance.FrameWidth, _appearance.FrameHeight));
            }
            _pageTurnFramesReversed = Enumerable.Reverse(_pageTurnFrames).ToList();

            DetermineSlidePositions();
            DetermineHotspotPositions();
        }

        /// <summary>Puts up an element that has been waiting on a ShowElement, restarting its <see cref="ElementData.Lifetime"/> from now.
        /// Showing one that's already up restarts it rather than adding a second, so repeated presses extend it instead of stacking.
        /// </summary>
        public bool TryShowElement(string elementId, out string error)
        {
            double shownAt = AnimationHelper.GetAnimationTime();
            bool hasShownAny = false;

            foreach (Element element in Book.FindElementsById(elementId))
            {
                if (element.Data.Lifetime is null)
                {
                    continue;
                }

                element.ShownAt = shownAt;
                element.IsVisible = true;
                hasShownAny = true;
            }

            if (hasShownAny is false)
            {
                error = $"the book '{Book.Data.Id}' has no element with the ID '{elementId}' carrying a \"Lifetime\", which is what an element needs to be shown this way";
                return false;
            }

            // The element it belongs to may have been sized out of the layout while it was away
            Book.InvalidateLayout();

            error = null;

            return true;
        }

        /// <summary>Takes the callback that rebuilds this book, handed over by the builder that opened it.</summary>
        public void SetRefreshCallback(Action? onRefresh)
        {
            _onRefresh = onRefresh;
        }

        /// <summary>Asks the owning mod to rebuild the book, which it does by assembling a fresh builder and calling TryRefresh on it.
        /// The rebuild is the mod's own work, as a builder holds the values it was given rather than the code that produced them, so Parchment has nothing to recompute on its behalf.
        /// </summary>
        public bool TryRunRefreshCallback(out string error)
        {
            // A registered book is opened from the books asset rather than from its builder, so its callback comes from the registration
            Action? onRefresh = _onRefresh;
            if (onRefresh is null && Parchment.bookManager.TryGetRegisteredRefreshCallback(Book.Data.Id, out Action registeredRefresh) is true)
            {
                onRefresh = registeredRefresh;
            }

            if (onRefresh is null)
            {
                error = $"the book '{Book.Data.Id}' has no refresh callback, which is set through the C# builder's OnRefresh before the book is opened or registered";
                return false;
            }

            if (_isRefreshing is true)
            {
                error = "a refresh is already running";
                return false;
            }

            _isRefreshing = true;

            try
            {
                onRefresh();
            }
            finally
            {
                _isRefreshing = false;
            }

            error = null;

            return true;
        }

        /// <summary>Swaps in a rebuilt version of the book the reader already has open, keeping them where they were.
        /// This is how content generated in C# responds to something changing under it, as a builder recipe holds the values it was given rather than recomputing them.
        /// The reader is returned to the page they were on by its ID, falling back to the same position when the page is gone and to the start when even that is out of range.
        /// Session state (flags, input text, seen pages) is untouched, as this replaces the book inside the living menu rather than putting up a new one.
        /// A shut cover rebuilds too, as the book's own layers are on screen there, and the book stays shut rather than being opened by the swap.
        /// </summary>
        public bool TryRefreshBook(Book book, out string error)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "the book isn't settled, as it's still sliding, opening, turning or closing";
                return false;
            }

            if (book.Pages.Count is 0)
            {
                error = "the rebuilt book has no pages";
                return false;
            }

            string? previousPageId = GetPageId(GetLeftPageIndex()) ?? GetPageId(GetRightPageIndex());
            int previousChapterIndex = _currentChapterIndex;
            int previousSpread = _currentSpread;

            // Dropped before the pages behind it are, as the element it points at is about to be thrown away
            SetHoveredElement(null);
            ClearInputFocus();

            Book previousBook = Book;

            ApplyBook(book);

            CarryElementState(previousBook, book);

            RestoreReadingPosition(previousPageId, previousChapterIndex, previousSpread);
            RefreshVisiblePages();

            error = null;

            return true;
        }

        /// <summary>Hands the rebuilt book's elements the clocks their counterparts were running on, so a refresh doesn't replay every animation from its first frame or take away something that was only just put up.
        /// A fresh element has no way to know it replaced one mid-cycle, since <see cref="AnimationHelper.RefreshActiveFrames"/> stamps a start time whenever the active frames appear, and on a new element they always do.
        /// </summary>
        private static void CarryElementState(Book previousBook, Book book)
        {
            CarryElementState(previousBook.Underlay, book.Underlay);
            CarryElementState(previousBook.Overlay, book.Overlay);

            // Paired by position, so a rebuild that added or removed pages carries what it can and lets the rest start fresh
            int pageCount = Math.Min(previousBook.Pages.Count, book.Pages.Count);

            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                Page previousPage = previousBook.Pages[pageIndex];
                Page page = book.Pages[pageIndex];

                // A page that moved is a different page, so its elements are left to start over rather than inheriting a stranger's clock
                if (string.Equals(previousPage.Data.Id, page.Data.Id, StringComparison.OrdinalIgnoreCase) is false)
                {
                    continue;
                }

                CarryElementState(previousPage.Elements, page.Elements);
                CarryElementState(previousPage.Background, page.Background);
                CarryElementState(previousPage.Foreground, page.Foreground);
            }
        }

        /// <summary>Walks two element lists together, carrying the state across wherever the pair lines up.
        /// Position is what pairs them, since an element isn't required to carry an Id, and a pair that does carry differing ones is taken as a mismatch rather than trusted.
        /// </summary>
        private static void CarryElementState(IReadOnlyList<Element> previousElements, IReadOnlyList<Element> elements)
        {
            int elementCount = Math.Min(previousElements.Count, elements.Count);

            for (int index = 0; index < elementCount; index++)
            {
                Element previousElement = previousElements[index];
                Element element = elements[index];

                if (previousElement.Data.Type != element.Data.Type || HasMatchingId(previousElement, element) is false)
                {
                    continue;
                }

                element.AnimationStartedAt = previousElement.AnimationStartedAt;
                element.HoverAnimationStartedAt = previousElement.HoverAnimationStartedAt;

                // Carried too, or a refresh in the same breath as a ShowElement would take the element straight back down
                element.ShownAt = previousElement.ShownAt;
                element.IsVisible = previousElement.IsVisible;

                // Mapped rather than copied, or the first frame of every animation would run its actions again on each refresh
                element.LastPlayedFrame = MapPlayedFrame(previousElement, element);

                CarryElementState(previousElement.Children, element.Children);
                CarryElementState(previousElement.Background, element.Background);
                CarryElementState(previousElement.Foreground, element.Foreground);
            }
        }

        /// <summary>Finds the rebuilt element's counterpart to the frame its predecessor last played, being the same position in the same list rather than the same object.
        /// Frames belong to the data and a rebuild produces its own, so carrying the reference across would never match what <see cref="DispatchFrameActions(IReadOnlyList{Element})"/> compares against.
        /// </summary>
        private static AnimationFrameData? MapPlayedFrame(Element previousElement, Element element)
        {
            if (previousElement.LastPlayedFrame is not AnimationFrameData playedFrame)
            {
                return null;
            }

            if (TryMapFrame(previousElement.ActiveFrames, element.ActiveFrames, playedFrame, out AnimationFrameData? mappedFrame) is true)
            {
                return mappedFrame;
            }

            return TryMapFrame(previousElement.ActiveHoverFrames, element.ActiveHoverFrames, playedFrame, out mappedFrame) is true ? mappedFrame : null;
        }

        /// <summary>Finds the frame sitting where the played one sat. A list that has changed length under it can leave the position unreachable, in which case the animation is treated as never having played.</summary>
        private static bool TryMapFrame(List<AnimationFrameData>? previousFrames, List<AnimationFrameData>? frames, AnimationFrameData playedFrame, out AnimationFrameData? mappedFrame)
        {
            mappedFrame = null;

            if (previousFrames is null || frames is null)
            {
                return false;
            }

            int frameIndex = previousFrames.IndexOf(playedFrame);

            if (frameIndex < 0 || frameIndex >= frames.Count)
            {
                return false;
            }

            mappedFrame = frames[frameIndex];

            return true;
        }

        /// <summary>Whether two elements at the same position agree on their Id. Two without one are taken as the same element, as position is all there is to go on.</summary>
        private static bool HasMatchingId(Element previousElement, Element element)
        {
            if (string.IsNullOrWhiteSpace(previousElement.Data.Id) is true && string.IsNullOrWhiteSpace(element.Data.Id) is true)
            {
                return true;
            }

            return string.Equals(previousElement.Data.Id, element.Data.Id, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Puts the reader back where they were after a refresh, preferring the page they were reading over the position it happened to sit at.</summary>
        private void RestoreReadingPosition(string? pageId, int chapterIndex, int spread)
        {
            if (pageId is not null)
            {
                int pageIndex = FindPageIndex(pageId, chapterFilter: -1);

                if (pageIndex >= 0)
                {
                    _currentChapterIndex = Book.GetChapterIndexForPage(pageIndex);
                    _currentSpread = (pageIndex - GetChapter(_currentChapterIndex).FirstPageIndex) / PagesPerSpread;

                    return;
                }
            }

            // The page is gone, so the same place in the book is the next best thing
            _currentChapterIndex = Math.Clamp(chapterIndex, 0, Book.Chapters.Count - 1);
            _currentSpread = Math.Clamp(spread, 0, GetSpreadCount() - 1);
        }

        // Public methods for game state queries
        public bool IsPagingForward()
        {
            return CurrentState is MenuState.Turning && _isTurningForward;
        }

        public bool IsHoveringLeftPage()
        {
            return _isHoveringLeftPage;
        }

        public bool IsHoveringRightPage()
        {
            return _isHoveringRightPage;
        }

        public bool IsOnPage(int pageIndex)
        {
            return pageIndex == GetLeftPageIndex() || pageIndex == GetRightPageIndex();
        }

        /// <summary>The data behind a page, found by its ID across the whole book. Null when no page carries that ID.</summary>
        public PageData? FindPageData(string pageId)
        {
            int pageIndex = FindPageIndex(pageId, chapterFilter: -1);

            return pageIndex < 0 ? null : _pages[pageIndex].Data;
        }

        /// <summary>Whether either page on screen carries a tag.</summary>
        public bool IsOnPageTagged(string tag)
        {
            return HasTag(GetLeftPageIndex(), tag) || HasTag(GetRightPageIndex(), tag);
        }

        private bool HasTag(int pageIndex, string tag)
        {
            return pageIndex < _pages.Count && _pages[pageIndex] is not null && _pages[pageIndex].Data.HasTag(tag);
        }

        public bool IsOnPage(string pageId)
        {
            return string.Equals(GetPageId(GetLeftPageIndex()), pageId, StringComparison.OrdinalIgnoreCase) || string.Equals(GetPageId(GetRightPageIndex()), pageId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Whether there is anywhere for <see cref="TryGoBack"/> to return to, which is what a back button hides itself on.</summary>
        public bool CanGoBack()
        {
            return _history.Any(IsWithinBook);
        }

        public bool IsInChapter(string chapterId)
        {
            var chapter = GetChapter(_currentChapterIndex);
            if (chapter is null)
            {
                return false;
            }

            return string.Equals(chapter.Id, chapterId, StringComparison.OrdinalIgnoreCase);
        }

        // Public methods for action usage
        public bool TryTurnPage(bool forward, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "The book is not ready";
                return false;
            }

            int targetSpread = forward ? _currentSpread + 1 : _currentSpread - 1;

            if (targetSpread < 0 || targetSpread >= GetSpreadCount())
            {
                error = $"There is no spread {targetSpread}";
                return false;
            }

            BeginPageTurn(targetSpread, skipAnimation);
            error = null;

            return true;
        }

        /// <summary>Returns to the spread the reader came from, dropping it from the history so a second call goes back a step further.
        /// Going back doesn't record a step of its own, so a book can't trap the reader bouncing between two spreads.
        /// </summary>
        public bool TryGoBack(out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "The book is not ready";
                return false;
            }

            while (_history.Count > 0)
            {
                (int ChapterIndex, int Spread) previous = _history[_history.Count - 1];
                _history.RemoveAt(_history.Count - 1);

                // An entry that no longer addresses anything, or that points at where the reader already is, is dropped and the one beneath it tried instead
                if (IsWithinBook(previous) is false || (previous.ChapterIndex == _currentChapterIndex && previous.Spread == _currentSpread))
                {
                    continue;
                }

                BeginPageTurn(previous.ChapterIndex, previous.Spread, recordHistory: false, skipAnimation: skipAnimation);
                error = null;

                return true;
            }

            error = "there is nowhere to go back to";
            return false;
        }

        public bool TryJumpToChapter(string chapterId, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "The book is not ready";
                return false;
            }

            if (Book.TryGetChapterIndex(chapterId, out int chapterIndex) is false)
            {
                error = $"There is no chapter '{chapterId}'";
                return false;
            }

            BeginJump(chapterIndex, 0, skipAnimation);

            error = null;

            return true;
        }

        public bool TryJumpToPage(int pageIndex, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "The book is not ready";
                return false;
            }

            if (pageIndex < 0 || pageIndex >= _pages.Count)
            {
                error = $"Page index {pageIndex} is out of range (0-{_pages.Count - 1})";
                return false;
            }

            int chapterIndex = Book.GetChapterIndexForPage(pageIndex);
            int targetSpread = (pageIndex - GetChapter(chapterIndex).FirstPageIndex) / PagesPerSpread;

            BeginJump(chapterIndex, targetSpread, skipAnimation);

            error = null;

            return true;
        }

        public bool TryJumpToChapterPage(string chapterId, int pageInChapter, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "The book is not ready";
                return false;
            }

            if (Book.TryGetChapterIndex(chapterId, out int chapterIndex) is false)
            {
                error = $"There is no chapter '{chapterId}'";
                return false;
            }

            Chapter chapter = GetChapter(chapterIndex);
            int pageIndex = chapter.FirstPageIndex + pageInChapter;

            if (pageInChapter < 0 || pageIndex > chapter.LastPageIndex)
            {
                error = $"Chapter '{chapterId}' has no page {pageInChapter}";
                return false;
            }

            int targetSpread = pageInChapter / PagesPerSpread;

            BeginJump(chapterIndex, targetSpread, skipAnimation);

            error = null;

            return true;
        }

        public bool TryJumpToPageId(string pageId, out string error, bool skipAnimation = false)
        {
            return TryJumpToPageId(null, pageId, out error, skipAnimation);
        }

        public bool TryJumpToPageId(string chapterId, string pageId, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                error = "The book is not ready";
                return false;
            }

            if (string.IsNullOrWhiteSpace(pageId))
            {
                error = "No page ID was provided";
                return false;
            }

            int chapterFilter = -1;
            if (chapterId is not null)
            {
                if (Book.TryGetChapterIndex(chapterId, out chapterFilter) is false)
                {
                    error = $"There is no chapter '{chapterId}'";
                    return false;
                }
            }

            int pageIndex = FindPageIndex(pageId, chapterFilter);
            if (pageIndex < 0)
            {
                error = chapterFilter >= 0 ? $"Chapter '{chapterId}' has no page '{pageId}'" : $"There is no page '{pageId}'";
                return false;
            }

            return TryJumpToPage(pageIndex, out error, skipAnimation);
        }

        public bool TryJumpToFirstPage(out string error, bool skipAnimation = false)
        {
            return TryJumpToPage(GetChapter(_currentChapterIndex).FirstPageIndex, out error, skipAnimation);
        }

        public bool TryJumpToLastPage(out string error, bool skipAnimation = false)
        {
            return TryJumpToPage(GetChapter(_currentChapterIndex).LastPageIndex, out error, skipAnimation);
        }

        /// <summary>Positions the book on a page by its index within the whole book, before the menu is shown.</summary>
        public bool TryOpenAtPage(int pageIndex, out string error)
        {
            if (pageIndex < 0 || pageIndex >= _pages.Count)
            {
                error = $"Page index {pageIndex} is out of range (0-{_pages.Count - 1})";
                return false;
            }

            int chapterIndex = Book.GetChapterIndexForPage(pageIndex);
            int spread = (pageIndex - GetChapter(chapterIndex).FirstPageIndex) / PagesPerSpread;

            ApplyInitialSpread(chapterIndex, spread);
            error = null;

            return true;
        }

        public bool TryOpenAtChapter(string chapterId, out string error)
        {
            if (Book.TryGetChapterIndex(chapterId, out int chapterIndex) is false)
            {
                error = $"There is no chapter '{chapterId}'";
                return false;
            }

            ApplyInitialSpread(chapterIndex, 0);
            error = null;
            return true;
        }

        public bool TryOpenAtChapterPage(string chapterId, int pageInChapter, out string error)
        {
            if (Book.TryGetChapterIndex(chapterId, out int chapterIndex) is false)
            {
                error = $"There is no chapter '{chapterId}'";
                return false;
            }

            Chapter chapter = GetChapter(chapterIndex);
            int pageIndex = chapter.FirstPageIndex + pageInChapter;

            if (pageInChapter < 0 || pageIndex > chapter.LastPageIndex)
            {
                error = $"Chapter '{chapterId}' has no page {pageInChapter}";
                return false;
            }

            ApplyInitialSpread(chapterIndex, pageInChapter / PagesPerSpread);
            error = null;
            return true;
        }
        public bool TryOpenAtPageId(string pageId, out string error)
        {
            return TryOpenAtPageId(null, pageId, out error);
        }

        public bool TryOpenAtPageId(string chapterId, string pageId, out string error)
        {
            if (string.IsNullOrWhiteSpace(pageId))
            {
                error = "No page ID was provided";
                return false;
            }

            int chapterFilter = -1;
            if (chapterId is not null)
            {
                if (Book.TryGetChapterIndex(chapterId, out chapterFilter) is false)
                {
                    error = $"There is no chapter '{chapterId}'";
                    return false;
                }
            }

            int pageIndex = FindPageIndex(pageId, chapterFilter);
            if (pageIndex < 0)
            {
                error = $"There is no page '{pageId}'";
                if (chapterFilter >= 0)
                {
                    error = $"Chapter '{chapterId}' has no page '{pageId}'";
                }

                return false;
            }

            int chapterIndex = Book.GetChapterIndexForPage(pageIndex);
            int spread = (pageIndex - GetChapter(chapterIndex).FirstPageIndex) / PagesPerSpread;

            ApplyInitialSpread(chapterIndex, spread);
            error = null;

            return true;
        }

        private int FindPageIndex(string pageId, int chapterFilter)
        {
            int start = 0;
            int end = _pages.Count;

            if (chapterFilter >= 0)
            {
                Chapter chapter = GetChapter(chapterFilter);
                start = chapter.FirstPageIndex;
                end = chapter.LastPageIndex + 1;
            }

            for (int i = start; i < end; i++)
            {
                if (string.Equals(GetPageId(i), pageId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ApplyInitialSpread(int chapterIndex, int spread)
        {
            _currentChapterIndex = chapterIndex;
            _currentSpread = spread;

            // If the book has already opened, resync the visible pages
            if (CurrentState is MenuState.Ready)
            {
                RefreshVisiblePages();
            }
        }

        /// <summary>Closes the book. When the book allows it and is still open, this shuts it in place and leaves the menu up, so a
        /// second call is what actually leaves.</summary>
        public void BeginClose()
        {
            if (CurrentState is MenuState.Closing or MenuState.Covering)
            {
                return;
            }

            if (Book.Data.ExitToCover is true && CurrentState is not MenuState.Cover)
            {
                BeginCover();
                return;
            }

            // The book is already shut in Cover, so replaying the close frames would shut it a second time
            if (CurrentState is MenuState.Cover)
            {
                exitThisMenu(playSound: false);
                return;
            }

            SetMenuState(MenuState.Closing);
            PlaySound(_animation.CloseSound);
        }

        /// <summary>Shuts the book but stays in the menu, leaving its cover on screen. Clicking the cover reopens at the same spread.</summary>
        public bool TryViewCover(out string error)
        {
            if (CurrentState is MenuState.Covering or MenuState.Cover)
            {
                error = "the book is already closed";
                return false;
            }

            if (CurrentState is MenuState.Closing)
            {
                error = "the book is closing";
                return false;
            }

            BeginCover();
            error = null;

            return true;
        }

        private void BeginCover()
        {
            SetMenuState(MenuState.Covering);
            PlaySound(_animation.CloseSound);
        }

        /// <summary>Reopens from the cover, at the spread the reader left off on.</summary>
        private void BeginReopen()
        {
            SetMenuState(MenuState.Opening);
            PlaySound(_animation.OpenSound);
        }

        /// <summary>Takes the book from the end of its slide, either opening it or holding on the cover until the reader clicks it.</summary>
        private void SettleAfterSlide()
        {
            if (Book.Data.StartOnCover is true)
            {
                SetMenuState(MenuState.Cover);
                return;
            }

            SetMenuState(MenuState.Opening);
            PlaySound(_animation.OpenSound);
        }

        // Start of internal logic
        private void DetermineSlidePositions()
        {
            Rectangle closedBookRectangle = _openFrames[0];

            float centeredX = base.xPositionOnScreen + base.width / 2f - (closedBookRectangle.Width * _appearance.Scale) / 2f;
            float centeredY = base.yPositionOnScreen + base.height / 2f - (closedBookRectangle.Height * _appearance.Scale) / 2f;
            _targetPosition = new Vector2(MathF.Round(centeredX + _appearance.Offset.X * _appearance.Scale), MathF.Round(centeredY + _appearance.Offset.Y * _appearance.Scale));
            _startPosition = new Vector2(_targetPosition.X, Game1.uiViewport.Height + (closedBookRectangle.Height * _appearance.Scale));
        }

        /// <summary>The whole book frame's bounds on screen.</summary>
        /// <remarks>Taken from the book's resting position, so this stays put while the open and close animations play rather than tracking the book as it slides.</remarks>
        public Rectangle GetBookScreenBounds()
        {
            Rectangle bookFrame = _openFrames[0];

            return new Rectangle((int)_targetPosition.X, (int)_targetPosition.Y, (int)(bookFrame.Width * _appearance.Scale), (int)(bookFrame.Height * _appearance.Scale));
        }

        private void DetermineHotspotPositions()
        {
            // An empty rect contains nothing, so a book without corners falls through the click and hover checks without either of them having to know about it
            if (_pageCurl.IsEnabled is false)
            {
                _previousPageHotspot = Rectangle.Empty;
                _nextPageHotspot = Rectangle.Empty;

                return;
            }

            Rectangle bookBounds = GetBookScreenBounds();

            _previousPageHotspot = GetCurlBounds(_pageCurl.PreviousPageOffset, bookBounds);
            _nextPageHotspot = GetCurlBounds(_pageCurl.NextPageOffset, bookBounds);
        }

        /// <summary>The screen rect of a page curl corner. This is both the drawn sprite's rect and its hotspot, so the two cannot drift apart.</summary>
        private Rectangle GetCurlBounds(Point spriteSpaceOffset, Rectangle bookBounds)
        {
            return new Rectangle(
                bookBounds.X + (int)(spriteSpaceOffset.X * _appearance.Scale),
                bookBounds.Y + (int)(spriteSpaceOffset.Y * _appearance.Scale),
                (int)(_pageCurl.FrameWidth * _pageCurl.Scale),
                (int)(_pageCurl.FrameHeight * _pageCurl.Scale));
        }

        private Chapter GetChapter(int chapterIndex)
        {
            return Book.Chapters[chapterIndex];
        }

        private int GetSpreadCount()
        {
            return GetChapter(_currentChapterIndex).SpreadCount;
        }

        private int GetLeftPageIndex()
        {
            return GetPageIndex(_currentChapterIndex, _currentSpread, left: true);
        }

        private int GetRightPageIndex()
        {
            return GetPageIndex(_currentChapterIndex, _currentSpread, left: false);
        }

        /// <summary>How many pages one spread holds, being one for a book that shows a single page at a time and two for one with a spine.</summary>
        private int PagesPerSpread => Book.Data.Layout.IsSinglePage ? 1 : 2;

        private int GetPageIndex(int chapterIndex, int spread, bool left)
        {
            // A single page book has no right page, so asking for one is out of range in the same way as asking past the end of a chapter, which every caller already handles
            if (left is false && Book.Data.Layout.IsSinglePage is true)
            {
                return int.MaxValue;
            }

            Chapter chapter = GetChapter(chapterIndex);
            int pageIndex = chapter.FirstPageIndex + spread * PagesPerSpread + (left ? 0 : 1);

            return pageIndex > chapter.LastPageIndex ? int.MaxValue : pageIndex;
        }

        private string? GetPageId(int pageIndex)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return null;
            }

            return _pages[pageIndex].Data.Id;
        }

        /// <summary>The left page's content area on screen, inside the book's margins.</summary>
        public Rectangle GetLeftPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();

            int marginOuter = (int)(Book.Data.Layout.MarginOuter * _appearance.Scale);
            int marginTop = (int)(Book.Data.Layout.MarginTop * _appearance.Scale);

            // Shared with the builder's measurement, so a page can't be measured against a size it won't be drawn at
            Point pageSize = PageLayoutHelper.GetPageContentSize(bookBounds.Width, bookBounds.Height, Book.Data.Layout, _appearance.Scale);

            return new Rectangle(bookBounds.X + marginOuter, bookBounds.Y + marginTop, pageSize.X, pageSize.Y);
        }

        /// <summary>The right page's content area on screen, inside the book's margins. Empty for a book showing a single page at a time, which has no right page.</summary>
        public Rectangle GetRightPageBounds()
        {
            if (Book.Data.Layout.IsSinglePage is true)
            {
                return Rectangle.Empty;
            }

            Rectangle bookBounds = GetBookScreenBounds();
            int spineX = bookBounds.X + bookBounds.Width / 2;

            int marginTop = (int)(Book.Data.Layout.MarginTop * _appearance.Scale);
            int marginSpine = (int)(Book.Data.Layout.MarginSpine * _appearance.Scale);

            Point pageSize = PageLayoutHelper.GetPageContentSize(bookBounds.Width, bookBounds.Height, Book.Data.Layout, _appearance.Scale);

            return new Rectangle(spineX + marginSpine, bookBounds.Y + marginTop, pageSize.X, pageSize.Y);
        }

        private void UpdateCornerAnimation(ref float animationTimer, ref int currentFrame, bool isHovering, float elapsedMilliseconds)
        {
            if (_pageCurlFrames.Count is 0)
            {
                return;
            }

            int lastFrame = _pageCurlFrames.Count - 1;

            float frameDuration = _animation.CurlDuration / _pageCurlFrames.Count;
            if (isHovering && currentFrame < lastFrame)
            {
                animationTimer += elapsedMilliseconds;
                if (animationTimer >= frameDuration)
                {
                    currentFrame++;
                    animationTimer = 0f;
                }
            }
            else if (!isHovering && currentFrame > 0)
            {
                animationTimer += elapsedMilliseconds;
                if (animationTimer >= frameDuration)
                {
                    currentFrame--;
                    animationTimer = 0f;
                }
            }
            else
            {
                animationTimer = 0f;
            }
        }

        /// <summary>Sends the reader to a spread, or leaves them where they are when that spread is the one they're already reading.
        /// A shut book has no spread to be on, so a jump from the cover always goes through and opens the book onto its target.
        /// </summary>
        private void BeginJump(int targetChapterIndex, int targetSpread, bool skipAnimation)
        {
            if (CurrentState is not MenuState.Cover && targetChapterIndex == _currentChapterIndex && targetSpread == _currentSpread)
            {
                return;
            }

            BeginPageTurn(targetChapterIndex, targetSpread, skipAnimation: skipAnimation);
        }

        /// <param name="skipAnimation">Whether to land on the target spread immediately rather than playing the turn. Nothing is drawn turning and nothing is heard, since neither belongs to a swap the reader didn't watch happen.</param>
        private void BeginPageTurn(int targetChapterIndex, int targetSpread, bool recordHistory = true, bool skipAnimation = false)
        {
            if (recordHistory)
            {
                PushHistory();
            }

            _isTurningForward = targetChapterIndex != _currentChapterIndex ? targetChapterIndex > _currentChapterIndex : targetSpread > _currentSpread;
            _pendingChapterIndex = targetChapterIndex;
            _pendingSpread = targetSpread;

            // A shut book has no spread to turn from, so the target is taken up on the spot and the open animation carries the reader in rather than the turn
            if (CurrentState is MenuState.Cover)
            {
                CommitPageTurn();

                if (skipAnimation is true)
                {
                    SetMenuState(MenuState.Ready);
                    return;
                }

                BeginReopen();
                return;
            }

            if (skipAnimation is false)
            {
                // A book with no turn frames has nothing to draw turning, so it lands where the animation would have left it. Still heard, unlike a skipped turn, since this one is a turn the reader asked for
                if (_pageTurnFrames.Count is 0)
                {
                    CommitPageTurn();
                    SetMenuState(MenuState.Ready);

                    PlaySound(_animation.TurnSound);
                    return;
                }

                SetMenuState(MenuState.Turning);

                PlaySound(_animation.TurnSound);
                return;
            }

            // The same landing the turn animation reaches when it finishes, and the same one a click skips to
            CommitPageTurn();
            SetMenuState(MenuState.Ready);
        }

        private void BeginPageTurn(int targetSpread, bool skipAnimation = false)
        {
            BeginPageTurn(_currentChapterIndex, targetSpread, skipAnimation: skipAnimation);
        }

        private void BeginPageTurn(bool forward)
        {
            BeginPageTurn(forward ? _currentSpread + 1 : _currentSpread - 1);
        }

        private void CommitPageTurn()
        {
            _currentChapterIndex = _pendingChapterIndex;
            _currentSpread = _pendingSpread;
        }

        /// <summary>Records where the reader is standing, before a turn takes them somewhere else.</summary>
        private void PushHistory()
        {
            _history.Add((_currentChapterIndex, _currentSpread));

            // A book with a lot of cross-linking would otherwise grow this for as long as it stays open
            if (_history.Count > HISTORY_LIMIT)
            {
                _history.RemoveAt(0);
            }
        }

        /// <summary>Whether a recorded spread still addresses somewhere in this book.</summary>
        private bool IsWithinBook((int ChapterIndex, int Spread) entry)
        {
            if (entry.ChapterIndex < 0 || entry.ChapterIndex >= Book.Chapters.Count)
            {
                return false;
            }

            return entry.Spread >= 0 && entry.Spread < GetChapter(entry.ChapterIndex).SpreadCount;
        }

        private void RefreshVisiblePages()
        {
            _conditionRefreshTimer = 0;

            Book.RefreshConditions();

            RefreshPageConditions(GetLeftPageIndex());
            RefreshPageConditions(GetRightPageIndex());

            RefreshHoverText();

            // Last, so a controller's cursor is put back onto something that survived whatever the pass above changed
            RefreshSnap();
        }

        private void RefreshPageConditions(int pageIndex)
        {
            if (pageIndex >= _pages.Count)
            {
                return;
            }

            _pages[pageIndex].RefreshConditions();
        }

        private Element? GetElementAt(Point screenPosition)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover and not MenuState.Turning)
            {
                return null;
            }

            Rectangle bookBounds = GetBookScreenBounds();

            Element? hitElement = Page.HitTest(Book.Overlay, bookBounds, screenPosition);

            // There are no pages to hit while the book is shut, only the book's own layers
            if (CurrentState is MenuState.Ready)
            {
                hitElement ??= HitTestPage(GetLeftPageIndex(), GetLeftPageBounds(), screenPosition);
                hitElement ??= HitTestPage(GetRightPageIndex(), GetRightPageBounds(), screenPosition);
            }

            return hitElement ?? Page.HitTest(Book.Underlay, bookBounds, screenPosition);
        }

        private Element? HitTestPage(int pageIndex, Rectangle pageBounds, Point screenPosition)
        {
            if (pageIndex >= _pages.Count)
            {
                return null;
            }

            Page page = _pages[pageIndex];

            // Topmost first, mirroring the draw order in DrawPage. The absolutely positioned layers only claim the cursor when the element under it has a description, display name or action, so decorative art doesn't cover the stacked elements.
            Element? hitElement = Page.HitTest(page.Foreground, pageBounds, screenPosition, interactiveOnly: true);
            hitElement ??= Page.HitTest(page.Elements, pageBounds, screenPosition);

            return hitElement ?? Page.HitTest(page.Background, pageBounds, screenPosition, interactiveOnly: true);
        }

        // The vanilla direction values, as they arrive through applyMovementKey
        private const int DIRECTION_UP = 0;
        private const int DIRECTION_RIGHT = 1;
        private const int DIRECTION_DOWN = 2;
        private const int DIRECTION_LEFT = 3;

        /// <summary>How much being off to the side of the direction counts against a target, measured against the distance in it.
        /// Above one, so a step goes to what lines up rather than to whatever happens to be nearest, which is what keeps a column being walked down from wandering into the next one.
        /// </summary>
        private const float SNAP_ACROSS_WEIGHT = 2f;

        /// <summary>Whether the menu is the one moving the cursor. Snappy menus is what takes the stick off the cursor, so without it the game is already moving the cursor and a click lands wherever a mouse would have put it.</summary>
        private static bool IsSnappingActive()
        {
            return Game1.options.snappyMenus && Game1.options.gamepadControls;
        }

        /// <summary>Steps the cursor to the next target in a direction, which is what the D-pad and the left stick reach under snappy menus.
        /// Nothing is hovered or clicked here. The cursor is put somewhere and <see cref="performHoverAction"/> takes it from there, so a controller and a mouse arrive at an element by the same route.
        /// </summary>
        public override void applyMovementKey(int direction)
        {
            if (IsSnappingActive() is false || CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                base.applyMovementKey(direction);
                return;
            }

            // A focused input has the stick for the same reason it has the keyboard, so the cursor doesn't walk off the box the reader is typing into
            if (_focusedInput is not null)
            {
                return;
            }

            CollectSnapTargets();

            if (_snapTargets.Count is 0)
            {
                return;
            }

            // Where the cursor is standing when nothing has been snapped yet, so a reader who was using the mouse a moment ago carries on from there
            Rectangle origin = _snappedTarget is SnapTarget snappedTarget ? snappedTarget.Bounds : new Rectangle(Game1.getMouseX(true), Game1.getMouseY(true), 1, 1);

            if (TryGetTargetInDirection(origin, direction, out SnapTarget target) is false)
            {
                return;
            }

            ApplySnapTarget(target);
        }

        /// <summary>Puts the cursor on the first thing worth reaching, which the game asks for when a menu comes up under snappy menus.
        /// The book is still sliding on at that point and has no spread to land on, so the opening snap is left to <see cref="RefreshSnap"/> once it settles.
        /// </summary>
        public override void snapToDefaultClickableComponent()
        {
            if (IsSnappingActive() is false || CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                return;
            }

            CollectSnapTargets();

            if (_snapTargets.Count is 0)
            {
                _snappedTarget = null;
                return;
            }

            ApplySnapTarget(_snapTargets[0]);
        }

        public override void setUpForGamePadMode()
        {
            base.setUpForGamePadMode();

            snapToDefaultClickableComponent();
        }

        /// <summary>Keeps the cursor on something that still exists, after a pass that may have taken away whatever it was on.
        /// Run from <see cref="RefreshVisiblePages"/>, so a condition hiding the snapped element, a rebuilt book and a landed page turn are all covered by the one call.
        /// </summary>
        private void RefreshSnap()
        {
            if (IsSnappingActive() is false)
            {
                _snappedTarget = null;
                return;
            }

            // Neither state has anywhere settled to stand, so the cursor is left where it is until the book lands
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
            {
                return;
            }

            CollectSnapTargets();

            if (_snapTargets.Count is 0)
            {
                _snappedTarget = null;
                return;
            }

            // The first target is the top of the spread, which is where a freshly opened book starts
            if (TryResolveSnappedTarget(out SnapTarget target) is false)
            {
                target = _snapTargets[0];
            }

            ApplySnapTarget(target);
        }

        /// <summary>Takes up a target, moving the cursor only when it isn't already standing there, so a refresh that changed nothing doesn't fight a reader who has picked the mouse back up.</summary>
        private void ApplySnapTarget(SnapTarget target)
        {
            bool hasMoved = _snappedTarget is not SnapTarget snappedTarget || snappedTarget.Bounds != target.Bounds;

            _snappedTarget = target;

            if (hasMoved is false)
            {
                return;
            }

            // In UI space, as that is what everything the menu measures against is in
            Game1.setMousePosition(target.Bounds.Center.X, target.Bounds.Center.Y, true);
        }

        /// <summary>Finds the snapped target again in a freshly gathered list, which is most of the work of surviving a refresh.</summary>
        private bool TryResolveSnappedTarget(out SnapTarget target)
        {
            target = default;

            if (_snappedTarget is not SnapTarget snappedTarget)
            {
                return false;
            }

            // The same element, which is the ordinary case, as a condition pass leaves the elements it walked in place
            foreach (SnapTarget candidate in _snapTargets)
            {
                if (candidate.Element is null || ReferenceEquals(candidate.Element, snappedTarget.Element) is false)
                {
                    continue;
                }

                target = candidate;
                return true;
            }

            // A rebuilt book brings its own elements, so the one the reader was on is looked for by its ID rather than by the object it used to be
            if (string.IsNullOrWhiteSpace(snappedTarget.Element?.Data.Id) is false)
            {
                foreach (SnapTarget candidate in _snapTargets)
                {
                    if (string.Equals(candidate.Element?.Data.Id, snappedTarget.Element.Data.Id, StringComparison.OrdinalIgnoreCase) is false)
                    {
                        continue;
                    }

                    target = candidate;
                    return true;
                }
            }

            // Nothing answers to it, so wherever it stood is the next best place to be. This is also what carries a corner across, having no element to be found by
            return TryGetNearestTarget(snappedTarget.Bounds.Center, out target);
        }

        /// <summary>Gathers everywhere the cursor can be sent, in reading order.
        /// The layout is settled here rather than waited on, since this runs from a condition pass that may have resized the very thing it is about to measure and the next draw is too late to be asking.
        /// </summary>
        private void CollectSnapTargets()
        {
            _snapTargets.Clear();

            Rectangle bookBounds = GetBookScreenBounds();
            EnsureBookLayout();

            // There are no pages to reach while the book is shut, only the book's own layers
            if (CurrentState is MenuState.Ready)
            {
                CollectPageSnapTargets(GetLeftPageIndex(), GetLeftPageBounds());
                CollectPageSnapTargets(GetRightPageIndex(), GetRightPageBounds());
            }

            // The book's own layers are on screen whatever is being read, and come after the page so a reader lands in the spread they opened onto rather than on the furniture around it
            Page.CollectTargets(Book.Underlay, bookBounds, _snapTargets);
            Page.CollectTargets(Book.Overlay, bookBounds, _snapTargets);

            if (CurrentState is MenuState.Ready)
            {
                // The corners are hotspots rather than elements, so they are added by hand, and only while there is a page that way, which is the same rule the click and the hover already follow
                if (_previousPageHotspot != Rectangle.Empty && _currentSpread > 0)
                {
                    _snapTargets.Add(new SnapTarget(_previousPageHotspot, null));
                }

                if (_nextPageHotspot != Rectangle.Empty && _currentSpread < GetSpreadCount() - 1)
                {
                    _snapTargets.Add(new SnapTarget(_nextPageHotspot, null));
                }
            }

            // A shut cover carrying nothing is still worth reaching, since clicking it is what opens the book
            if (_snapTargets.Count is 0 && CurrentState is MenuState.Cover)
            {
                _snapTargets.Add(new SnapTarget(bookBounds, null));
            }
        }

        private void CollectPageSnapTargets(int pageIndex, Rectangle pageBounds)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return;
            }

            Page page = _pages[pageIndex];
            EnsureLayout(page, pageBounds);

            Page.CollectTargets(page.Background, pageBounds, _snapTargets);
            Page.CollectTargets(page.Elements, pageBounds, _snapTargets);
            Page.CollectTargets(page.Foreground, pageBounds, _snapTargets);
        }

        /// <summary>The best target to step onto from where the cursor stands, or none when there is nothing that way.</summary>
        private bool TryGetTargetInDirection(Rectangle origin, int direction, out SnapTarget target)
        {
            target = default;

            float bestScore = float.MaxValue;
            bool hasFound = false;

            foreach (SnapTarget candidate in _snapTargets)
            {
                if (candidate.Bounds == origin)
                {
                    continue;
                }

                if (TryScoreTarget(origin, candidate.Bounds, direction, out float score) is false)
                {
                    continue;
                }

                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                target = candidate;
                hasFound = true;
            }

            return hasFound;
        }

        /// <summary>Scores a step onto a target, lower being better, or reports that the target isn't in the direction at all.
        /// Distance along the direction is measured middle to middle and distance across it edge to edge, so two targets equally far off are separated by how squarely they line up rather than by how big they are.
        /// </summary>
        private static bool TryScoreTarget(Rectangle origin, Rectangle candidate, int direction, out float score)
        {
            score = 0f;

            float alongDirection;
            float acrossDirection;

            if (direction is DIRECTION_UP or DIRECTION_DOWN)
            {
                alongDirection = direction is DIRECTION_UP ? origin.Center.Y - candidate.Center.Y : candidate.Center.Y - origin.Center.Y;
                acrossDirection = GetSpanDistance(origin.Left, origin.Right, candidate.Left, candidate.Right);
            }
            else
            {
                alongDirection = direction is DIRECTION_LEFT ? origin.Center.X - candidate.Center.X : candidate.Center.X - origin.Center.X;
                acrossDirection = GetSpanDistance(origin.Top, origin.Bottom, candidate.Top, candidate.Bottom);
            }

            // A target that isn't past the middle of where the cursor stands is not in the direction being asked for, which is what stops a step landing on something overlapping it
            if (alongDirection <= 0f)
            {
                return false;
            }

            score = alongDirection + acrossDirection * SNAP_ACROSS_WEIGHT;

            return true;
        }

        /// <summary>The gap between two spans on one axis, being zero wherever they overlap.</summary>
        private static float GetSpanDistance(int originStart, int originEnd, int candidateStart, int candidateEnd)
        {
            if (candidateEnd <= originStart)
            {
                return originStart - candidateEnd;
            }

            if (candidateStart >= originEnd)
            {
                return candidateStart - originEnd;
            }

            return 0f;
        }

        private bool TryGetNearestTarget(Point position, out SnapTarget target)
        {
            target = default;

            float bestDistance = float.MaxValue;
            bool hasFound = false;

            foreach (SnapTarget candidate in _snapTargets)
            {
                float distance = Vector2.DistanceSquared(new Vector2(position.X, position.Y), new Vector2(candidate.Bounds.Center.X, candidate.Bounds.Center.Y));

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                target = candidate;
                hasFound = true;
            }

            return hasFound;
        }

        private void SetHoveredElement(Element? element)
        {
            if (ReferenceEquals(_hoveredElement, element))
            {
                return;
            }

            if (_hoveredElement is not null)
            {
                _hoveredElement.IsHovered = false;
            }

            // Assigned before the action runs, so an action that changes what's hovered doesn't dispatch this element a second time
            _hoveredElement = element;

            if (_hoveredElement is null)
            {
                RefreshHoverText();
                return;
            }

            _hoveredElement.IsHovered = true;
            RunHoverActions(_hoveredElement);

            // Last, so a tooltip reading a variable one of those actions set shows the new value rather than the one it replaced
            RefreshHoverText();
        }

        /// <summary>Resolves the tokens in the hovered element's tooltip, from the same vocabulary an element's text uses.
        /// Refreshed on cursor entry and then alongside conditions, so a value that moves while the cursor rests on the element is followed rather than frozen at the moment it arrived.
        /// </summary>
        private void RefreshHoverText()
        {
            RefreshHoveredContent();

            if (_hoveredElement is null)
            {
                _hoveredDisplayName = null;
                _hoveredDescription = null;

                return;
            }

            _hoveredDisplayName = ResolveHoverText(_hoveredElement.DisplayName, _hoveredElement);
            _hoveredDescription = ResolveHoverText(_hoveredElement.Description, _hoveredElement);
        }

        /// <summary>Points the fields other mods read at whatever the cursor is over, being the item or NPC the hovered element is about and every tag it carries.
        /// Refreshed alongside the tooltip, so a Grid cell whose item changed under a resting cursor is followed rather than frozen at the moment the cursor arrived.
        /// </summary>
        private void RefreshHoveredContent()
        {
            if (_hoveredElement is null)
            {
                ClearHoveredContent();

                return;
            }

            HoveredItem = TagHelper.ResolveItem(_hoveredElement);
            HoveredNpc = TagHelper.ResolveNpc(_hoveredElement);
            HoveredTags = _hoveredElement.GetTags().ToList();
        }

        /// <summary>Drops what the hovered content fields point at, so nothing reading them finds an item from a book that has already been closed.</summary>
        private void ClearHoveredContent()
        {
            HoveredItem = null;
            HoveredNpc = null;
            HoveredTags = Array.Empty<string>();
        }

        private static string? ResolveHoverText(string? text, Element element)
        {
            if (string.IsNullOrEmpty(text) is true)
            {
                return text;
            }

            return TokenHelper.Resolve(text, element, quoteValues: false);
        }

        /// <summary>Runs an element's click actions in order, from <see cref="ElementData.Action"/> and then <see cref="ElementData.Actions"/>.
        /// A failing action doesn't stop the ones after it, so an action that navigates or closes the book should be the last entry.
        /// </summary>
        private void RunClickActions(Element element)
        {
            RunActions(element.Data.GetActions(), element, "action");
        }

        /// <summary>Runs an input's submit actions in order, from <see cref="InputElementData.SubmitAction"/> and then <see cref="InputElementData.SubmitActions"/>.</summary>
        private void RunSubmitActions(Element element)
        {
            if (element.Data is not InputElementData inputData || inputData.HasSubmitActions is false)
            {
                return;
            }

            RunActions(inputData.GetSubmitActions(), element, "submit action");

            RefreshVisiblePages();
        }

        /// <summary>Runs a list of trigger actions in order, resolving any placeholders first. A failing action doesn't stop the ones after it.</summary>
        private void RunActions(IEnumerable<string> actions, Element? element, string label)
        {
            foreach (string action in actions)
            {
                string resolvedAction = ActionTokenHelper.Resolve(action, element);

                if (TriggerActionManager.TryRunAction(resolvedAction, out string error, out Exception exception) is false)
                {
                    Parchment.monitor.Log($"Element {label} '{resolvedAction}' failed: {error}", LogLevel.Warn);

                    if (exception is not null)
                    {
                        Parchment.monitor.Log(exception.ToString(), LogLevel.Trace);
                    }
                }
            }
        }

        /// <summary>Runs an element's hover actions in order, from <see cref="ElementData.HoverAction"/> and then <see cref="ElementData.HoverActions"/>.
        /// A failing action doesn't stop the ones after it, so an action that navigates or closes the book should be the last entry.
        /// </summary>
        private void RunHoverActions(Element element)
        {
            if (element.Data.HasHoverActions is false)
            {
                return;
            }

            RunActions(element.Data.GetHoverActions(), element, "hover action");

            RefreshVisiblePages();
        }

        private void SetMenuState(MenuState menuState)
        {
            CurrentState = menuState;
            _animationTimer = 0f;
            _animationFrame = 0;

            // An element on the book's own layers is on screen whatever page is being read, so the cursor never left it and it keeps its hover frames through a turn. One on a page goes with the page
            bool keepHoveredElement = (menuState is MenuState.Ready or MenuState.Turning) && _hoveredElement is not null && Book.OwnsElement(_hoveredElement) is true;

            ClearHoverState(keepHoveredElement);

            // An input on the book's own layers is on screen whatever page is being read, so it keeps the keyboard through a turn rather than being dropped halfway. One on a page goes with the page, and a book that is shutting or closing drops focus whatever holds it
            if (menuState is not MenuState.Ready and not MenuState.Turning || _focusedElement is null || Book.OwnsElement(_focusedElement) is false)
            {
                ClearInputFocus();
            }

            // The book state is itself testable through CurrentBookState, so a transition can change what's visible. Refreshing here rather than waiting for the next tick keeps the swap in step with the animation it belongs to
            RefreshVisiblePages();

            if (menuState is MenuState.Ready)
            {
                HandleVisiblePages();
            }
        }

        /// <summary>Runs the visible spread's <see cref="PageData.OnView"/> triggers, then records the spread as seen.
        /// The spread is captured up front because a trigger's action can navigate away, and the pages that were on screen are the ones to mark.
        /// Triggers run before <see cref="MarkVisibleSeen"/> so one can gate itself on the page not having been seen yet.
        /// </summary>
        private void HandleVisiblePages()
        {
            Chapter chapter = GetChapter(_currentChapterIndex);
            int leftPageIndex = GetLeftPageIndex();
            int rightPageIndex = GetRightPageIndex();

            DispatchPageTriggers(leftPageIndex);

            // A left page action may have turned the page, closed the book or jumped elsewhere, in which case the right page was never really viewed.
            if (CurrentState is MenuState.Ready)
            {
                DispatchPageTriggers(rightPageIndex);
            }

            MarkVisibleSeen(chapter, leftPageIndex, rightPageIndex);
        }

        private void DispatchPageTriggers(int pageIndex)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return;
            }

            List<PageTriggerData>? triggers = _pages[pageIndex].Data.OnView;
            if (triggers is null)
            {
                return;
            }

            string pageId = _pages[pageIndex].Data.Id;
            foreach (PageTriggerData trigger in triggers)
            {
                if (ConditionHelper.Check(trigger.Condition) is false)
                {
                    continue;
                }

                foreach (string action in trigger.Actions)
                {
                    string resolvedAction = ActionTokenHelper.Resolve(action, element: null);

                    if (TriggerActionManager.TryRunAction(resolvedAction, out string error, out Exception exception) is false)
                    {
                        Parchment.monitor.Log($"OnView action '{resolvedAction}' on page '{pageId}' failed: {error}", LogLevel.Warn);

                        if (exception is not null)
                        {
                            Parchment.monitor.Log(exception.ToString(), LogLevel.Trace);
                        }
                    }
                }
            }
        }

        /// <summary>Runs every keybind the button matches, the visible spread's first and the book's own only when no page bind took it, and reports whether
        /// any of them claimed the button. A claimed button never reaches the menu's own handling, which is how a page takes over the exit button.
        /// The shut cover has the book's own binds only, as no page is being read there.
        /// </summary>
        private bool HandleKeybinds(SButton button)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Cover || button is SButton.None)
            {
                return false;
            }

            // A focused input takes precedence over every bind, so a book that binds a letter doesn't fire it while the reader types that letter
            if (_focusedInput is not null)
            {
                return false;
            }

            bool hasRunAny = false;
            bool isSuppressed = DispatchPageKeybinds(GetLeftPageIndex(), button, ref hasRunAny);

            // A left page action may have turned the page, closed the book or jumped elsewhere, in which case the right page is no longer the one being read
            if (CurrentState is MenuState.Ready)
            {
                isSuppressed |= DispatchPageKeybinds(GetRightPageIndex(), button, ref hasRunAny);
            }

            // The book's own binds are a fallback rather than a second helping, so a page binding the same button takes it off the book while that page is being read
            if (hasRunAny is false)
            {
                isSuppressed |= DispatchKeybinds(Book.Data.OnKeyPress, $"book '{Book.Data.Id}'", button, ref hasRunAny);
            }

            if (hasRunAny)
            {
                RefreshVisiblePages();
            }

            return isSuppressed;
        }

        private bool DispatchPageKeybinds(int pageIndex, SButton button, ref bool hasRunAny)
        {
            // No page is being read while the book is shut, so a cover press only reaches the book's own binds
            if (CurrentState is not MenuState.Ready || pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return false;
            }

            return DispatchKeybinds(_pages[pageIndex].Data.OnKeyPress, $"page '{_pages[pageIndex].Data.Id}'", button, ref hasRunAny);
        }

        /// <summary>Runs each keybind in the list the button matches and whose condition passes, reporting whether any of them claimed it.
        /// <paramref name="hasRunAny"/> is what decides the page over book precedence, so it is set by anything that actually runs rather than by anything that merely matched.
        /// </summary>
        private bool DispatchKeybinds(List<KeybindData>? keybinds, string source, SButton button, ref bool hasRunAny)
        {
            if (keybinds is null)
            {
                return false;
            }

            bool isSuppressed = false;

            foreach (KeybindData keybind in keybinds)
            {
                if (keybind.Matches(button) is false)
                {
                    continue;
                }

                if (ConditionHelper.Check(keybind.Condition) is false)
                {
                    continue;
                }

                isSuppressed |= keybind.SuppressDefault;
                hasRunAny = true;

                PlaySound(keybind.Sound);

                foreach (string action in keybind.GetActions())
                {
                    if (TriggerActionManager.TryRunAction(action, out string error, out Exception exception) is false)
                    {
                        Parchment.monitor.Log($"OnKeyPress action '{action}' on {source} failed: {error}", LogLevel.Warn);

                        if (exception is not null)
                        {
                            Parchment.monitor.Log(exception.ToString(), LogLevel.Trace);
                        }
                    }
                }
            }

            return isSuppressed;
        }

        /// <summary>Starts counting the exit button's hold, after a page claimed the press.</summary>
        private void BeginForceCloseHold()
        {
            _isExitButtonSuppressed = true;
            _forceCloseHoldTimer = 0f;
        }

        /// <summary>Counts how long the exit button stays down after a page took it over, forcing the book shut once the hold is long enough.
        /// This is the reader's guaranteed way out, so a page that redirects the exit button can never strand them.
        /// </summary>
        private void UpdateForceCloseHold(float elapsedMilliseconds)
        {
            if (_isExitButtonSuppressed is false)
            {
                return;
            }

            if (IsExitButtonHeld() is false)
            {
                _isExitButtonSuppressed = false;
                _forceCloseHoldTimer = 0f;

                return;
            }

            _forceCloseHoldTimer += elapsedMilliseconds;
            if (_forceCloseHoldTimer < FORCE_CLOSE_HOLD_DURATION)
            {
                return;
            }

            _isExitButtonSuppressed = false;
            _forceCloseHoldTimer = 0f;

            ForceClose();
        }

        /// <summary>Whether anything bound to the menu's exit is currently down, covering the keyboard binding and the controller's B button.</summary>
        private static bool IsExitButtonHeld()
        {
            KeyboardState keyboardState = Game1.input.GetKeyboardState();
            foreach (InputButton inputButton in Game1.options.menuButton)
            {
                if (inputButton.key != Keys.None && keyboardState.IsKeyDown(inputButton.key))
                {
                    return true;
                }
            }

            return Game1.options.gamepadControls && Game1.input.GetGamePadState().IsButtonDown(Buttons.B);
        }

        /// <summary>Shuts the book and leaves the menu, ignoring ExitToCover. The reader held the exit button to get here, so landing on the cover isn't what they asked for.</summary>
        private void ForceClose()
        {
            if (CurrentState is MenuState.Closing)
            {
                return;
            }

            SetMenuState(MenuState.Closing);
            PlaySound(_animation.CloseSound);
        }

        private void MarkVisibleSeen(Chapter chapter, int leftPageIndex, int rightPageIndex)
        {
            var who = Game1.player;
            string bookId = Book.Data.Id;

            if (string.IsNullOrWhiteSpace(chapter.Id) is false && Parchment.bookManager.HasSeenChapter(who, bookId, chapter.Id) is false)
            {
                Parchment.bookManager.SetSeenChapter(who, bookId, chapter.Id);
            }

            MarkPageSeen(who, bookId, chapter, leftPageIndex);
            MarkPageSeen(who, bookId, chapter, rightPageIndex);
        }

        private void MarkPageSeen(Farmer who, string bookId, Chapter chapter, int pageIndex)
        {
            if (pageIndex >= _pages.Count)
            {
                return;
            }

            var page = _pages[pageIndex];
            string chapterId = chapter.Id ?? string.Empty;
            if (Parchment.bookManager.HasSeenPage(who, bookId, chapterId, page.Data.Id) is false)
            {
                Parchment.bookManager.SetSeenPage(who, bookId, chapterId, page.Data.Id);
            }
        }

        /// <summary>Whether an Input element currently has the keyboard. Read from the mod's button handler, which suppresses world binds while the reader is typing.</summary>
        public bool HasFocusedInput => _focusedInput is not null;

        /// <summary>Gives an Input element the keyboard, so typing goes to it rather than to the menu.</summary>
        private void FocusInput(Element element)
        {
            if (element.Data is not InputElementData inputData || string.IsNullOrWhiteSpace(inputData.InputId))
            {
                return;
            }

            if (ReferenceEquals(_focusedElement, element) is true)
            {
                return;
            }

            ClearInputFocus();

            _focusedElement = element;
            element.IsFocused = true;

            _focusedInput = new InputTextSubscriber(inputData.InputId, inputData.MaxLength, OnInputTextChanged, () => RunSubmitActions(element));

            // The on-screen keyboard takes the characters itself, so the subscriber is kept off the dispatcher and stands only for the input being focused
            if (IsTextEntryPreferred() is true)
            {
                OpenTextEntry();
                return;
            }

            // Assigning the subscriber is what starts the game routing characters here. The dispatcher owns the Selected flag on both the old and new subscriber
            Game1.keyboardDispatcher.Subscriber = _focusedInput;
        }

        private void ClearInputFocus()
        {
            CloseTextEntry();

            if (_focusedElement is not null)
            {
                _focusedElement.IsFocused = false;
                _focusedElement = null;
            }

            if (_focusedInput is null)
            {
                return;
            }

            // Only release the dispatcher when it is still ours, so a menu opened over this one keeps the keyboard
            if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, _focusedInput) is true)
            {
                Game1.keyboardDispatcher.Subscriber = null;
            }

            _focusedInput = null;
        }

        /// <summary>Whether an input is typed into through the on-screen keyboard rather than straight off the hardware keyboard.
        /// Snappy menus is what a vanilla text box goes by, so a reader who has set the game up for a controller is offered the same thing in a book as everywhere else.
        /// </summary>
        private static bool IsTextEntryPreferred()
        {
            return Game1.options.snappyMenus && Game1.options.gamepadControls;
        }

        /// <summary>Whether the on-screen keyboard is up. This and the two methods below it are everywhere Parchment reaches for the game's own entry menu.</summary>
        private static bool IsTextEntryOpen()
        {
            return Game1.textEntry is not null;
        }

        /// <summary>Puts up the on-screen keyboard over a throwaway box seeded with the input's text.
        /// The box exists only because the keyboard writes into a <see cref="TextBox"/> rather than handing back a string, and Parchment never draws it.
        /// </summary>
        private void OpenTextEntry()
        {
            if (_focusedInput is null)
            {
                return;
            }

            _lastTextEntryText = Parchment.inputManager.GetText(_focusedInput.InputId);

            // The same textures a vanilla naming box is built from, since the box is handed to a menu that draws it
            _textEntryBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor);
            _textEntryBox.Text = _lastTextEntryText;

            // Selecting the box is what hands it the characters, and under snappy menus that is also what puts the keyboard up, so the keyboard is only asked for when it wasn't
            _textEntryBox.Selected = true;

            if (IsTextEntryOpen() is false)
            {
                Game1.showTextEntry(_textEntryBox);
            }
        }

        private void CloseTextEntry()
        {
            if (_textEntryBox is null)
            {
                return;
            }

            TextBox textEntryBox = _textEntryBox;

            // Let go of before the box is dropped, since dropping the box is what tells everything below this that there is no keyboard to follow
            _textEntryBox = null;
            _lastTextEntryText = string.Empty;

            textEntryBox.Selected = false;

            if (IsTextEntryOpen() is true)
            {
                Game1.closeTextEntry();
            }
        }

        /// <summary>Follows the on-screen keyboard while it is up, writing what it holds through to the input, then lets the input go once it closes.
        /// The text is written before the close is checked, so the last of it lands whether or not the menu is updated while the keyboard sits over it.
        /// </summary>
        private void UpdateTextEntry()
        {
            if (_textEntryBox is null)
            {
                return;
            }

            string enteredText = _textEntryBox.Text ?? string.Empty;

            if (string.Equals(enteredText, _lastTextEntryText, StringComparison.Ordinal) is false)
            {
                WriteTextEntry(enteredText);
            }

            if (IsTextEntryOpen() is true)
            {
                return;
            }

            // Closing the keyboard is a controller reader's only enter, so it stands in for one. Held onto because dropping focus is what clears it
            Element? submittedElement = _focusedElement;

            ClearInputFocus();

            if (submittedElement is not null)
            {
                RunSubmitActions(submittedElement);
            }
        }

        /// <summary>Writes what the keyboard holds through to the input, taking the same route a typed character takes.</summary>
        private void WriteTextEntry(string enteredText)
        {
            if (_focusedInput is null || _textEntryBox is null)
            {
                return;
            }

            // The keyboard has no limit of its own, so the box is cut back rather than left showing text the input won't keep
            if (_focusedInput.MaxLength is int maximumLength && enteredText.Length > maximumLength)
            {
                enteredText = enteredText.Substring(0, maximumLength);
                _textEntryBox.Text = enteredText;
            }

            _lastTextEntryText = enteredText;

            Parchment.inputManager.SetText(_focusedInput.InputId, enteredText);

            OnInputTextChanged();
        }

        /// <summary>Refreshes conditions the moment an input changes, so a list filtered on the typed text keeps up with the reader rather than waiting for the next condition tick.</summary>
        private void OnInputTextChanged()
        {
            RefreshVisiblePages();
        }

        /// <param name="keepHoveredElement">Whether the element under the cursor stays hovered. The corners are dropped either way, as which of them can be turned isn't settled until the turn lands.</param>
        private void ClearHoverState(bool keepHoveredElement = false)
        {
            if (keepHoveredElement is false)
            {
                SetHoveredElement(null);
            }

            _isHoveringPreviousPage = false;
            _isHoveringNextPage = false;

            _isHoveringLeftPage = false;
            _isHoveringRightPage = false;

            _previousCornerFrame = 0;
            _nextCornerFrame = 0;
            _previousCornerAnimationTimer = 0f;
            _nextCornerAnimationTimer = 0f;
        }

        /// <summary>Runs the actions of any frame that started this tick, on either visible page.
        /// Only elements that carry a frame action are walked, so a book without any pays almost nothing for this running every tick.
        /// </summary>
        private void DispatchFrameActions()
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Turning)
            {
                return;
            }

            // The book's own layers are on screen whatever is being read, so they run first and regardless of which pages are showing
            bool hasRunAny = DispatchFrameActions(Book.FrameActionElements);

            if (CurrentState is MenuState.Ready or MenuState.Turning)
            {
                hasRunAny |= DispatchPageFrameActions(GetLeftPageIndex());
            }

            // A frame action may have turned the page or closed the book, in which case the right page is no longer the one being read
            if (CurrentState is MenuState.Ready or MenuState.Turning)
            {
                hasRunAny |= DispatchPageFrameActions(GetRightPageIndex());
            }

            // Refreshed straight away rather than on the next interval, so an animation that ends by setting a flag has its frames conditioned out before the cycle wraps and replays
            if (hasRunAny is true)
            {
                RefreshVisiblePages();
            }
        }

        private bool DispatchPageFrameActions(int pageIndex)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return false;
            }

            return DispatchFrameActions(_pages[pageIndex].FrameActionElements);
        }

        /// <summary>Runs the actions of any frame in this list that started on this tick, reporting whether any of them ran.</summary>
        private bool DispatchFrameActions(IReadOnlyList<Element> frameActionElements)
        {
            bool hasRunAny = false;

            foreach (Element element in frameActionElements)
            {
                if (element.IsVisible is false)
                {
                    continue;
                }

                AnimationFrameData? activeFrame = AnimationHelper.GetActiveFrame(element, element.Data.FrameDuration);
                if (ReferenceEquals(element.LastPlayedFrame, activeFrame) is true)
                {
                    continue;
                }

                element.LastPlayedFrame = activeFrame;

                if (activeFrame is null || activeFrame.HasActions is false)
                {
                    continue;
                }

                RunActions(activeFrame.GetActions(), element, "frame action");
                hasRunAny = true;
            }

            return hasRunAny;
        }

        /// <summary>Runs the text changed actions of any Input whose text has stopped moving for its TextChangedDelay.
        /// The text is polled rather than hooked off typing, so a clear button or a SetInput action counts as a change the same as a keystroke does.
        /// </summary>
        private void DispatchTextChangedActions(float elapsedMilliseconds)
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Turning)
            {
                return;
            }

            bool hasRunAny = DispatchTextChangedActions(Book.TextChangedActionElements, elapsedMilliseconds);

            if (CurrentState is MenuState.Ready or MenuState.Turning)
            {
                hasRunAny |= DispatchPageTextChangedActions(GetLeftPageIndex(), elapsedMilliseconds);
            }

            if (CurrentState is MenuState.Ready or MenuState.Turning)
            {
                hasRunAny |= DispatchPageTextChangedActions(GetRightPageIndex(), elapsedMilliseconds);
            }

            if (hasRunAny is true)
            {
                RefreshVisiblePages();
            }
        }

        private bool DispatchPageTextChangedActions(int pageIndex, float elapsedMilliseconds)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return false;
            }

            return DispatchTextChangedActions(_pages[pageIndex].TextChangedActionElements, elapsedMilliseconds);
        }

        private bool DispatchTextChangedActions(IReadOnlyList<Element> textChangedActionElements, float elapsedMilliseconds)
        {
            bool hasRunAny = false;

            foreach (Element element in textChangedActionElements)
            {
                if (element.IsVisible is false || element.Data is not InputElementData inputData)
                {
                    continue;
                }

                string currentText = Parchment.inputManager.GetText(inputData.InputId);

                if (string.Equals(element.LastSeenInputText, currentText, StringComparison.Ordinal) is false)
                {
                    // The first look records the text without arming anything, so a book doesn't run its text changed actions the moment it opens
                    bool hasSeenBefore = element.LastSeenInputText is not null;

                    element.LastSeenInputText = currentText;
                    element.TextChangedDelayRemaining = hasSeenBefore ? inputData.TextChangedDelay : null;

                    continue;
                }

                if (element.TextChangedDelayRemaining is not float delayRemaining)
                {
                    continue;
                }

                delayRemaining -= elapsedMilliseconds;
                if (delayRemaining > 0f)
                {
                    element.TextChangedDelayRemaining = delayRemaining;
                    continue;
                }

                element.TextChangedDelayRemaining = null;

                RunActions(inputData.GetTextChangedActions(), element, "text changed action");
                hasRunAny = true;
            }

            return hasRunAny;
        }

        /// <summary>Hands each result Grid's cells the items matching whatever the reader has typed. A grid whose filter hasn't moved does nothing, so this is a pair of string compares on a quiet tick.</summary>
        private void RefreshResults()
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Turning)
            {
                return;
            }

            if (RefreshResults(Book.ResultElements) is true)
            {
                Book.InvalidateLayout();
            }

            RefreshPageResults(GetLeftPageIndex());
            RefreshPageResults(GetRightPageIndex());
        }

        private void RefreshPageResults(int pageIndex)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return;
            }

            // Cells gaining or losing an item isn't something the condition pass can see, so the relayout has to be asked for rather than waited on
            if (RefreshResults(_pages[pageIndex].ResultElements) is true)
            {
                _pages[pageIndex].InvalidateLayout();
            }
        }

        private static bool RefreshResults(IReadOnlyList<Element> resultElements)
        {
            bool hasChanged = false;

            foreach (Element element in resultElements)
            {
                if (element.Results is null)
                {
                    continue;
                }

                hasChanged |= element.Results.TryRefresh(element.Children);
            }

            return hasChanged;
        }

        /// <summary>The counts behind a Grid, found by its Id across the book's own layers and both visible pages.
        /// A grid filling its cells from a Source reports on its candidates, and one with authored children reports on those, so the same tokens read either kind.
        /// </summary>
        public bool TryGetGridCounts(string gridId, out int displayed, out int matched, out int total)
        {
            displayed = 0;
            matched = 0;
            total = 0;

            if (TryFindGridInBook(gridId, out Element? grid) is false)
            {
                return false;
            }

            if (grid!.Results is ResultSet results)
            {
                displayed = results.DisplayedCount;
                matched = results.MatchedCount;
                total = results.TotalCount;

                return true;
            }

            foreach (Element child in grid.Children)
            {
                total++;

                if (child.IsVisible is true)
                {
                    matched++;
                }
            }

            // A capped grid draws only what its cells hold, so what is displayed and what matched are different numbers
            int cellCount = grid.Data is GridElementData gridData && gridData.Rows is int rows ? rows * gridData.Columns : matched;
            displayed = Math.Min(matched, cellCount);

            return true;
        }

        /// <summary>Looks for a grid on the book's own layers first, then on each visible page. A grid the reader can't see isn't found, so a token can only report on what is in front of them.</summary>
        private bool TryFindGridInBook(string gridId, out Element? grid)
        {
            if (TryFindGrid(Book.Underlay, gridId, out grid) is true || TryFindGrid(Book.Overlay, gridId, out grid) is true)
            {
                return true;
            }

            return TryFindGridOnPage(GetLeftPageIndex(), gridId, out grid) || TryFindGridOnPage(GetRightPageIndex(), gridId, out grid);
        }

        private bool TryFindGridOnPage(int pageIndex, string gridId, out Element? grid)
        {
            grid = null;

            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return false;
            }

            Page page = _pages[pageIndex];

            return TryFindGrid(page.Elements, gridId, out grid) || TryFindGrid(page.Background, gridId, out grid) || TryFindGrid(page.Foreground, gridId, out grid);
        }

        private static bool TryFindGrid(IReadOnlyList<Element> elements, string gridId, out Element? grid)
        {
            foreach (Element element in elements)
            {
                if (element.Data is GridElementData && string.Equals(element.Data.Id, gridId, StringComparison.OrdinalIgnoreCase) is true)
                {
                    grid = element;
                    return true;
                }

                if (TryFindGrid(element.Children, gridId, out grid) is true || TryFindGrid(element.Background, gridId, out grid) is true || TryFindGrid(element.Foreground, gridId, out grid) is true)
                {
                    return true;
                }
            }

            grid = null;
            return false;
        }

        /// <summary>Watches the elements whose text carries a token and asks for a relayout when one resolves differently.
        /// Text is wrapped and measured once per layout pass, so a token whose value moves without a condition moving with it would otherwise stay on screen as it was.
        /// </summary>
        private void RefreshTokenText()
        {
            if (CurrentState is not MenuState.Ready and not MenuState.Turning)
            {
                return;
            }

            if (RefreshTokenText(Book.TokenTextElements) is true)
            {
                Book.InvalidateLayout();
            }

            RefreshPageTokenText(GetLeftPageIndex());
            RefreshPageTokenText(GetRightPageIndex());
        }

        private void RefreshPageTokenText(int pageIndex)
        {
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
            {
                return;
            }

            if (RefreshTokenText(_pages[pageIndex].TokenTextElements) is true)
            {
                _pages[pageIndex].InvalidateLayout();
            }
        }

        private static bool RefreshTokenText(IReadOnlyList<Element> tokenTextElements)
        {
            bool hasChanged = false;

            foreach (Element element in tokenTextElements)
            {
                string? resolvedText = TokenHelper.ResolveElementText(element);

                if (string.Equals(element.LastResolvedText, resolvedText, StringComparison.Ordinal) is true)
                {
                    continue;
                }

                // The first look records the text without asking for anything, since the layout that is about to run will resolve it anyway
                bool hasSeenBefore = element.LastResolvedText is not null;

                element.LastResolvedText = resolvedText;
                hasChanged |= hasSeenBefore;
            }

            return hasChanged;
        }

        private void UpdateConditionTimer()
        {
            _conditionRefreshTimer++;

            if (_conditionRefreshTimer >= CONDITION_REFRESH_INTERVAL)
            {
                RefreshVisiblePages();
            }
        }

        private static void PlaySound(string? sound)
        {
            if (string.IsNullOrWhiteSpace(sound))
            {
                return;
            }

            Game1.playSound(sound);
        }

        /// <summary>Holds down what the book changes outside itself, for as long as this menu is the one being updated.
        /// Asserted every tick rather than set once when the book opens, since a mod can push its own menu over the book and hand it straight back without the book ever seeing an exit.
        /// Lookup Anything does exactly that, so the HUD it saw restored on the way in is put back here on the way out.
        /// </summary>
        private void AssertReadingSession()
        {
            Game1.displayHUD = false;

            // The book is being read again, so a later close still puts the session down properly even if something already ended it while the book was covered
            _hasEndedSession = false;
        }

        /// <summary>Hands the HUD back to whatever the reader had it set to.
        /// A book handed straight over to another book leaves it alone, since the new one is already holding it down and has taken on the state to restore.
        /// </summary>
        private void RestoreHud()
        {
            if (Game1.activeClickableMenu is BookMenu incomingBook && ReferenceEquals(incomingBook, this) is false)
            {
                return;
            }

            Game1.displayHUD = _previousHudState;
        }

        /// <summary>Puts the reading down while something else holds the active menu, restoring what the book changed outside itself without ending the session.
        /// Reversible on purpose, as a covering menu may hand the book straight back, and the reader's typed text and flags are theirs until the book is genuinely put away.
        /// </summary>
        public void SuspendSession()
        {
            ClearInputFocus();
            ClearHoveredContent();
            RestoreHud();
        }

        /// <summary>Puts down the reading session for good, restoring what the menu changed outside itself and clearing what only lasts as long as the book is open.
        /// Runs from the menu's own exit, or from <see cref="Managers.BookManager"/> once a book that lost the active menu has been settled as closed rather than covered, whichever comes first.
        /// </summary>
        public void EndSession()
        {
            if (_hasEndedSession is true)
            {
                return;
            }
            _hasEndedSession = true;

            SuspendSession();

            // A book handed straight over to another book leaves the rest to the new one, which is already holding the reader
            if (Game1.activeClickableMenu is BookMenu incomingBook && ReferenceEquals(incomingBook, this) is false)
            {
                return;
            }

            // Input text and flags are per reading session, so they don't survive the book being closed
            Parchment.inputManager.ClearAll();
            Parchment.flagManager.ClearAll();

            // Variables do survive, so the global ones are written out here rather than waiting for the next save
            Parchment.variableManager.Save();

            // A book edited while it was being read is reloaded now rather than under the reader
            Parchment.bookManager.ApplyPendingBookReload();
        }

        protected override void cleanupBeforeExit()
        {
            EndSession();

            base.cleanupBeforeExit();
        }

        public override void emergencyShutDown()
        {
            EndSession();

            base.emergencyShutDown();
        }

        public override bool readyToClose()
        {
            if (CurrentState is MenuState.Covering)
            {
                return false;
            }

            return CurrentState != MenuState.Closing || _animationTimer >= _animation.CloseDuration;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (CurrentState == MenuState.Closing)
            {
                exitThisMenu(playSound: false);
                return;
            }

            bool isExitButton = Game1.options.doesInputListContain(Game1.options.menuButton, key);

            // A focused input has the keyboard, so no keystroke reaches the page's binds or the menu's own handling while the reader is typing
            if (_focusedInput is not null)
            {
                // Escape alone leaves the box, as the menu button list also holds E by default and a reader typing "e" means the letter. A second press then closes the book
                if (key is Keys.Escape)
                {
                    ClearInputFocus();
                }

                return;
            }

            // The visible page gets first refusal, so it can take the button over before the menu acts on it
            if (HandleKeybinds(key.ToSButton()) is true)
            {
                if (isExitButton)
                {
                    BeginForceCloseHold();
                }

                return;
            }

            if (isExitButton)
            {
                BeginClose();
                return;
            }

            if (CurrentState != MenuState.Ready)
            {
                return;
            }

            base.receiveKeyPress(key);
        }

        public override void receiveGamePadButton(Buttons button)
        {
            if (HandleKeybinds(button.ToSButton()) is true)
            {
                if (button is Buttons.B)
                {
                    BeginForceCloseHold();
                }

                return;
            }

            // The triggers turn the page on a controller, offered after the binds so a book that takes them over for itself keeps them
            if ((button is Buttons.LeftTrigger or Buttons.RightTrigger) && _focusedInput is null)
            {
                TryTurnPage(button is Buttons.RightTrigger, out _);
                return;
            }

            base.receiveGamePadButton(button);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (CurrentState == MenuState.Closing)
            {
                exitThisMenu(playSound: false);
                return;
            }

            if (CurrentState == MenuState.Covering)
            {
                // Skip the shutting animation
                SetMenuState(MenuState.Cover);
                return;
            }

            if (CurrentState == MenuState.Cover)
            {
                // An overlay element gets first refusal, so a button authored onto the cover still works
                Element? coverElement = GetElementAt(new Point(x, y));
                if (coverElement is not null && coverElement.Data.HasActions)
                {
                    PlaySound(coverElement.Data.Sound);
                    RunClickActions(coverElement);

                    return;
                }

                if (GetBookScreenBounds().Contains(x, y) is true)
                {
                    BeginReopen();
                }

                return;
            }

            if (CurrentState == MenuState.Sliding)
            {
                // Skip the slide, landing wherever the slide would have left the book
                _currentPosition = _targetPosition;
                SettleAfterSlide();
                return;
            }

            if (CurrentState == MenuState.Opening)
            {
                // Skip the rest of the opening animation. The reader has already asked for the book open, so this doesn't stop at the cover.
                _currentPosition = _targetPosition;
                SetMenuState(MenuState.Ready);
                return;
            }

            if (CurrentState == MenuState.Turning)
            {
                // Skip turn
                CommitPageTurn();
                SetMenuState(MenuState.Ready);
                return;
            }

            // Check for any button element
            Element? clickedElement = GetElementAt(new Point(x, y));

            if (clickedElement is not null && clickedElement.Data is InputElementData)
            {
                FocusInput(clickedElement);
                return;
            }

            ClearInputFocus();

            if (clickedElement is not null && clickedElement.Data.HasActions)
            {
                PlaySound(clickedElement.Data.Sound);
                RunClickActions(clickedElement);

                RefreshVisiblePages();
                return;
            }

            if (_previousPageHotspot.Contains(x, y) && _currentSpread > 0)
            {
                BeginPageTurn(forward: false); return;
            }
            if (_nextPageHotspot.Contains(x, y) && _currentSpread < GetSpreadCount() - 1)
            {
                BeginPageTurn(forward: true); return;
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (CurrentState != MenuState.Ready)
            {
                return;
            }
        }

        public override void performHoverAction(int x, int y)
        {
            // Neither state has pages to hover, one because the book is shut and the other because they are mid-turn, so only the book's own layers are followed and the corner hotspots stay dark
            if (CurrentState is MenuState.Cover or MenuState.Turning)
            {
                base.performHoverAction(x, y);
                SetHoveredElement(GetElementAt(new Point(x, y)));

                return;
            }

            if (CurrentState != MenuState.Ready)
            {
                return;
            }

            base.performHoverAction(x, y);

            _isHoveringPreviousPage = _previousPageHotspot.Contains(x, y) && _currentSpread > 0;
            _isHoveringNextPage = _nextPageHotspot.Contains(x, y) && _currentSpread < GetSpreadCount() - 1;

            _isHoveringLeftPage = GetLeftPageBounds().Contains(x, y);
            _isHoveringRightPage = GetRightPageBounds().Contains(x, y);

            SetHoveredElement(GetElementAt(new Point(x, y)));
        }

        public override void update(GameTime time)
        {
            base.update(time);

            AssertReadingSession();

            float elapsedMilliseconds = (float)time.ElapsedGameTime.TotalMilliseconds;

            // Conditions refresh in every state, so CurrentBookState works for all of them and there's no state where a condition goes stale
            UpdateConditionTimer();

            // Every tick rather than on the condition interval, as a frame shorter than that interval would otherwise be stepped over without ever being seen
            DispatchFrameActions();

            DispatchTextChangedActions(elapsedMilliseconds);

            RefreshResults();

            RefreshTokenText();

            // Tracked in every state, since an action a keybind ran may have started an animation the reader is now holding the button through
            UpdateForceCloseHold(elapsedMilliseconds);

            // Followed in every state too, as the keyboard is put up over whatever the book was doing and the text it holds is wanted back either way
            UpdateTextEntry();

            if (CurrentState is MenuState.Sliding)
            {
                _animationTimer += elapsedMilliseconds;

                float progress = Math.Clamp(_animationTimer / _animation.SlideDuration, 0f, 1f);

                //  Ease out for a fast start but soft landing
                float easedProgress = 1f - (1f - progress) * (1f - progress);

                _currentPosition = Vector2.Lerp(_startPosition, _targetPosition, easedProgress);

                if (_animationTimer >= _animation.SlideDuration)
                {
                    _currentPosition = _targetPosition;

                    SettleAfterSlide();
                }
            }
            else if (CurrentState is MenuState.Opening)
            {
                _animationTimer += elapsedMilliseconds;

                // Advance frames evenly across the duration
                _animationFrame = Math.Min((int)(_animationTimer / _animation.OpenDuration * _openFrames.Count), _openFrames.Count - 1);

                if (_animationTimer >= _animation.OpenDuration)
                {
                    SetMenuState(MenuState.Ready);
                    PlaySound(_animation.OpenSound);
                }
            }
            else if (CurrentState is MenuState.Ready)
            {
                UpdateCornerAnimation(ref _nextCornerAnimationTimer, ref _nextCornerFrame, _isHoveringNextPage, elapsedMilliseconds);
                UpdateCornerAnimation(ref _previousCornerAnimationTimer, ref _previousCornerFrame, _isHoveringPreviousPage, elapsedMilliseconds);
            }
            else if (CurrentState is MenuState.Turning)
            {
                _animationTimer += elapsedMilliseconds;

                _animationFrame = Math.Min((int)(_animationTimer / _animation.TurnDuration * _pageTurnFrames.Count), _pageTurnFrames.Count - 1);

                if (_animationTimer >= _animation.TurnDuration)
                {
                    CommitPageTurn();
                    SetMenuState(MenuState.Ready);
                }
            }
            else if (CurrentState is MenuState.Covering)
            {
                _animationTimer += elapsedMilliseconds;

                // Run the frames backwards
                _animationFrame = Math.Min((int)(_animationTimer / _animation.CloseDuration * _closeFrames.Count), _closeFrames.Count - 1);

                if (_animationTimer >= _animation.CloseDuration)
                {
                    SetMenuState(MenuState.Cover);
                }
            }
            else if (CurrentState is MenuState.Closing)
            {
                _animationTimer += elapsedMilliseconds;

                // Run the frames backwards
                _animationFrame = Math.Min((int)(_animationTimer / _animation.CloseDuration * _closeFrames.Count), _closeFrames.Count - 1);

                if (_animationTimer >= _animation.CloseDuration)
                {
                    exitThisMenu(playSound: false);
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            if (!Game1.options.showClearBackgrounds)
            {
                b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            }

            var textureBounds = _openFrames[_openFrames.Count - 1];
            if (CurrentState == MenuState.Sliding)
            {
                textureBounds = _openFrames[0];
            }
            else if (CurrentState == MenuState.Opening)
            {
                textureBounds = _openFrames[_animationFrame];
            }
            else if (CurrentState == MenuState.Turning)
            {
                textureBounds = _isTurningForward ? _pageTurnFrames[_animationFrame] : _pageTurnFramesReversed[_animationFrame];
            }
            else if (CurrentState == MenuState.Covering || CurrentState == MenuState.Closing)
            {
                textureBounds = _closeFrames[_animationFrame];
            }
            else if (CurrentState == MenuState.Cover)
            {
                textureBounds = _openFrames[0];
            }

            Rectangle liveBookBounds = GetLiveBookScreenBounds();
            ElementRenderContext bookContext = EnsureBookLayout();

            DrawElements(b, Book.Underlay, liveBookBounds, bookContext);

            if (_bookGrayscaleTexture is not null)
            {
                b.Draw(_bookGrayscaleTexture, _currentPosition, textureBounds, _bookTintColor, 0f, Vector2.Zero, _appearance.Scale, SpriteEffects.None, BOOK_LAYER_DEPTH);
            }

            b.Draw(_bookTexture, _currentPosition, textureBounds, Color.White, 0f, Vector2.Zero, _appearance.Scale, SpriteEffects.None, BOOK_LAYER_DEPTH);

            if (CurrentState is MenuState.Ready or MenuState.Turning)
            {
                DrawPages(b);

                if (CurrentState is MenuState.Ready)
                {
                    DrawCorners(b);
                }
            }

            // Drawn in every state, the same as the underlay, so it rides in with the book and stays on the shut cover
            DrawElements(b, Book.Overlay, liveBookBounds, bookContext);

            base.draw(b);

            if (CurrentState is (MenuState.Ready or MenuState.Cover) && _hoveredElement is not null && (string.IsNullOrEmpty(_hoveredDisplayName) is false || string.IsNullOrEmpty(_hoveredDescription) is false))
            {
                if (string.IsNullOrEmpty(_hoveredDisplayName) is false && string.IsNullOrEmpty(_hoveredDescription) is true)
                {
                    drawHoverText(b, _hoveredDisplayName, Game1.smallFont);
                }
                else
                {
                    drawHoverText(b, _hoveredDescription, Game1.smallFont, boldTitleText: _hoveredDisplayName);
                }
            }

            base.drawMouse(b, ignore_transparency: true);
        }

        private void DrawCorners(SpriteBatch b)
        {
            if (_pageCurl.IsEnabled is false || _pageCurlTexture is null)
            {
                DrawDebugBounds(b);
                return;
            }

            if (_currentSpread > 0)
            {
                b.Draw(_pageCurlTexture, new Vector2(_previousPageHotspot.X, _previousPageHotspot.Y), _pageCurlFrames[_previousCornerFrame], Color.White, 0f, Vector2.Zero, _pageCurl.Scale, SpriteEffects.FlipHorizontally, CURL_LAYER_DEPTH);
            }

            if (_currentSpread < GetSpreadCount() - 1)
            {
                b.Draw(_pageCurlTexture, new Vector2(_nextPageHotspot.X, _nextPageHotspot.Y), _pageCurlFrames[_nextCornerFrame], Color.White, 0f, Vector2.Zero, _pageCurl.Scale, SpriteEffects.None, CURL_LAYER_DEPTH);
            }

            DrawDebugBounds(b);
        }

        /// <summary>Draws the page and corner rectangles in debug mode. An empty rect draws nothing, so a book without corners shows only its pages.</summary>
        private void DrawDebugBounds(SpriteBatch b)
        {
            if (Parchment.isDebugMode is false)
            {
                return;
            }

            b.Draw(Game1.staminaRect, GetLeftPageBounds(), Color.Red * 0.4f);
            b.Draw(Game1.staminaRect, GetRightPageBounds(), Color.Red * 0.4f);
            b.Draw(Game1.staminaRect, _previousPageHotspot, Color.Cyan * 0.4f);
            b.Draw(Game1.staminaRect, _nextPageHotspot, Color.Cyan * 0.4f);
        }

        private void DrawPages(SpriteBatch b)
        {
            if (CurrentState != MenuState.Turning)
            {
                DrawSide(b, _currentChapterIndex, _currentSpread, left: true);
                DrawSide(b, _currentChapterIndex, _currentSpread, left: false);
                return;
            }

            float turnProgress = Math.Clamp(_animationTimer / _animation.TurnDuration, 0f, 1f);
            bool hasSwapped = turnProgress >= _animation.ContentSwapProgress;

            // A single page is the whole turn rather than a side of one, so it carries the old content across to the swap and the new content on from it, never going blank
            if (Book.Data.Layout.IsSinglePage is true)
            {
                if (hasSwapped)
                {
                    DrawSide(b, _pendingChapterIndex, _pendingSpread, left: true);
                    return;
                }

                DrawSide(b, _currentChapterIndex, _currentSpread, left: true);
                return;
            }

            // The swept side (right when forward, left when backward): blank until swap then NEW content
            // The stationary side: Old content until swap then blank until landing
            bool leftIsSwept = !_isTurningForward;

            if (leftIsSwept)
            {
                if (hasSwapped)
                {
                    DrawSide(b, _pendingChapterIndex, _pendingSpread, left: true);
                }
            }
            else if (!hasSwapped)
            {
                DrawSide(b, _currentChapterIndex, _currentSpread, left: true);
            }

            if (!leftIsSwept)
            {
                if (hasSwapped)
                {
                    DrawSide(b, _pendingChapterIndex, _pendingSpread, left: false);
                }
            }
            else if (!hasSwapped)
            {
                DrawSide(b, _currentChapterIndex, _currentSpread, left: false);
            }
        }

        private void DrawSide(SpriteBatch b, int chapterIndex, int spread, bool left)
        {
            int pageIndex = GetPageIndex(chapterIndex, spread, left);

            if (pageIndex < _pages.Count)
            {
                DrawPage(b, pageIndex, left ? GetLeftPageBounds() : GetRightPageBounds());
            }
        }

        private void DrawPage(SpriteBatch b, int pageIndex, Rectangle pageBounds)
        {
            if (pageIndex >= _pages.Count)
            {
                return;
            }

            var page = _pages[pageIndex];
            ElementRenderContext context = EnsureLayout(page, pageBounds);

            DrawElements(b, page.Background, pageBounds, context);
            DrawElements(b, page.Elements, pageBounds, context);
            DrawElements(b, page.Foreground, pageBounds, context);
        }

        private void DrawElements(SpriteBatch b, IReadOnlyList<Element> elements, Rectangle pageBounds, ElementRenderContext context)
        {
            foreach (var element in elements)
            {
                if (element.Bounds == Rectangle.Empty)
                {
                    continue;
                }

                Rectangle screenBounds = new Rectangle(element.Bounds.X + pageBounds.X, element.Bounds.Y + pageBounds.Y, element.Bounds.Width, element.Bounds.Height);
                element.Renderer.Draw(b, element, screenBounds, context);

                if (Parchment.isDebugMode)
                {
                    b.Draw(Game1.staminaRect, screenBounds, Color.Lime * 0.3f);
                }
            }
        }

        private ElementRenderContext EnsureLayout(Page page, Rectangle pageContentBounds)
        {
            ElementRenderContext context = BuildRenderContext(pageContentBounds, page);

            if (page.LastLayoutContext != context)
            {
                page.PerformLayout(context);
                page.LastLayoutContext = context;
            }

            return context;
        }

        private ElementRenderContext BuildRenderContext(Rectangle pageBounds, Page page)
        {
            return new ElementRenderContext(pageBounds.Width, pageBounds.Height, page.Index, page.IndexInChapter);
        }

        private ElementRenderContext EnsureBookLayout()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            ElementRenderContext context = new ElementRenderContext(bookBounds.Width, bookBounds.Height);

            if (Book.LastLayoutContext != context)
            {
                Book.PerformLayout(context);
            }

            return context;
        }

        private Rectangle GetLiveBookScreenBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            Vector2 slideOffset = _currentPosition - _targetPosition;

            return new Rectangle(bookBounds.X + (int)slideOffset.X, bookBounds.Y + (int)slideOffset.Y, bookBounds.Width, bookBounds.Height);
        }

        private Color ResolveBookTintColor(BookData data)
        {
            if (string.IsNullOrWhiteSpace(data.Appearance.TintColor))
            {
                return Color.White;
            }

            if (ColorParser.TryParse(data.Appearance.TintColor, out Color parsedColor) is false)
            {
                Parchment.monitor.Log($"Book '{data.Id}' has an unparsable {nameof(data.Appearance.TintColor)} '{data.Appearance.TintColor}'; the book will not be tinted.", LogLevel.Warn);
                return Color.White;
            }

            return parsedColor;
        }
    }
}
