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

        /// <summary>The source rectangle used for drawing, which swaps to the hover art when the element is hovered.</summary>
        public static Rectangle? GetDrawSourceRectangle(ElementData data, Element element)
        {
            if (data is not ISprite sprite || element.Texture is null || element.Texture.IsDisposed)
            {
                return null;
            }

            if (element.IsHovered && sprite.HoverTextureSourceRectangle is Rectangle hoverSourceRectangle)
            {
                return hoverSourceRectangle;
            }

            return element.SourceRectangle ?? sprite.TextureSourceRectangle ?? element.Texture.Bounds;
        }
    }
}
