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
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class PanelElementRenderer : ElementRenderer<PanelElementData>
    {
        public override Rectangle GetContentBounds(Element element, Rectangle bounds)
        {
            if (element.LayoutState is not PanelLayout panelLayout)
            {
                return bounds;
            }

            return new Rectangle(bounds.X + panelLayout.Inset, bounds.Y + panelLayout.Inset, bounds.Width - panelLayout.Inset * 2, bounds.Height - panelLayout.Inset * 2);
        }

        protected override Vector2 Measure(PanelElementData data, Element element, ElementRenderContext context)
        {
            int borderThickness = GetBorderThickness(data, element);
            int inset = borderThickness + (int)(data.Padding * data.Scale);

            float availableChildHeight = Math.Max(0f, context.AvailableHeight - inset * 2f);
            float childHeight = data.Height is int fixedHeight ? Math.Min(fixedHeight * data.Scale, availableChildHeight) : availableChildHeight;

            float panelWidth;
            switch (data.Sizing)
            {
                case SizingMode.Fixed:
                    panelWidth = Math.Min(data.Width.Value * data.Scale + inset * 2f, context.AvailableWidth);
                    break;
                case SizingMode.ShrinkToFit:
                    float maximumChildWidth = Math.Max(0f, context.AvailableWidth - inset * 2f);
                    float naturalChildWidth = GetNaturalChildWidth(element.Children, context.WithSize(maximumChildWidth, childHeight));
                    panelWidth = Math.Min(naturalChildWidth + inset * 2f, context.AvailableWidth);
                    break;
                default:
                    panelWidth = context.AvailableWidth;
                    break;
            }

            float childWidth = Math.Max(0f, panelWidth - inset * 2f);
            ElementRenderContext childContext = context.WithSize(childWidth, childHeight);
            element.LayoutState = new PanelLayout(childContext, inset);

            float contentHeight = Page.StackElements(element.Children, childContext);

            if (data.Height is not null)
            {
                contentHeight = childHeight;
            }

            return new Vector2(Math.Max(panelWidth, borderThickness * 2f), Math.Max(contentHeight + inset * 2f, borderThickness * 2f));
        }

        private static int GetBorderThickness(PanelElementData data, Element element)
        {
            if (element.Texture is null || element.Texture.IsDisposed || SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle)
            {
                return 0;
            }

            return NineSliceHelper.GetBorderThickness(sourceRectangle, data.Scale);
        }

        private static float GetNaturalChildWidth(IReadOnlyList<Element> children, ElementRenderContext context)
        {
            float naturalChildWidth = 0f;
            foreach (Element child in children)
            {
                Vector2 childSize = child.Renderer.Measure(child, context);
                naturalChildWidth = Math.Max(naturalChildWidth, childSize.X);
            }

            return naturalChildWidth;
        }

        protected override void Draw(SpriteBatch spriteBatch, PanelElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (element.LayoutState is not PanelLayout panelLayout)
            {
                return;
            }

            if (element.Texture is not null && element.Texture.IsDisposed is false)
            {
                IClickableMenu.drawTextureBox(spriteBatch, element.Texture, data.TextureSourceRectangle ?? element.Texture.Bounds, bounds.X, bounds.Y, bounds.Width, bounds.Height, Color.White, data.Scale, false);
            }

            Rectangle contentBounds = GetContentBounds(element, bounds);

            foreach (Element child in element.Children)
            {
                Rectangle childBounds = new Rectangle(contentBounds.X + child.Bounds.X, contentBounds.Y + child.Bounds.Y, child.Bounds.Width, child.Bounds.Height);
                child.Renderer.Draw(spriteBatch, child, childBounds, panelLayout.ChildContext);
            }
        }
    }
}
