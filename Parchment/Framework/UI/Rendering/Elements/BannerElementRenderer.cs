using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class BannerElementRenderer : TextElementRenderer<BannerElementData>
    {
        private const float LAYER_DEPTH = 0.86f;

        protected override string GetText(BannerElementData data)
        {
            return data.Text ?? string.Empty;
        }

        protected override Vector2 Measure(BannerElementData data, Element element, ElementRenderContext context)
        {
            element.LayoutState = null;

            if (element.Font is null || element.Texture is null || element.Texture.IsDisposed)
            {
                return Vector2.Zero;
            }

            Rectangle sourceRectangle = data.TextureSourceRectangle ?? element.Texture.Bounds;
            int unscaledCapWidth = data.CapWidth ?? (int)(sourceRectangle.Width / 3f);

            if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0 || unscaledCapWidth <= 0 || unscaledCapWidth * 2 >= sourceRectangle.Width)
            {
                Parchment.monitor.LogOnce($"Banner has an unusable source rectangle ({sourceRectangle.Width}x{sourceRectangle.Height}) or cap width ({unscaledCapWidth}); element will not render.", LogLevel.Warn);
                return Vector2.Zero;
            }

            int capWidth = (int)(unscaledCapWidth * data.Scale);
            int padding = (int)(data.Padding * data.Scale);
            float bannerHeight = sourceRectangle.Height * data.Scale;
            float maximumTextWidth = Math.Max(0f, context.AvailableWidth - (capWidth + padding) * 2f);
            float maximumTextHeight = Math.Max(0f, bannerHeight - padding * 2f);

            float textScale = data.TextScale;

            if (textScale <= 0f)
            {
                float naturalLineHeight = element.Font.MeasureString("Ag", 1f).Y;
                textScale = naturalLineHeight > 0f ? Math.Max(1f, (float)Math.Floor(maximumTextHeight / naturalLineHeight)) : 1f;
            }

            WrappedText wrappedText = TextWrapper.Wrap(GetText(data), element.Font, maximumTextWidth, textScale);
            if (wrappedText.Size.Y > maximumTextHeight)
            {
                Parchment.monitor.LogOnce($"Banner text is {(int)wrappedText.Size.Y}px tall but the banner only has {(int)maximumTextHeight}px; text will overflow. Try a smaller {nameof(data.TextScale)} or a shorter font.", LogLevel.Warn);
            }

            float bannerWidth;
            switch (data.Sizing)
            {
                case SizingMode.Fixed:
                    bannerWidth = Math.Min(data.Width.Value * data.Scale + (capWidth + padding) * 2f, context.AvailableWidth);
                    break;
                case SizingMode.Fill:
                    bannerWidth = context.AvailableWidth;
                    break;
                default:
                    bannerWidth = Math.Min(wrappedText.Size.X + (capWidth + padding) * 2f, context.AvailableWidth);
                    break;
            }

            bannerWidth = Math.Max(bannerWidth, capWidth * 2f);

            Rectangle leftSource = new Rectangle(sourceRectangle.X, sourceRectangle.Y, unscaledCapWidth, sourceRectangle.Height);
            Rectangle middleSource = new Rectangle(sourceRectangle.X + unscaledCapWidth, sourceRectangle.Y, sourceRectangle.Width - unscaledCapWidth * 2, sourceRectangle.Height);
            Rectangle rightSource = new Rectangle(sourceRectangle.Right - unscaledCapWidth, sourceRectangle.Y, unscaledCapWidth, sourceRectangle.Height);

            element.LayoutState = new BannerLayout(wrappedText, textScale, leftSource, middleSource, rightSource, capWidth, padding);

            return new Vector2(bannerWidth, bannerHeight);
        }

        protected override void Draw(SpriteBatch spriteBatch, BannerElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.LayoutState is not BannerLayout bannerLayout)
            {
                return;
            }

            if (element.Texture is null || element.Texture.IsDisposed)
            {
                return;
            }

            spriteBatch.Draw(element.Texture, new Vector2(bounds.X, bounds.Y), bannerLayout.LeftSource, Color.White, 0f, Vector2.Zero, data.Scale, SpriteEffects.None, LAYER_DEPTH);

            Rectangle middleDestination = new Rectangle(bounds.X + bannerLayout.CapWidth, bounds.Y, bounds.Width - bannerLayout.CapWidth * 2, bounds.Height);
            spriteBatch.Draw(element.Texture, middleDestination, bannerLayout.MiddleSource, Color.White, 0f, Vector2.Zero, SpriteEffects.None, LAYER_DEPTH);

            spriteBatch.Draw(element.Texture, new Vector2(bounds.Right - bannerLayout.CapWidth, bounds.Y), bannerLayout.RightSource, Color.White, 0f, Vector2.Zero, data.Scale, SpriteEffects.None, LAYER_DEPTH);

            Rectangle textBounds = new Rectangle(
                bounds.X + bannerLayout.CapWidth + bannerLayout.Padding,
                bounds.Y + (int)((bounds.Height - bannerLayout.WrappedText.Size.Y) / 2f),
                bounds.Width - (bannerLayout.CapWidth + bannerLayout.Padding) * 2,
                (int)bannerLayout.WrappedText.Size.Y
            );

            StringHelper.DrawLines(spriteBatch, element, bannerLayout.WrappedText, textBounds, AlignmentType.Center, element.Color, bannerLayout.TextScale);
        }
    }
}
