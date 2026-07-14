using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Enums;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Menus
{
    public class BookMenu : IClickableMenu
    {
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
        private const float OPEN_DURATION = 400f;
        private const float CURL_DURATION = 250f;
        private const float TURN_DURATION = 1000f;
        private const float CLOSE_DURATION = 400f;

        // Adjust this for page speed
        private const float CONTENT_SWAP_PROGRESS = 0.5f;

        // TODO: Adjust this and make it a parameter?
        private int PAGE_MARGIN_OUTER = 40;
        private int PAGE_MARGIN_SPINE = 24;
        private int PAGE_MARGIN_TOP = 32;
        private int PAGE_MARGIN_BOTTOM = 90;

        private readonly List<Rectangle> _openFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageCurlFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageTurnFrames = new List<Rectangle>();
        private readonly List<Rectangle> _pageTurnFramesReversed = new List<Rectangle>();

        private Vector2 _currentPosition;
        private Vector2 _startPosition;
        private Vector2 _targetPosition;

        private Rectangle _previousPageHotspot;
        private Rectangle _nextPageHotspot;

        private readonly List<PageEntry> _pages;
        private int _currentSpread = 0;
        private int _pendingSpread;
        private bool _isTurningForward;
        private int _pendingPage;

        private bool _isHoveringPreviousPage;
        private bool _isHoveringNextPage;

        private Texture2D _pageCurlTexture;
        private Texture2D _bookTexture;
        private Texture2D _bookGrayscaleTexture;

        private readonly bool _previousHudState;

        public BookMenu(List<PageEntry> pages) : base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: true)
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
            base.xPositionOnScreen = (int)topLeft.X;
            base.yPositionOnScreen = (int)topLeft.Y;
            base.upperRightCloseButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);

            _pages = pages;

            _pageCurlTexture = Parchment.modHelper.ModContent.Load<Texture2D>("Framework/Assets/curlPage.png");
            _bookTexture = Parchment.modHelper.ModContent.Load<Texture2D>("Framework/Assets/smallBook.png");
            _bookGrayscaleTexture = Parchment.modHelper.ModContent.Load<Texture2D>("Framework/Assets/smallBookGrayscale.png");

            // Cache HUD state
            _previousHudState = Game1.displayHUD;
            Game1.displayHUD = false;

            // Set open frames
            for (int i = 0; i < 4; i++)
            {
                _openFrames.Add(new Rectangle(219 * i, 0, 219, 158));
            }

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
            PAGE_MARGIN_OUTER = 55;
            PAGE_MARGIN_SPINE = 24;
            PAGE_MARGIN_TOP = 145;
            PAGE_MARGIN_BOTTOM = 75;
            Rectangle bookBounds = GetBookScreenBounds();
            return new Rectangle(bookBounds.X + PAGE_MARGIN_OUTER, bookBounds.Y + PAGE_MARGIN_TOP, bookBounds.Width / 2 - PAGE_MARGIN_OUTER - PAGE_MARGIN_SPINE, bookBounds.Height - PAGE_MARGIN_TOP - PAGE_MARGIN_BOTTOM);
        }

        private Rectangle GetRightPageBounds()
        {
            Rectangle bookBounds = GetBookScreenBounds();
            int spineX = bookBounds.X + bookBounds.Width / 2;
            return new Rectangle(spineX + PAGE_MARGIN_SPINE, bookBounds.Y + PAGE_MARGIN_TOP, bookBounds.Width / 2 - PAGE_MARGIN_OUTER - PAGE_MARGIN_SPINE, bookBounds.Height - PAGE_MARGIN_TOP - PAGE_MARGIN_BOTTOM);
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

        private void BeginPageTurn(bool forward)
        {
            _menuState = MenuState.Turning;
            _animationTimer = 0f;
            _animationFrame = 0;
            _isTurningForward = forward;
            _pendingSpread = forward ? _currentSpread + 1 : _currentSpread - 1;

            // Stop the curl instantly
            _previousCornerFrame = 0;
            _nextCornerFrame = 0;
            _isHoveringPreviousPage = false;
            _isHoveringNextPage = false;

            Game1.playSound("shwip");
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

        public override void receiveKeyPress(Keys key)
        {
            if (_menuState != MenuState.Ready)
            {
                if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                {
                    exitThisMenu();
                }

                return;
            }

            base.receiveKeyPress(key);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (_menuState == MenuState.Sliding || _menuState == MenuState.Opening)
            {
                // Skip intro
                _currentPosition = _targetPosition;
                _menuState = MenuState.Ready;
                _animationTimer = OPEN_DURATION;
                return;
            }

            if (_menuState == MenuState.Turning)
            {
                _currentSpread = _pendingSpread;
                _menuState = MenuState.Ready;
                _animationTimer = 0f;
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
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (_menuState == MenuState.Sliding)
            {
                _animationTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

                float progress = Math.Clamp(_animationTimer / SLIDE_DURATION, 0f, 1f);

                //  Ease out for a fast start but soft landing
                float easedProgress = 1f - (1f - progress) * (1f - progress);

                _currentPosition = Vector2.Lerp(_startPosition, _targetPosition, easedProgress);
                if (_animationTimer >= SLIDE_DURATION)
                {
                    _currentPosition = _targetPosition;
                    _menuState = MenuState.Opening;
                    _animationTimer = 0f;
                    Game1.playSound("shwip");
                }
            }
            else if (_menuState == MenuState.Opening)
            {
                _animationTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

                // Advance frames evenly across the duration
                _animationFrame = Math.Min((int)(_animationTimer / 250f * _openFrames.Count), _openFrames.Count - 1);

                if (_animationTimer >= 250f)
                {
                    _menuState = MenuState.Ready;
                    Game1.playSound("shwip");
                }
            }
            else if (_menuState == MenuState.Ready)
            {
                float elapsed = (float)time.ElapsedGameTime.TotalMilliseconds;

                UpdateCornerAnimation(ref _nextCornerAnimationTimer, ref _nextCornerFrame, _isHoveringNextPage, elapsed);
                UpdateCornerAnimation(ref _previousCornerAnimationTimer, ref _previousCornerFrame, _isHoveringPreviousPage, elapsed);
            }
            else if (_menuState == MenuState.Turning)
            {
                _animationTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

                _animationFrame = Math.Min((int)(_animationTimer / TURN_DURATION * _pageTurnFrames.Count), _pageTurnFrames.Count - 1);

                if (_animationTimer >= TURN_DURATION)
                {
                    _currentSpread = _pendingSpread;
                    _menuState = MenuState.Ready;
                    _animationTimer = 0f;
                }
            }
            else if (_menuState == MenuState.Closing)
            {
                _animationTimer += (float)time.ElapsedGameTime.TotalMilliseconds;

                // Run the frames backwards
                float progress = Math.Clamp(_animationTimer / CLOSE_DURATION, 0f, 1f);

                _animationFrame = Math.Max((int)((1f - progress) * _openFrames.Count), 0);
                _animationFrame = Math.Min(_animationFrame, _openFrames.Count - 1);

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

            //Vector2 centerPosition = new Vector2(xPositionOnScreen + width / 2, yPositionOnScreen + height / 2);
            //Vector2 drawPosition = new Vector2(centerPosition.X - (textureBounds.Width * 4f) / 2f, centerPosition.Y - (textureBounds.Height * 4f) / 2f);

            b.Draw(_bookGrayscaleTexture, _currentPosition, textureBounds, Color.Brown, 0f, BOOK_ORIGIN, BOOK_SCALE, SpriteEffects.None, 0.86f);
            b.Draw(_bookTexture, _currentPosition, textureBounds, Color.White, 0f, BOOK_ORIGIN, BOOK_SCALE, SpriteEffects.None, 0.86f);

            if (_menuState is MenuState.Ready or MenuState.Turning)
            {
                DrawPages(b);

                if (_menuState is MenuState.Ready)
                {
                    DrawCorners(b);
                }
            }

            base.draw(b);
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
            b.Draw(Game1.staminaRect, GetLeftPageBounds(), Color.Red * 0.4f);
            b.Draw(Game1.staminaRect, GetRightPageBounds(), Color.Red * 0.4f);
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

        private void DrawPage(SpriteBatch b, int pageIndex, Rectangle bounds)
        {
            if (pageIndex >= _pages.Count)
            {
                return;
            }

            PageEntry entry = _pages[pageIndex];
            PageData data = entry.Data;

            switch (data.Type)
            {
                case PageType.Title:
                    SpriteText.drawStringHorizontallyCenteredAt(b, data.Title ?? string.Empty, bounds.Center.X, bounds.Y + 32);
                    break;

                case PageType.Text:
                    if (data.Title is not null)
                    {
                        Utility.drawTextWithShadow(b, data.Title, Game1.dialogueFont, new Vector2(bounds.X, bounds.Y), Game1.textColor);
                    }
                    if (data.Text is not null)
                    {
                        string wrappedText = Game1.parseText(data.Text, Game1.smallFont, bounds.Width);
                        Utility.drawTextWithShadow(b, wrappedText, Game1.smallFont, new Vector2(bounds.X, bounds.Y + 64), Game1.textColor);
                    }
                    break;

                case PageType.Image:
                    Texture2D? imageTexture = entry.GetImageTexture();
                    if (imageTexture is not null)
                    {
                        Rectangle sourceRectangle = data.ImageSourceRectangle ?? new Rectangle(0, 0, imageTexture.Width, imageTexture.Height);

                        Vector2 imagePosition = new Vector2(bounds.Center.X - (sourceRectangle.Width * data.ImageScale) / 2f, bounds.Center.Y - (sourceRectangle.Height * data.ImageScale) / 2f);
                        b.Draw(imageTexture, imagePosition, sourceRectangle, Color.White, 0f, Vector2.Zero, data.ImageScale, SpriteEffects.None, 0.9f);
                    }
                    break;

                case PageType.Unknown:
                default:
                    // Unhandled, don't render
                    break;
            }
        }
    }
}
