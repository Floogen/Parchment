using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities;
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
            element.LayoutState = new ImageLayout(sourceRectangle, drawScale, drawSize);

            return new Vector2(context.AvailableWidth, drawSize.Y);
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

            //float drawX = bounds.X + (bounds.Width - imageLayout.DrawSize.X) / 2f;
            float drawX = GetAlignedX(bounds, imageLayout.DrawSize.X, element.Data.Alignment);
            spriteBatch.Draw(element.Texture, new Vector2(drawX, bounds.Y), imageLayout.SourceRectangle, Color.White, 0f, Vector2.Zero, imageLayout.DrawScale, SpriteEffects.None, LAYER_DEPTH);
        }
    }
}
