using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Interfaces
{
    public interface ISprite
    {
        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }

        /// <summary>How to mirror the sprite when drawing. Ignored by nine-sliced elements, which have no meaningful flip.
        /// Must match SpriteEffects exactly for JSON (such as "FlipHorizontally, FlipVertically" for both).</summary>
        public SpriteEffects SpriteEffects { get; set; }
    }
}
