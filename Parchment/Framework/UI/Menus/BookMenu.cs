using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Rendering;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
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
        private float TURN_DURATION = 500f;
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

        private readonly List<Page> _pages;
        private readonly bool _debug = false; // TODO: Make this an option on the BookData or command?

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

        public BookMenu(Book book) : base((int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).X, (int)Utility.getTopLeftPositionForCenteringOnScreen(1280, 720).Y, 1280, 720, showUpperRightCloseButton: false)
        {
            Vector2 topLeft = Utility.getTopLeftPositionForCenteringOnScreen(base.width, base.height);
            base.xPositionOnScreen = (int)topLeft.X;
            base.yPositionOnScreen = (int)topLeft.Y;

            Book = book;
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

        private void BeginClose()
        {
            if (_menuState == MenuState.Closing)
            {
                return;
            }

            // TODO: Change this so _menuState is changed via method, which resets _animationFrame and _animationTimer
            _menuState = MenuState.Closing;
            _animationFrame = 0;
            _animationTimer = 0f;
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
                _animationFrame = Math.Min((int)(_animationTimer / OPEN_DURATION * _openFrames.Count), _openFrames.Count - 1);

                if (_animationTimer >= OPEN_DURATION)
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

                _animationFrame = Math.Min((int)(_animationTimer / OPEN_DURATION * _closeFrames.Count), _closeFrames.Count - 1);

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

            float currentY = pageBounds.Y;

            var page = _pages[pageIndex];

            ElementRenderContext context = EnsureLayout(page, pageBounds);
            foreach (var element in page.Elements)
            {
                Rectangle screenBounds = new Rectangle(pageBounds.X, element.Bounds.Y + pageBounds.Y, pageBounds.Width, pageBounds.Height);
                element.Renderer.Draw(b, element, screenBounds, context);

                currentY = screenBounds.Y;
                if (currentY > pageBounds.Bottom)
                {
                    // TODO: Handle moving overflow content to new page
                    break;
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

        private ElementRenderContext BuildRenderContext(Rectangle pageContentBounds)
        {
            return new ElementRenderContext()
            {
                AvailableWidth = pageContentBounds.Width
            };
        }

        private float DrawElement(SpriteBatch b, Page page, ElementData element, Rectangle bounds, float y)
        {
            return 0f;
            /*
            switch (element.Type)
            {
                case ElementType.Title:
                    {
                        string text = element.Text ?? string.Empty;
                        float textWidth = SpriteText.getWidthOfString(text);
                        float x = GetAlignedX(pageBounds, textWidth, element.Alignment);
                        SpriteText.drawString(b, text, (int)x, (int)y);
                        return SpriteText.getHeightOfString(text);
                    }
                case ElementType.Heading:
                    {
                        string text = element.Text ?? string.Empty;
                        Vector2 size = Game1.dialogueFont.MeasureString(text);
                        float x = GetAlignedX(pageBounds, size.X, element.Alignment);
                        Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(x, y), Game1.textColor);
                        return size.Y;
                    }
                case ElementType.Paragraph:
                    {
                        string wrapped = Game1.parseText(element.Text ?? string.Empty, Game1.smallFont, pageBounds.Width);
                        Vector2 size = Game1.smallFont.MeasureString(wrapped);
                        Utility.drawTextWithShadow(b, wrapped, Game1.smallFont, new Vector2(pageBounds.X, y), Game1.textColor);
                        return size.Y;
                    }
                case ElementType.Image:
                    {
                        Texture2D? texture = page.GetElementTexture(element);
                        if (texture is null)
                        {
                            return 0f;
                        }

                        Rectangle source = element.TextureSourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);
                        float drawnWidth = source.Width * element.Scale;
                        float x = GetAlignedX(pageBounds, drawnWidth, element.Alignment);
                        b.Draw(texture, new Vector2(x, y), source, Color.White, 0f, Vector2.Zero, element.Scale, SpriteEffects.None, 0.9f);
                        return source.Height * element.Scale;
                    }
                case ElementType.Divider:
                    {
                        int lineY = (int)y + 4;
                        b.Draw(Game1.staminaRect, new Rectangle(pageBounds.X + 16, lineY, pageBounds.Width - 32, 2), Game1.textColor * 0.4f);
                        return 10f;
                    }
                case ElementType.Panel:
                    {
                        var panel = element as PanelElementData;

                        float panelHeight = MeasureElement(page, element, pageBounds.Width);
                        int panelWidth = panel.Width ?? pageBounds.Width;
                        float x = GetAlignedX(pageBounds, panelWidth, element.Alignment);

                        if (element.TexturePath is null)
                        {
                            IClickableMenu.drawTextureBox(b, (int)x, (int)y, panelWidth, panel.Height, Color.White);
                        }
                        else
                        {
                            Texture2D? texture = page.GetElementTexture(element);
                            if (texture is null)
                            {
                                return 0f;
                            }

                            Rectangle sourceRectangle = element.TextureSourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);
                            IClickableMenu.drawTextureBox(b, texture, sourceRectangle, (int)x, (int)y, panelWidth, (int)panelHeight, Color.White, element.Scale, drawShadow: false);
                        }

                        if (panel.Children is not null)
                        {
                            int borderInset = GetPanelBorderInset(element);
                            Rectangle innerBounds = new Rectangle((int)x + borderInset, (int)y + borderInset, panelWidth - borderInset * 2, (int)panelHeight - (int)(borderInset * 2));

                            foreach (var child in panel.Children)
                            {
                                DrawElement(b, page, child, pageBounds, y);
                            }
                        }

                        return panelHeight;
                    }

                default:
                    return 0f;
            }
            */
        }

        private float GetAlignedX(Rectangle bounds, float contentWidth, AlignmentType alignment)
        {
            return alignment switch
            {
                AlignmentType.Center => bounds.X + (bounds.Width - contentWidth) / 2f,
                AlignmentType.Right => bounds.Right - contentWidth, _ => bounds.X
            };
        }

        private float MeasureElement(Page page, ElementData element, int availableWidth)
        {
            return 0f;
            /*
            switch (element.Type)
            {
                case ElementType.Title:
                    {
                        return SpriteText.getHeightOfString(element.Text ?? string.Empty);
                    }
                case ElementType.Heading:
                    {
                        return Game1.dialogueFont.MeasureString(element.Text ?? string.Empty).Y;
                    }

                case ElementType.Paragraph:
                    {
                        string wrappedText = Game1.parseText(element.Text ?? string.Empty, Game1.smallFont, availableWidth);
                        return Game1.smallFont.MeasureString(wrappedText).Y;
                    }

                case ElementType.Image:
                    {
                        Texture2D? texture = page.GetElementTexture(element);
                        if (texture is null)
                        {
                            return 0f;
                        }

                        Rectangle sourceRectangle = element.TextureSourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);
                        return sourceRectangle.Height * element.Scale;
                    }

                case ElementType.Divider:
                    {
                        return 10f;
                    }

                case ElementType.Panel:
                    {
                        var panel = element as PanelElementData;
                        if (panel.Children is null || panel.Children.Count == 0)
                        {
                            return panel.Height;
                        }

                        int borderInset = GetPanelBorderInset(element);
                        int innerWidth = (panel.Width ?? availableWidth) - borderInset * 2;
                        float childrenHeight = MeasureElementList(page, panel.Children, innerWidth);
                        return childrenHeight;// + borderInset;
                    }

                default:
                    {
                        return 0f;
                    }
            }
            */
        }

        private float MeasureElementList(Page page, List<ElementData> elements, int availableWidth)
        {
            float totalHeight = 0f;
            for (int i = 0; i < elements.Count; i++)
            {
                totalHeight += MeasureElement(page, elements[i], availableWidth);
                if (i < elements.Count - 1)
                {
                    totalHeight += elements[i].SpacingAfter;
                }
            }
            return totalHeight;
        }

        /*
        private int GetPanelBorderInset(ElementData element)
        {
            if (element.TexturePath is null)
            {
                return 16;
            }

            Rectangle sourceRectangle = element.TextureSourceRectangle ?? new Rectangle(0, 0, 48, 48);
            return (int)(Math.Min(sourceRectangle.Width, sourceRectangle.Height) / 3f * element.Scale);
        }
        */
    }
}
