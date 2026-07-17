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
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class ButtonElementRenderer : ElementRenderer<ButtonElementData>
    {
        private const float LAYER_DEPTH = 0.87f;

        protected override Vector2 Measure(ButtonElementData data, Element element, ElementRenderContext context)
        {
            element.LayoutState = null;

            if (element.Texture is null || element.Texture.IsDisposed)
            {
                return Vector2.Zero;
            }

            if (SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle || sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0)
            {
                Parchment.monitor.LogOnce($"Button has an empty source rectangle for \"TexturePath\" at '{data.TexturePath}' and will not render!", LogLevel.Warn);
                return Vector2.Zero;
            }

            int borderThickness = NineSliceHelper.GetBorderThickness(sourceRectangle, data.Scale);
            int inset = borderThickness + (int)(data.Padding * data.Scale);

            float maximumTextWidth = Math.Max(0f, context.AvailableWidth - inset * 2f);
            WrappedText wrappedText = MeasureText(data, element, maximumTextWidth);

            float buttonWidth;

            switch (data.Sizing)
            {
                case SizingMode.Fixed:
                    buttonWidth = Math.Min(data.Width.Value * data.Scale + inset * 2f, context.AvailableWidth);
                    break;
                case SizingMode.Fill:
                    buttonWidth = context.AvailableWidth;
                    break;
                default:
                    buttonWidth = Math.Min(wrappedText.Size.X + inset * 2f, context.AvailableWidth);
                    break;
            }

            buttonWidth = Math.Max(buttonWidth, borderThickness * 2f);
            float buttonHeight = Math.Max(wrappedText.Size.Y + inset * 2f, borderThickness * 2f);

            element.LayoutState = new ButtonLayout(wrappedText, inset, data.TextScale);

            return new Vector2(buttonWidth, buttonHeight);
        }

        private static WrappedText MeasureText(ButtonElementData data, Element element, float maximumTextWidth)
        {
            if (string.IsNullOrEmpty(data.Text) || element.Font is null)
            {
                return TextWrapper.Wrap(string.Empty, element.Font!, maximumTextWidth, data.TextScale);
            }

            return TextWrapper.Wrap(data.Text, element.Font, maximumTextWidth, data.TextScale);
        }

        protected override void Draw(SpriteBatch spriteBatch, ButtonElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.LayoutState is not ButtonLayout buttonLayout)
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
            IClickableMenu.drawTextureBox(spriteBatch, element.Texture, sourceRectangle, bounds.X, bounds.Y, bounds.Width, bounds.Height, element.TintColor, data.Scale, drawShadow: false);

            StringHelper.DrawLines(spriteBatch, element, buttonLayout.WrappedText, GetTextBounds(buttonLayout, bounds), AlignmentType.Center, element.TextColor, buttonLayout.TextScale);
        }

        private static Rectangle GetTextBounds(ButtonLayout buttonLayout, Rectangle bounds)
        {
            return new Rectangle(bounds.X + buttonLayout.Inset, bounds.Y + (int)((bounds.Height - buttonLayout.WrappedText.Size.Y) / 2f), bounds.Width - buttonLayout.Inset * 2, (int)buttonLayout.WrappedText.Size.Y);
        }

        public override Rectangle GetContentBounds(Element element, Rectangle bounds)
        {
            return bounds;
        }
    }
}
