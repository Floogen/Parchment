using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class SpriteHelper
    {
        /// <summary>The source rectangle used for layout. Not hover aware as <see cref="IElementRenderer.Measure"/> is cached and would freeze whichever state was active during the last layout.</summary>
        public static Rectangle? GetSourceRectangle(ElementData data, Element element)
        {
            if (data is not ISprite sprite || element.Texture is null || element.Texture.IsDisposed)
            {
                return null;
            }

            return element.SourceRectangle ?? sprite.TextureSourceRectangle ?? element.Texture.Bounds;
        }

        /// <summary>The source rectangle used for drawing, which swaps to the hover art when the element is hovered and then moves to the animation's current frame.
        /// A frame only moves where in the sheet the art is read from, never how much of it, so every element still draws at the size it was measured at and nothing has to be laid out again.
        /// The two hover mechanisms stack rather than compete: the swap picks the rectangle and the frame, itself already the hover animation's when the cursor is on the element, then moves it.
        /// </summary>
        public static Rectangle? GetDrawSourceRectangle(ElementData data, Element element)
        {
            if (data is not ISprite sprite || element.Texture is null || element.Texture.IsDisposed)
            {
                return null;
            }

            Rectangle sourceRectangle = element.IsHovered && sprite.HoverTextureSourceRectangle is Rectangle hoverSourceRectangle ? hoverSourceRectangle : element.SourceRectangle ?? sprite.TextureSourceRectangle ?? element.Texture.Bounds;

            return AnimationHelper.GetFrameRectangle(sourceRectangle, AnimationHelper.GetActiveFrame(element, data.FrameDuration));
        }
    }
}
