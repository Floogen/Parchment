using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Books;
using Parchment.Framework.Models.Data.Elements;
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

        public enum MenuState { Sliding, Opening, Ready, Turning, Closing }
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
        private int _conditionRefreshTimer = 0;

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

        private Element? _hoveredElement;

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

        public bool IsOnPage(string pageId)
        {
            return GetPageId(GetLeftPageIndex()).EqualsIgnoreCase(pageId) || GetPageId(GetRightPageIndex()).EqualsIgnoreCase(pageId);
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
        public bool TryTurnPage(bool forward, out string error)
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

            BeginPageTurn(targetSpread);
            error = null;

            return true;
        }

        public bool TryJumpToChapter(string chapterId, out string error)
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
                BeginPageTurn(chapterIndex, 0);
            }

            error = null;

            return true;
        }

        public bool TryJumpToPage(int pageIndex, out string error)
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
                BeginPageTurn(chapterIndex, targetSpread);
            }

            error = null;

            return true;
        }

        public bool TryJumpToFirstPage(out string error)
        {
            return TryJumpToPage(GetChapter(_currentChapterIndex).FirstPageIndex, out error);
        }

        public bool TryJumpToLastPage(out string error)
        {
            return TryJumpToPage(GetChapter(_currentChapterIndex).LastPageIndex, out error);
        }

        public void BeginClose()
        {
            if (CurrentState is MenuState.Closing)
            {
                return;
            }

            SetMenuState(MenuState.Closing);
            PlaySound(_animation.CloseSound);
        }

        // Start of internal logic
        private void DetermineSlidePositions()
        {
            Rectangle closedBookRectangle = _openFrames[0];

            float centeredX = base.xPositionOnScreen + base.width / 2f - (closedBookRectangle.Width * _appearance.Scale) / 2f;
            float centeredY = base.yPositionOnScreen + base.height / 2f - (closedBookRectangle.Height * _appearance.Scale) / 2f;

            _targetPosition = new Vector2(centeredX + _appearance.Offset.X * _appearance.Scale, centeredY + _appearance.Offset.Y * _appearance.Scale);
            _startPosition = new Vector2(_targetPosition.X, Game1.uiViewport.Height + (closedBookRectangle.Height * _appearance.Scale));

            _currentPosition = _startPosition;
        }

        private Rectangle GetBookScreenBounds()
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

        private Rectangle GetLeftPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();

            int marginOuter = (int)(Book.Data.Layout.MarginOuter * _appearance.Scale);
            int marginTop = (int)(Book.Data.Layout.MarginTop * _appearance.Scale);
            int marginBottom = (int)(Book.Data.Layout.MarginBottom * _appearance.Scale);
            int marginSpine = (int)(Book.Data.Layout.MarginSpine * _appearance.Scale);

            return new Rectangle(bookBounds.X + marginOuter, bookBounds.Y + marginTop, bookBounds.Width / 2 - marginOuter - marginSpine, bookBounds.Height - marginTop - marginBottom);
        }

        private Rectangle GetRightPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            int spineX = bookBounds.X + bookBounds.Width / 2;

            int marginOuter = (int)(Book.Data.Layout.MarginOuter * _appearance.Scale);
            int marginTop = (int)(Book.Data.Layout.MarginTop * _appearance.Scale);
            int marginBottom = (int)(Book.Data.Layout.MarginBottom * _appearance.Scale);
            int marginSpine = (int)(Book.Data.Layout.MarginSpine * _appearance.Scale);

            return new Rectangle(spineX + marginSpine, bookBounds.Y + marginTop, bookBounds.Width / 2 - marginOuter - marginSpine, bookBounds.Height - marginTop - marginBottom);
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

        private void BeginPageTurn(int targetChapterIndex, int targetSpread)
        {
            _isTurningForward = targetChapterIndex != _currentChapterIndex ? targetChapterIndex > _currentChapterIndex : targetSpread > _currentSpread;
            _pendingChapterIndex = targetChapterIndex;
            _pendingSpread = targetSpread;

            SetMenuState(MenuState.Turning);

            PlaySound(_animation.TurnSound);
        }

        private void BeginPageTurn(int targetSpread)
        {
            BeginPageTurn(_currentChapterIndex, targetSpread);
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
            if (CurrentState is not MenuState.Ready)
            {
                return null;
            }

            Rectangle bookBounds = GetBookScreenBounds();

            Element? hitElement = Page.HitTest(Book.Overlay, bookBounds, screenPosition);
            hitElement ??= HitTestPage(GetLeftPageIndex(), GetLeftPageBounds(), screenPosition);
            hitElement ??= HitTestPage(GetRightPageIndex(), GetRightPageBounds(), screenPosition);

            return hitElement ?? Page.HitTest(Book.Underlay, bookBounds, screenPosition);
        }

        private Element? HitTestPage(int pageIndex, Rectangle pageBounds, Point screenPosition)
        {
            if (pageIndex >= _pages.Count)
            {
                return null;
            }

            return Page.HitTest(_pages[pageIndex].Elements, pageBounds, screenPosition);
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

            _hoveredElement = element;

            if (_hoveredElement is not null)
            {
                _hoveredElement.IsHovered = true;
            }
        }

        private void SetMenuState(MenuState menuState)
        {
            CurrentState = menuState;
            _animationTimer = 0f;
            _animationFrame = 0;

            ClearHoverState();

            if (menuState is MenuState.Ready)
            {
                RefreshVisiblePages();
            }
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
            base.cleanupBeforeExit();
        }

        public override void emergencyShutDown()
        {
            Game1.displayHUD = _previousHudState;
            base.emergencyShutDown();
        }

        public override bool readyToClose()
        {
            return CurrentState != MenuState.Closing || _animationTimer >= _animation.CloseDuration;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (CurrentState == MenuState.Closing)
            {
                exitThisMenu(playSound: false);
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
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

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (CurrentState == MenuState.Closing)
            {
                exitThisMenu(playSound: false);
                return;
            }

            if (CurrentState == MenuState.Sliding || CurrentState == MenuState.Opening)
            {
                // Skip intro
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
            if (clickedElement is not null && string.IsNullOrWhiteSpace(clickedElement.Data.Action) is false)
            {
                PlaySound(clickedElement.Data.Sound);

                if (TriggerActionManager.TryRunAction(clickedElement.Data.Action, out string error, out Exception exception) is false)
                {
                    Parchment.monitor.Log($"Element action '{clickedElement.Data.Action}' failed: {error}", LogLevel.Warn);

                    if (exception is not null)
                    {
                        Parchment.monitor.Log(exception.ToString(), LogLevel.Trace);
                    }
                }

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

                    SetMenuState(MenuState.Opening);
                    PlaySound(_animation.OpenSound);
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
                UpdateConditionTimer();

                UpdateCornerAnimation(ref _nextCornerAnimationTimer, ref _nextCornerFrame, _isHoveringNextPage, elapsedMilliseconds);
                UpdateCornerAnimation(ref _previousCornerAnimationTimer, ref _previousCornerFrame, _isHoveringPreviousPage, elapsedMilliseconds);
            }
            else if (CurrentState is MenuState.Turning)
            {
                UpdateConditionTimer();

                _animationTimer += elapsedMilliseconds;

                _animationFrame = Math.Min((int)(_animationTimer / _animation.TurnDuration * _pageTurnFrames.Count), _pageTurnFrames.Count - 1);

                if (_animationTimer >= _animation.TurnDuration)
                {
                    CommitPageTurn();
                    SetMenuState(MenuState.Ready);
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
            else if (CurrentState == MenuState.Closing)
            {
                textureBounds = _closeFrames[_animationFrame];
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

                DrawElements(b, Book.Overlay, liveBookBounds, bookContext);
            }

            base.draw(b);

            if (CurrentState is MenuState.Ready && _hoveredElement is not null && (string.IsNullOrEmpty(_hoveredElement.DisplayName) is false || string.IsNullOrEmpty(_hoveredElement.Description) is false))
            {
                drawHoverText(b, _hoveredElement.Description, Game1.smallFont, boldTitleText: _hoveredElement.DisplayName);
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
            ElementRenderContext context = BuildRenderContext(pageContentBounds);

            if (page.LastLayoutContext != context)
            {
                page.PerformLayout(context);
                page.LastLayoutContext = context;
            }

            return context;
        }

        private ElementRenderContext BuildRenderContext(Rectangle pageBounds)
        {
            return new ElementRenderContext(pageBounds.Width, pageBounds.Height);
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
            if (string.IsNullOrWhiteSpace(data.TintColor))
            {
                return Color.White;
            }

            if (ColorParser.TryParse(data.TintColor, out Color parsedColor) is false)
            {
                Parchment.monitor.Log($"Book '{data.Id}' has an unparsable {nameof(data.TintColor)} '{data.TintColor}'; the book will not be tinted.", LogLevel.Warn);
                return Color.White;
            }

            return parsedColor;
        }
    }
}