using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Interfaces
{
    public interface IFont
    {
        Vector2 MeasureString(string text, float scale);
        void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale);
    }
}
