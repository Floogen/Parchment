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
        public Book Book { get; }

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

        private readonly BookAppearanceData _appearance;
        private readonly PageCurlData _pageCurl;
        private readonly BookAnimationData _animation;

        // Adjust this for GSQ refresh rate check
        private const int CONDITION_REFRESH_INTERVAL = 6;
        private int _conditionRefreshTimer = CONDITION_REFRESH_INTERVAL;

        // How long the exit button has to be held to leave a page that has taken the button over
        private const float FORCE_CLOSE_HOLD_DURATION = 3000f;
        private float _forceCloseHoldTimer = 0f;
        private bool _isExitButtonSuppressed = false;

        private readonly List<Rectangle> _openFrames = new List<Rectangle>();
        private readonly List<Rectangle> _closeFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageCurlFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageTurnFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageTurnFramesReversed = new List<Rectangle>();

        private Vector2 _currentPosition;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;

        private Rectangle _previousPageHotspot;
        private Rectangle _nextPageHotspot;

        private readonly Color _bookTintColor;
        private readonly List<Page> _pages;

        private int _currentChapterIndex = 0;
        private int _pendingChapterIndex;

        private int _currentSpread = 0;
        private int _pendingSpread;
        private bool _isTurningForward;

        // Where the reader has been this session, most recent last, so GoBack can retrace its way out of a chain of jumps
        private const int HISTORY_LIMIT = 64;
        private readonly List<(int ChapterIndex, int Spread)> _history = new List<(int ChapterIndex, int Spread)>();

        private Element? _hoveredElement;

        private Element? _focusedElement;
        private InputTextSubscriber? _focusedInput;

        private bool _isHoveringLeftPage;
        private bool _isHoveringRightPage;

        private bool _isHoveringPreviousPage;
        private bool _isHoveringNextPage;

        private Texture2D _pageCurlTexture;
        private Texture2D _bookTexture;
        private Texture2D? _bookGrayscaleTexture;

        private readonly bool _previousHudState;

        public BookMenu(Book book) : base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: false)
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
            base.xPositionOnScreen = (int)topLeft.X;
            base.yPositionOnScreen = (int)topLeft.Y;

            Book = book;
            _bookTintColor = ResolveBookTintColor(book.Data);
            _pages = book.Pages;

            _appearance = book.Data.Appearance;
            _pageCurl = book.Data.PageCurl;
            _animation = book.Data.Animation;

            _bookTexture = Parchment.modHelper.GameContent.Load<Texture2D>(_appearance.TexturePath);
            _bookGrayscaleTexture = string.IsNullOrWhiteSpace(_appearance.GrayscaleTexturePath) ? null : Parchment.modHelper.GameContent.Load<Texture2D>(_appearance.GrayscaleTexturePath);
            _pageCurlTexture = Parchment.modHelper.GameContent.Load<Texture2D>(_pageCurl.TexturePath);

            for (int frameIndex = 0; frameIndex < _appearance.OpenFrameCount; frameIndex++)
            {
                _openFrames.Add(new Rectangle(_appearance.FrameWidth * frameIndex, 0, _appearance.FrameWidth, _appearance.FrameHeight));
            }
            _closeFrames = Enumerable.Reverse(_openFrames).ToList();

            for (int frameIndex = 0; frameIndex < _pageCurl.FrameCount; frameIndex++)
            {
                _pageCurlFrames.Add(new Rectangle(_pageCurl.FrameWidth * frameIndex, 0, _pageCurl.FrameWidth, _pageCurl.FrameHeight));
            }

            for (int frameIndex = _appearance.OpenFrameCount; frameIndex < _appearance.OpenFrameCount + _appearance.TurnFrameCount; frameIndex++)
            {
                _pageTurnFrames.Add(new Rectangle(_appearance.FrameWidth * frameIndex, 0, _appearance.FrameWidth, _appearance.FrameHeight));
            }
            _pageTurnFramesReversed = Enumerable.Reverse(_pageTurnFrames).ToList();

            // Cache HUD state
            _previousHudState = Game1.displayHUD;
            Game1.displayHUD = false;

            DetermineSlidePositions();
            DetermineHotspotPositions();
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
            if (CurrentState is not MenuState.Ready)
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
            if (CurrentState is not MenuState.Ready)
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
            if (CurrentState is not MenuState.Ready)
            {
                error = "The book is not ready";
                return false;
            }

            if (Book.TryGetChapterIndex(chapterId, out int chapterIndex) is false)
            {
                error = $"There is no chapter '{chapterId}'";
                return false;
            }

            if (chapterIndex != _currentChapterIndex || _currentSpread != 0)
            {
                BeginPageTurn(chapterIndex, 0, skipAnimation: skipAnimation);
            }

            error = null;

            return true;
        }

        public bool TryJumpToPage(int pageIndex, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready)
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
            int targetSpread = (pageIndex - GetChapter(chapterIndex).FirstPageIndex) / 2;

            if (chapterIndex != _currentChapterIndex || targetSpread != _currentSpread)
            {
                BeginPageTurn(chapterIndex, targetSpread, skipAnimation: skipAnimation);
            }

            error = null;

            return true;
        }

        public bool TryJumpToChapterPage(string chapterId, int pageInChapter, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready)
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

            int targetSpread = pageInChapter / 2;

            if (chapterIndex != _currentChapterIndex || targetSpread != _currentSpread)
            {
                BeginPageTurn(chapterIndex, targetSpread, skipAnimation: skipAnimation);
            }

            error = null;

            return true;
        }

        public bool TryJumpToPageId(string pageId, out string error, bool skipAnimation = false)
        {
            return TryJumpToPageId(null, pageId, out error, skipAnimation);
        }

        public bool TryJumpToPageId(string chapterId, string pageId, out string error, bool skipAnimation = false)
        {
            if (CurrentState is not MenuState.Ready)
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
            int spread = (pageIndex - GetChapter(chapterIndex).FirstPageIndex) / 2;

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

            ApplyInitialSpread(chapterIndex, pageInChapter / 2);
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
            int spread = (pageIndex - GetChapter(chapterIndex).FirstPageIndex) / 2;

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

            _currentPosition = _startPosition;
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

        private int GetPageIndex(int chapterIndex, int spread, bool left)
        {
            Chapter chapter = GetChapter(chapterIndex);
            int pageIndex = chapter.FirstPageIndex + spread * 2 + (left ? 0 : 1);

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

        /// <summary>The right page's content area on screen, inside the book's margins.</summary>
        public Rectangle GetRightPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            int spineX = bookBounds.X + bookBounds.Width / 2;

            int marginTop = (int)(Book.Data.Layout.MarginTop * _appearance.Scale);
            int marginSpine = (int)(Book.Data.Layout.MarginSpine * _appearance.Scale);

            Point pageSize = PageLayoutHelper.GetPageContentSize(bookBounds.Width, bookBounds.Height, Book.Data.Layout, _appearance.Scale);

            return new Rectangle(spineX + marginSpine, bookBounds.Y + marginTop, pageSize.X, pageSize.Y);
        }

        private void UpdateCornerAnimation(ref float animationTimer, ref int currentFrame, bool isHovering, float elapsedMilliseconds)
        {
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

            if (skipAnimation is false)
            {
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
            if (CurrentState is not MenuState.Ready and not MenuState.Cover)
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
                return;
            }

            _hoveredElement.IsHovered = true;
            RunHoverActions(_hoveredElement);
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

            ClearHoverState();

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
                if (string.IsNullOrWhiteSpace(trigger.Condition) is false && GameStateQuery.CheckConditions(trigger.Condition) is false)
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
        /// </summary>
        private bool HandleKeybinds(SButton button)
        {
            if (CurrentState is not MenuState.Ready || button is SButton.None)
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
            if (pageIndex >= _pages.Count || _pages[pageIndex] is null)
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

                if (string.IsNullOrWhiteSpace(keybind.Condition) is false && GameStateQuery.CheckConditions(keybind.Condition) is false)
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

            // Assigning the subscriber is what starts the game routing characters here. The dispatcher owns the Selected flag on both the old and new subscriber
            Game1.keyboardDispatcher.Subscriber = _focusedInput;
        }

        private void ClearInputFocus()
        {
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

        /// <summary>Refreshes conditions the moment an input changes, so a list filtered on the typed text keeps up with the reader rather than waiting for the next condition tick.</summary>
        private void OnInputTextChanged()
        {
            RefreshVisiblePages();
        }

        private void ClearHoverState()
        {
            SetHoveredElement(null);

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
                if (element.IsVisible is false || element.Data is not ImageElementData imageData)
                {
                    continue;
                }

                AnimationFrameData? activeFrame = AnimationHelper.GetActiveFrame(element, imageData.FrameDuration);
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

        protected override void cleanupBeforeExit()
        {
            Game1.displayHUD = _previousHudState;

            ClearInputFocus();

            // Input text and flags are per reading session, so they don't survive the book being put down
            Parchment.inputManager.ClearAll();
            Parchment.flagManager.ClearAll();

            base.cleanupBeforeExit();
        }

        public override void emergencyShutDown()
        {
            Game1.displayHUD = _previousHudState;

            ClearInputFocus();
            Parchment.inputManager.ClearAll();
            Parchment.flagManager.ClearAll();

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
            if (CurrentState is MenuState.Cover)
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

            float elapsedMilliseconds = (float)time.ElapsedGameTime.TotalMilliseconds;

            // Conditions refresh in every state, so CurrentBookState works for all of them and there's no state where a condition goes stale
            UpdateConditionTimer();

            // Every tick rather than on the condition interval, as a frame shorter than that interval would otherwise be stepped over without ever being seen
            DispatchFrameActions();

            DispatchTextChangedActions(elapsedMilliseconds);

            RefreshResults();

            // Tracked in every state, since an action a keybind ran may have started an animation the reader is now holding the button through
            UpdateForceCloseHold(elapsedMilliseconds);

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

            if (CurrentState is (MenuState.Ready or MenuState.Cover) && _hoveredElement is not null && (string.IsNullOrEmpty(_hoveredElement.DisplayName) is false || string.IsNullOrEmpty(_hoveredElement.Description) is false))
            {
                if (string.IsNullOrEmpty(_hoveredElement.DisplayName) is false && string.IsNullOrEmpty(_hoveredElement.Description) is true)
                {
                    drawHoverText(b, _hoveredElement.DisplayName, Game1.smallFont);
                }
                else
                {
                    drawHoverText(b, _hoveredElement.Description, Game1.smallFont, boldTitleText: _hoveredElement.DisplayName);
                }
            }

            base.drawMouse(b, ignore_transparency: true);
        }

        private void DrawCorners(SpriteBatch b)
        {
            if (_currentSpread > 0)
            {
                b.Draw(_pageCurlTexture, new Vector2(_previousPageHotspot.X, _previousPageHotspot.Y), _pageCurlFrames[_previousCornerFrame], Color.White, 0f, Vector2.Zero, _pageCurl.Scale, SpriteEffects.FlipHorizontally, CURL_LAYER_DEPTH);
            }

            if (_currentSpread < GetSpreadCount() - 1)
            {
                b.Draw(_pageCurlTexture, new Vector2(_nextPageHotspot.X, _nextPageHotspot.Y), _pageCurlFrames[_nextCornerFrame], Color.White, 0f, Vector2.Zero, _pageCurl.Scale, SpriteEffects.None, CURL_LAYER_DEPTH);
            }

            if (Parchment.isDebugMode)
            {
                b.Draw(Game1.staminaRect, GetLeftPageBounds(), Color.Red * 0.4f);
                b.Draw(Game1.staminaRect, GetRightPageBounds(), Color.Red * 0.4f);
                b.Draw(Game1.staminaRect, _previousPageHotspot, Color.Cyan * 0.4f);
                b.Draw(Game1.staminaRect, _nextPageHotspot, Color.Cyan * 0.4f);
            }
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
