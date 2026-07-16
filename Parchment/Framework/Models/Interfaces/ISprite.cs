using Microsoft.Xna.Framework;
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
    }
}
