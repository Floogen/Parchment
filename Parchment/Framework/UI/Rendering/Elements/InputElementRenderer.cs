using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class InputElementRenderer : ElementRenderer<InputElementData>
    {
        private const string LINE_HEIGHT_MEASURE_TEXT = "Ay";
        private const float CARET_BLINK_INTERVAL = 500f;
        private const float PLACEHOLDER_OPACITY = 0.5f;

        protected override Vector2 Measure(InputElementData data, Element element, ElementRenderContext context)
        {
            element.LayoutState = null;

            if (element.Font is null)
            {
                Parchment.monitor.LogOnce($"An Input element has no resolved font and will not render.", LogLevel.Warn);
                return Vector2.Zero;
            }

            // Laying the input out is the first point at which its ID is known to be on screen, so this is where its starting text is recorded
            Parchment.inputManager.Seed(data.InputId, data.Text);

            int borderThickness = 0;
            if (SpriteHelper.GetSourceRectangle(data, element) is Rectangle sourceRectangle && sourceRectangle.Width > 0 && sourceRectangle.Height > 0)
            {
                borderThickness = NineSliceHelper.GetBorderThickness(sourceRectangle, data.Scale);
            }

            int inset = borderThickness + (int)(data.Padding * data.Scale);
            float lineHeight = element.Font.MeasureString(LINE_HEIGHT_MEASURE_TEXT, data.TextScale).Y;

            float inputWidth;
            switch (data.Sizing)
            {
                case SizingMode.Fixed:
                    inputWidth = Math.Min(data.Width.Value * data.Scale + inset * 2f, context.AvailableWidth);
                    break;
                case SizingMode.ShrinkToFit:
                    inputWidth = Math.Min(GetPlaceholderWidth(data, element) + inset * 2f, context.AvailableWidth);
                    break;
                default:
                    inputWidth = context.AvailableWidth;
                    break;
            }

            // An authored Height scales the way Width does under Fixed sizing, so raising Scale grows the box on both axes rather than only across
            float contentHeight = data.Height is int height ? height * data.Scale : lineHeight;

            inputWidth = Math.Max(inputWidth, borderThickness * 2f);
            float inputHeight = Math.Max(contentHeight + inset * 2f, borderThickness * 2f);

            element.LayoutState = new InputLayout(inset, data.TextScale, lineHeight, ResolvePlaceholderColor(data, element));

            return new Vector2(inputWidth, inputHeight);
        }

        /// <summary>The width a ShrinkToFit input hugs. The typed text is deliberately not measured, as a box that grew and shrank while the reader typed would reflow the page under them.</summary>
        private static float GetPlaceholderWidth(InputElementData data, Element element)
        {
            if (element.Font is null || string.IsNullOrEmpty(data.Placeholder))
            {
                return 0f;
            }

            return element.Font.MeasureString(data.Placeholder, data.TextScale).X;
        }

        private static Color ResolvePlaceholderColor(InputElementData data, Element element)
        {
            if (string.IsNullOrWhiteSpace(data.PlaceholderColor))
            {
                return element.TextColor * PLACEHOLDER_OPACITY;
            }

            if (ColorParser.TryParse(data.PlaceholderColor, out Color parsedColor) is false)
            {
                Parchment.monitor.LogOnce($"Input '{data.InputId}' has an unparsable \"PlaceholderColor\" '{data.PlaceholderColor}'; the default will be used.", LogLevel.Warn);
                return element.TextColor * PLACEHOLDER_OPACITY;
            }

            return parsedColor;
        }

        protected override void Draw(SpriteBatch spriteBatch, InputElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.LayoutState is not InputLayout inputLayout || element.Font is null)
            {
                return;
            }

            if (element.Texture is not null && element.Texture.IsDisposed is false && SpriteHelper.GetDrawSourceRectangle(data, element) is Rectangle sourceRectangle)
            {
                IClickableMenu.drawTextureBox(spriteBatch, element.Texture, sourceRectangle, bounds.X, bounds.Y, bounds.Width, bounds.Height, element.TintColor, data.Scale, drawShadow: false);
            }

            Rectangle textBounds = new Rectangle(bounds.X + inputLayout.Inset, bounds.Y + (int)((bounds.Height - inputLayout.LineHeight) / 2f), Math.Max(0, bounds.Width - inputLayout.Inset * 2), (int)inputLayout.LineHeight);
            string text = Parchment.inputManager.GetText(data.InputId);

            if (string.IsNullOrEmpty(text) is true)
            {
                if (string.IsNullOrEmpty(data.Placeholder) is false)
                {
                    element.Font.DrawString(spriteBatch, Truncate(data.Placeholder, element, inputLayout, textBounds.Width), new Vector2(textBounds.X, textBounds.Y), inputLayout.PlaceholderColor, inputLayout.TextScale);
                }

                DrawCaret(spriteBatch, element, inputLayout, textBounds, 0f);
                return;
            }

            string visibleText = Truncate(text, element, inputLayout, textBounds.Width, keepEnd: true);
            element.Font.DrawString(spriteBatch, visibleText, new Vector2(textBounds.X, textBounds.Y), element.TextColor, inputLayout.TextScale);

            DrawCaret(spriteBatch, element, inputLayout, textBounds, element.Font.MeasureString(visibleText, inputLayout.TextScale).X);
        }

        /// <summary>Trims text to the width available. Typed text keeps its end, so the caret stays in view as the reader types, while a placeholder keeps its start.</summary>
        private static string Truncate(string? text, Element element, InputLayout inputLayout, int maximumWidth, bool keepEnd = false)
        {
            if (element.Font is null || string.IsNullOrEmpty(text) || maximumWidth <= 0)
            {
                return string.Empty;
            }

            string visibleText = text;
            while (visibleText.Length > 0 && element.Font.MeasureString(visibleText, inputLayout.TextScale).X > maximumWidth)
            {
                visibleText = keepEnd ? visibleText.Substring(1) : visibleText.Substring(0, visibleText.Length - 1);
            }

            return visibleText;
        }

        private static void DrawCaret(SpriteBatch spriteBatch, Element element, InputLayout inputLayout, Rectangle textBounds, float textWidth)
        {
            if (element.IsFocused is false || Game1.currentGameTime is null)
            {
                return;
            }

            if (Game1.currentGameTime.TotalGameTime.TotalMilliseconds % (CARET_BLINK_INTERVAL * 2f) >= CARET_BLINK_INTERVAL)
            {
                return;
            }

            int caretWidth = Math.Max(1, (int)(2f * inputLayout.TextScale));
            int caretX = Math.Min(textBounds.X + (int)textWidth, textBounds.Right - caretWidth);

            spriteBatch.Draw(Game1.staminaRect, new Rectangle(caretX, textBounds.Y, caretWidth, (int)inputLayout.LineHeight), element.TextColor * element.DrawAlpha);
        }

        public override Rectangle GetContentBounds(Element element, Rectangle bounds)
        {
            return bounds;
        }
    }
}
