using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities;
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
    public class ImageElementRenderer : ElementRenderer<ImageElementData>
    {
        private const float LAYER_DEPTH = 0.86f;

        protected override Vector2 Measure(ImageElementData data, Element element, ElementRenderContext context)
        {
            element.LayoutState = null;
            if (element.Texture is null || element.Texture.IsDisposed)
            {
                return Vector2.Zero;
            }

            Rectangle sourceRectangle = data.TextureSourceRectangle ?? element.Texture.Bounds;
            if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
            {
                Parchment.monitor.Log($"Image element has an empty source rectangle for '{data.TexturePath}' and will not render!", LogLevel.Warn);
                return Vector2.Zero;
            }

            float drawScale = data.Scale * data.Scale;
            if (sourceRectangle.Width * drawScale > context.AvailableWidth)
            {
                drawScale = context.AvailableWidth / sourceRectangle.Width;
            }

            Vector2 drawSize = new Vector2(sourceRectangle.Width * drawScale, sourceRectangle.Height * drawScale);
            WrappedText? wrappedText = MeasureText(data, element, drawSize);

            element.LayoutState = new ImageLayout(sourceRectangle, drawScale, drawSize, wrappedText, data.TextScale);

            return drawSize;
        }

        private static WrappedText? MeasureText(ImageElementData data, Element element, Vector2 drawSize)
        {
            if (string.IsNullOrEmpty(data.Text))
            {
                return null;
            }

            if (element.Font is null)
            {
                Parchment.monitor.LogOnce($"Image element has text but no resolved font; the text will not render.", LogLevel.Warn);
                return null;
            }

            WrappedText wrappedText = TextWrapper.Wrap(data.Text, element.Font, drawSize.X, data.TextScale);

            if (wrappedText.Size.Y > drawSize.Y)
            {
                Parchment.monitor.LogOnce($"Image text is {(int)wrappedText.Size.Y}px tall but the image is only {(int)drawSize.Y}px; the text will overflow. Try a smaller {nameof(data.TextScale)}.", LogLevel.Warn);
            }

            return wrappedText;
        }

        protected override void Draw(SpriteBatch spriteBatch, ImageElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.LayoutState is not ImageLayout imageLayout)
            {
                return;
            }

            if (element.Texture is null || element.Texture.IsDisposed)
            {
                return;
            }

            spriteBatch.Draw(element.Texture, new Vector2(bounds.X, bounds.Y), imageLayout.SourceRectangle, Color.White, 0f, Vector2.Zero, imageLayout.DrawScale, SpriteEffects.None, LAYER_DEPTH);

            if (imageLayout.WrappedText is null || element.Font is null)
            {
                return;
            }

            Rectangle textBounds = new Rectangle(bounds.X, bounds.Y + (int)((bounds.Height - imageLayout.WrappedText.Size.Y) / 2f), bounds.Width, (int)imageLayout.WrappedText.Size.Y);
            StringHelper.DrawLines(spriteBatch, element, imageLayout.WrappedText, textBounds, data.TextAlignment, element.Color, imageLayout.TextScale);
        }
    }
}
