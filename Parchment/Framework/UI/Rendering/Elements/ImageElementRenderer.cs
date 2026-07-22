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

            if (SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle || sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
            {
                Parchment.monitor.Log($"Image element has an empty source rectangle for '{data.TexturePath}' and will not render!", LogLevel.Warn);
                return Vector2.Zero;
            }

            float drawScale = GetDrawScale(data, sourceRectangle, context.AvailableWidth);
            if (sourceRectangle.Width * drawScale > context.AvailableWidth)
            {
                drawScale = context.AvailableWidth / sourceRectangle.Width;
            }

            Vector2 drawSize = new Vector2(sourceRectangle.Width * drawScale, sourceRectangle.Height * drawScale);
            Rectangle textArea = GetScaledTextArea(data, sourceRectangle, drawScale);
            WrappedText? wrappedText = MeasureText(data, element, textArea);

            element.LayoutState = new ImageLayout(sourceRectangle, drawScale, drawSize, textArea, wrappedText, data.TextScale);

            return drawSize;
        }

        private static float GetDrawScale(ImageElementData data, Rectangle sourceRectangle, float availableWidth)
        {
            if (sourceRectangle.Width * data.Scale <= availableWidth)
            {
                return data.Scale;
            }

            float fittedScale = availableWidth / sourceRectangle.Width;
            float snappedScale = (float)Math.Floor(fittedScale);

            Parchment.monitor.LogOnce($"Image '{data.TexturePath}' is {(int)(sourceRectangle.Width * data.Scale)}px wide at {nameof(data.Scale)} {data.Scale}, but only {(int)availableWidth}px is available; it will be scaled down to fit.", LogLevel.Warn);

            return snappedScale >= 1f ? snappedScale : fittedScale;
        }

        private static Rectangle GetScaledTextArea(ImageElementData data, Rectangle sourceRectangle, float drawScale)
        {
            if (data.TextArea is not Rectangle textArea)
            {
                return new Rectangle(0, 0, (int)(sourceRectangle.Width * drawScale), (int)(sourceRectangle.Height * drawScale));
            }

            return new Rectangle((int)(textArea.X * drawScale), (int)(textArea.Y * drawScale), (int)(textArea.Width * drawScale), (int)(textArea.Height * drawScale));
        }

        private static WrappedText? MeasureText(ImageElementData data, Element element, Rectangle textArea)
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

            WrappedText wrappedText = TextWrapper.Wrap(data.Text, element.Font, textArea.Width, data.TextScale);
            if (wrappedText.Size.Y > textArea.Height)
            {
                Parchment.monitor.LogOnce($"Image text is {(int)wrappedText.Size.Y}px tall but the text area is only {(int)textArea.Height}px; the text will overflow. Try a smaller {nameof(data.TextScale)}.", LogLevel.Warn);
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

            if (SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle)
            {
                return;
            }

            Rectangle frameRectangle = AnimationHelper.GetFrame(sourceRectangle, data.Frames, data.FrameDuration);
            spriteBatch.Draw(element.Texture, new Vector2(bounds.X, bounds.Y), frameRectangle, element.TintColor, 0f, Vector2.Zero, imageLayout.DrawScale, data.SpriteEffects, LAYER_DEPTH);

            if (imageLayout.WrappedText is null)
            {
                return;
            }

            Rectangle textRegion = new Rectangle(bounds.X + imageLayout.TextArea.X, bounds.Y + imageLayout.TextArea.Y, imageLayout.TextArea.Width, imageLayout.TextArea.Height);
            Rectangle textBounds = new Rectangle(textRegion.X, textRegion.Y + (int)((textRegion.Height - imageLayout.WrappedText.Size.Y) / 2f), textRegion.Width, (int)imageLayout.WrappedText.Size.Y);

            StringHelper.DrawLines(spriteBatch, element, imageLayout.WrappedText, textBounds, data.TextAlignment, element.TextColor, imageLayout.TextScale);
        }
    }
}
