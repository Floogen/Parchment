using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Rendering;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StardewValley.FarmerSprite;
using static System.Net.Mime.MediaTypeNames;

namespace Parchment.Framework.UI.Menus
{
    public class BookMenu : IClickableMenu
    {
        public Book Book { get; }

        private enum MenuState { Sliding, Opening, Ready, Turning, Closing }
        private MenuState _menuState = MenuState.Sliding;

        private float _animationTimer = 0f;
        private int _animationFrame = 0;

        // Curl corners animation state
        private float _previousCornerAnimationTimer = 0f;
        private float _nextCornerAnimationTimer = 0f;
        private int _previousCornerFrame = 0;
        private int _nextCornerFrame = 0;

        private const float BOOK_SCALE = 5f;
        private const float CURL_SCALE = 4f;
        private Vector2 BOOK_ORIGIN = new Vector2(20f, 24f);

        private const float SLIDE_DURATION = 350f;
        private const float OPEN_DURATION = 250f;
        private const float CURL_DURATION = 250f;
        private const float TURN_DURATION = 500f;
        private const float CLOSE_DURATION = 400f;

        // Adjust this for page speed
        private const float CONTENT_SWAP_PROGRESS = 0.5f;

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
        private readonly bool _debug = false; // TODO: Make this an option on the BookData or command?

        private int _currentSpread = 0;
        private int _pendingSpread;
        private bool _isTurningForward;
        private int _pendingPage;

        private Element? _hoveredElement;
        private bool _isHoveringPreviousPage;
        private bool _isHoveringNextPage;

        private Texture2D _pageCurlTexture;
        private Texture2D _bookTexture;
        private Texture2D _bookGrayscaleTexture;

        private readonly bool _previousHudState;

        public BookMenu(Book book) : base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: false)
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
            base.xPositionOnScreen = (int)topLeft.X;
            base.yPositionOnScreen = (int)topLeft.Y;

            Book = book;
            _bookTintColor = ResolveBookTintColor(book.Data);
            _pages = book.Pages;

            _pageCurlTexture = Parchment.modHelper.GameContent.Load<Texture2D>("Assets/PeacefulEnd.Parchment/curlPage");
            _bookTexture = Parchment.modHelper.GameContent.Load<Texture2D>("Assets/PeacefulEnd.Parchment/smallBook");
            _bookGrayscaleTexture = Parchment.modHelper.GameContent.Load<Texture2D>("Assets/PeacefulEnd.Parchment/smallBookGrayscale");

            // Cache HUD state
            _previousHudState = Game1.displayHUD;
            Game1.displayHUD = false;

            // Set open frames
            for (int i = 0; i < 4; i++)
            {
                _openFrames.Add(new Rectangle(219 * i, 0, 219, 158));
            }
            _closeFrames = Enumerable.Reverse(_openFrames).ToList();

            // Set page curl frames
            for (int i = 0; i < 7; i++)
            {
                _pageCurlFrames.Add(new Rectangle(32 * i, 0, 32, 32));
            }            

            // Set page turn frames
            for (int i = 4; i < 10; i++)
            {
                _pageTurnFrames.Add(new Rectangle(219 * i, 0, 219, 158));
            }
            _pageTurnFramesReversed = Enumerable.Reverse(_pageTurnFrames).ToList();

            DetermineSlidePositions();
            DetermineHotspotPositions();
        }

        // Public methods for action usage
        public bool TryTurnPage(bool forward, out string error)
        {
            if (_menuState is not MenuState.Ready)
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

        public bool TryJumpToPage(int pageIndex, out string error)
        {
            if (_menuState is not MenuState.Ready)
            {
                error = "The book is not ready";
                return false;
            }

            if (pageIndex < 0 || pageIndex >= _pages.Count)
            {
                error = $"Page index {pageIndex} is out of range (0-{_pages.Count - 1})";
                return false;
            }

            int targetSpread = pageIndex / 2;

            if (targetSpread != _currentSpread)
            {
                BeginPageTurn(targetSpread);
            }

            error = null;

            return true;
        }

        public bool TryJumpToLastPage(out string error)
        {
            return TryJumpToPage(_pages.Count - 1, out error);
        }

        public void BeginClose()
        {
            if (_menuState is MenuState.Closing)
            {
                return;
            }

            SetMenuState(MenuState.Closing);
        }

        // Start of internal logic
        private void DetermineSlidePositions()
        {
            float scale = 4f;
            Rectangle closedBookRectangle = _openFrames[0];
            _targetPosition = new Vector2(this.xPositionOnScreen + this.width / 2f - (closedBookRectangle.Width * scale) / 2f, this.yPositionOnScreen + this.height / 2f - (closedBookRectangle.Height * scale) / 2f);

            _startPosition = new Vector2(_targetPosition.X, Game1.uiViewport.Height + (closedBookRectangle.Height * scale));

            _currentPosition = _startPosition;
        }

        private Rectangle GetBookScreenBounds()
        {
            Rectangle bookFrame = _openFrames[0];

            return new Rectangle((int)(_targetPosition.X - BOOK_ORIGIN.X * BOOK_SCALE), (int)(_targetPosition.Y - BOOK_ORIGIN.Y * BOOK_SCALE), (int)(bookFrame.Width * BOOK_SCALE), (int)(bookFrame.Height * BOOK_SCALE));
        }

        private void DetermineHotspotPositions()
        {
            const int HOTSPOT_INSET_X = 42;
            const int HOTSPOT_INSET_Y = 67;

            Rectangle bookBounds = GetBookScreenBounds();
            int hotspotSize = (int)(_pageCurlFrames[0].Width * CURL_SCALE);

            _previousPageHotspot = new Rectangle(bookBounds.Left + 10, bookBounds.Bottom - hotspotSize - HOTSPOT_INSET_Y, hotspotSize, hotspotSize);
            _nextPageHotspot = new Rectangle(bookBounds.Right - hotspotSize - HOTSPOT_INSET_X, bookBounds.Bottom - hotspotSize - HOTSPOT_INSET_Y, hotspotSize, hotspotSize);
        }

        private int GetSpreadCount()
        {
            return (_pages.Count + 1) / 2;
        }

        private int GetLeftPageIndex()
        {
            return _currentSpread * 2;
        }

        private int GetRightPageIndex()
        {
            return _currentSpread * 2 + 1;
        }

        private Rectangle GetLeftPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            return new Rectangle(bookBounds.X + Book.Data.Layout.MarginOuter, bookBounds.Y + Book.Data.Layout.MarginTop, bookBounds.Width / 2 - Book.Data.Layout.MarginOuter - Book.Data.Layout.MarginSpine, bookBounds.Height - Book.Data.Layout.MarginTop - Book.Data.Layout.MarginBottom);
        }

        private Rectangle GetRightPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            int spineX = bookBounds.X + bookBounds.Width / 2;
            return new Rectangle(spineX + Book.Data.Layout.MarginSpine, bookBounds.Y + Book.Data.Layout.MarginTop, bookBounds.Width / 2 - Book.Data.Layout.MarginOuter - Book.Data.Layout.MarginSpine, bookBounds.Height - Book.Data.Layout.MarginTop - Book.Data.Layout.MarginBottom);
        }

        private void UpdateCornerAnimation(ref float animationTimer, ref int currentFrame, bool isHovering, float elapsedMilliseconds)
        {
            int lastFrame = _pageCurlFrames.Count - 1;

            float frameDuration = CURL_DURATION / _pageCurlFrames.Count;
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

        private void BeginPageTurn(int targetSpread)
        {
            _isTurningForward = targetSpread > _currentSpread;
            _pendingSpread = targetSpread;

            SetMenuState(MenuState.Turning);

            Game1.playSound("shwip");
        }

        private void BeginPageTurn(bool forward)
        {
            BeginPageTurn(forward ? _currentSpread + 1 : _currentSpread - 1);
        }

        private void CommitPageTurn()
        {
            _currentSpread = _pendingSpread;
        }

        private Element? GetElementAt(Point screenPosition)
        {
            if (_menuState is not MenuState.Ready)
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
            _menuState = menuState;
            _animationTimer = 0f;
            _animationFrame = 0;

            ClearHoverState();
        }

        private void ClearHoverState()
        {
            SetHoveredElement(null);

            _isHoveringPreviousPage = false;
            _isHoveringNextPage = false;
            _previousCornerFrame = 0;
            _nextCornerFrame = 0;
            _previousCornerAnimationTimer = 0f;
            _nextCornerAnimationTimer = 0f;
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
            return _menuState != MenuState.Closing || _animationTimer >= CLOSE_DURATION;
        }

        public override void receiveKeyPress(Keys key)
        {
            if (_menuState == MenuState.Closing)
            {
                exitThisMenu(playSound: false);
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
            {
                BeginClose();
                return;
            }

            if (_menuState != MenuState.Ready)
            {
                return;
            }

            base.receiveKeyPress(key);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (_menuState == MenuState.Closing)
            {
                exitThisMenu(playSound: false);
                return;
            }

            if (_menuState == MenuState.Sliding || _menuState == MenuState.Opening)
            {
                // Skip intro
                _currentPosition = _targetPosition;
                SetMenuState(MenuState.Ready);
                return;
            }

            if (_menuState == MenuState.Turning)
            {
                // Skip turn
                _currentSpread = _pendingSpread;
                SetMenuState(MenuState.Ready);
                return;
            }

            // Check for any button element
            Element? clickedElement = GetElementAt(new Point(x, y));
            if (clickedElement is not null && string.IsNullOrWhiteSpace(clickedElement.Data.Action) is false)
            {
                if (string.IsNullOrWhiteSpace(clickedElement.Data.Sound) is false)
                {
                    Game1.playSound(clickedElement.Data.Sound);
                }

                if (TriggerActionManager.TryRunAction(clickedElement.Data.Action, out string error, out Exception exception) is false)
                {
                    Parchment.monitor.Log($"Element action '{clickedElement.Data.Action}' failed: {error}", LogLevel.Warn);

                    if (exception is not null)
                    {
                        Parchment.monitor.Log(exception.ToString(), LogLevel.Trace);
                    }
                }

                return;
            }

            if (_previousPageHotspot.Contains(x, y) && _currentSpread > 0)
            {
                BeginPageTurn(forward: false); return;
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
            if (_menuState != MenuState.Ready)
            {
                return;
            }
        }

        public override void performHoverAction(int x, int y)
        {
            if (_menuState != MenuState.Ready)
            {
                return;
            }

            base.performHoverAction(x, y);

            _isHoveringPreviousPage = _previousPageHotspot.Contains(x, y) && _currentSpread > 0;
            _isHoveringNextPage = _nextPageHotspot.Contains(x, y) && _currentSpread < GetSpreadCount() - 1;

            SetHoveredElement(GetElementAt(new Point(x, y)));
        }

        public override void update(GameTime time)
        {
            base.update(time);

            float elapsedMilliseconds = (float)time.ElapsedGameTime.TotalMilliseconds;

            if (_menuState is MenuState.Sliding)
            {
                _animationTimer += elapsedMilliseconds;

                float progress = Math.Clamp(_animationTimer / SLIDE_DURATION, 0f, 1f);

                //  Ease out for a fast start but soft landing
                float easedProgress = 1f - (1f - progress) * (1f - progress);

                _currentPosition = Vector2.Lerp(_startPosition, _targetPosition, easedProgress);

                if (_animationTimer >= SLIDE_DURATION)
                {
                    _currentPosition = _targetPosition;

                    SetMenuState(MenuState.Opening);
                    Game1.playSound("shwip");
                }
            }
            else if (_menuState is MenuState.Opening)
            {
                _animationTimer += elapsedMilliseconds;

                // Advance frames evenly across the duration
                _animationFrame = Math.Min((int)(_animationTimer / OPEN_DURATION * _openFrames.Count), _openFrames.Count - 1);

                if (_animationTimer >= OPEN_DURATION)
                {
                    SetMenuState(MenuState.Ready);
                    Game1.playSound("shwip");
                }
            }
            else if (_menuState is MenuState.Ready)
            {
                UpdateCornerAnimation(ref _nextCornerAnimationTimer, ref _nextCornerFrame, _isHoveringNextPage, elapsedMilliseconds);
                UpdateCornerAnimation(ref _previousCornerAnimationTimer, ref _previousCornerFrame, _isHoveringPreviousPage, elapsedMilliseconds);
            }
            else if (_menuState is MenuState.Turning)
            {
                _animationTimer += elapsedMilliseconds;

                _animationFrame = Math.Min((int)(_animationTimer / TURN_DURATION * _pageTurnFrames.Count), _pageTurnFrames.Count - 1);

                if (_animationTimer >= TURN_DURATION)
                {
                    CommitPageTurn();
                    SetMenuState(MenuState.Ready);
                }
            }
            else if (_menuState is MenuState.Closing)
            {
                _animationTimer += elapsedMilliseconds;

                // Run the frames backwards
                _animationFrame = Math.Min((int)(_animationTimer / CLOSE_DURATION * _closeFrames.Count), _closeFrames.Count - 1);

                if (_animationTimer >= CLOSE_DURATION)
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

            var textureBounds = new Rectangle(219 * 3, 0, 219, 158);
            if (_menuState == MenuState.Sliding)
            {
                textureBounds = _openFrames[0];
            }
            else if (_menuState == MenuState.Opening)
            {
                textureBounds = _openFrames[_animationFrame];
            }
            else if (_menuState == MenuState.Turning)
            {
                textureBounds = _isTurningForward ? _pageTurnFrames[_animationFrame] : _pageTurnFramesReversed[_animationFrame];
            }
            else if (_menuState == MenuState.Closing)
            {
                textureBounds = _closeFrames[_animationFrame];
            }

            Rectangle liveBookBounds = GetLiveBookScreenBounds();
            ElementRenderContext bookContext = EnsureBookLayout();

            DrawElements(b, Book.Underlay, liveBookBounds, bookContext);

            b.Draw(_bookGrayscaleTexture, _currentPosition, textureBounds, _bookTintColor, 0f, BOOK_ORIGIN, BOOK_SCALE, SpriteEffects.None, 0.86f);
            b.Draw(_bookTexture, _currentPosition, textureBounds, Color.White, 0f, BOOK_ORIGIN, BOOK_SCALE, SpriteEffects.None, 0.86f);

            if (_menuState is MenuState.Ready or MenuState.Turning)
            {
                DrawPages(b);

                if (_menuState is MenuState.Ready)
                {
                    DrawCorners(b);
                }

                DrawElements(b, Book.Overlay, liveBookBounds, bookContext);
            }

            base.draw(b);

            if (_menuState is MenuState.Ready && _hoveredElement is not null && string.IsNullOrEmpty(_hoveredElement.Data.Description) is false)
            {
                IClickableMenu.drawHoverText(b, _hoveredElement.Data.Description, Game1.smallFont);
            }

            base.drawMouse(b, ignore_transparency: true);
        }

        private void DrawCorners(SpriteBatch b)
        {
            Vector2 previousCornerPosition = new Vector2(_previousPageHotspot.Left, _previousPageHotspot.Bottom - (_pageCurlFrames[0].Height * 4f));
            Vector2 nextCornerPosition = new Vector2(_nextPageHotspot.Right - (_pageCurlFrames[0].Width * 4f), _nextPageHotspot.Bottom - (_pageCurlFrames[0].Height * 4f));

            if (_currentSpread > 0)
            {
                b.Draw(_pageCurlTexture, previousCornerPosition, _pageCurlFrames[_previousCornerFrame], Color.White, 0f, Vector2.Zero, 5f, SpriteEffects.FlipHorizontally, 0.99f);
            }
            if (_currentSpread < GetSpreadCount() - 1)
            {
                b.Draw(_pageCurlTexture, nextCornerPosition, _pageCurlFrames[_nextCornerFrame], Color.White, 0f, Vector2.Zero, 5f, SpriteEffects.None, 0.99f);
            }

            if (_debug is true)
            {
                b.Draw(Game1.staminaRect, GetLeftPageBounds(), Color.Red * 0.4f);
                b.Draw(Game1.staminaRect, GetRightPageBounds(), Color.Red * 0.4f);
            }
        }

        private void DrawPages(SpriteBatch b)
        {
            if (_menuState != MenuState.Turning)
            {
                DrawSide(b, _currentSpread, left: true);
                DrawSide(b, _currentSpread, left: false);
                return;
            }

            float turnProgress = Math.Clamp(_animationTimer / TURN_DURATION, 0f, 1f);
            bool hasSwapped = turnProgress >= CONTENT_SWAP_PROGRESS;

            // The swept side (right when forward, left when backward): blank until swap then NEW content
            // The stationary side: Old content until swap then blank until landing
            bool leftIsSwept = !_isTurningForward;

            if (leftIsSwept)
            {
                if (hasSwapped)
                {
                    DrawSide(b, _pendingSpread, left: true);
                }
            }
            else if (!hasSwapped)
            {
                DrawSide(b, _currentSpread, left: true);
            }

            if (!leftIsSwept)
            {
                if (hasSwapped)
                {
                    DrawSide(b, _pendingSpread, left: false);
                }
            }
            else if (!hasSwapped)
            {
                DrawSide(b, _currentSpread, left: false);
            }
        }

        private void DrawSide(SpriteBatch b, int spread, bool left)
        {
            int pageIndex = spread * 2 + (left ? 0 : 1);
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

                if (_debug)
                {
                    b.Draw(Game1.staminaRect, screenBounds, Color.Lime * 0.3f);
                }
            }
        }

        private ElementRenderContext EnsureLayout(Page page, Rectangle pageContentBounds)
        {
            ElementRenderContext context = this.BuildRenderContext(pageContentBounds);

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
