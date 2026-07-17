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
    public class DividerElementRenderer : ElementRenderer<DividerElementData>
    {
        private const float LAYER_DEPTH = 0.87f;

        protected override Vector2 Measure(DividerElementData data, Element element, ElementRenderContext context)
        {
            if (SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle)
            {
                return Vector2.Zero;
            }

            float dividerHeight = sourceRectangle is Rectangle source ? source.Height * data.Scale : data.Thickness * data.Scale;
            float dividerWidth;

            switch (data.Sizing)
            {
                case SizingMode.Fixed:
                    dividerWidth = Math.Min(data.Width.Value * data.Scale, context.AvailableWidth);
                    break;
                case SizingMode.ShrinkToFit:
                    dividerWidth = sourceRectangle is Rectangle naturalSource ? Math.Min(naturalSource.Width * data.Scale, context.AvailableWidth) : context.AvailableWidth;
                    break;
                default:
                    dividerWidth = context.AvailableWidth;
                    break;
            }

            return new Vector2(dividerWidth, dividerHeight);
        }

        protected override void Draw(SpriteBatch spriteBatch, DividerElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (SpriteHelper.GetDrawSourceRectangle(data, element) is not Rectangle sourceRectangle)
            {
                Color lineColor = string.IsNullOrWhiteSpace(data.TintColor) ? Game1.textColor * 0.4f : element.TintColor;

                spriteBatch.Draw(Game1.staminaRect, bounds, null, lineColor, 0f, Vector2.Zero, SpriteEffects.None, LAYER_DEPTH);
                return;
            }

            spriteBatch.Draw(element.Texture, bounds, sourceRectangle, element.TintColor, 0f, Vector2.Zero, SpriteEffects.None, LAYER_DEPTH);
        }
    }
}
