using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Fonts
{
    public class FontResolver
    {
        private readonly Dictionary<FontType, IFont> _fontsByType = new Dictionary<FontType, IFont>();

        public IFont Resolve(FontType fontType)
        {
            if (_fontsByType.TryGetValue(fontType, out IFont font))
            {
                return font;
            }

            font = Create(fontType);
            _fontsByType[fontType] = font;

            return font;
        }

        private IFont Create(FontType fontType)
        {
            switch (fontType)
            {
                case FontType.SpriteText:
                    return new SpriteTextAdapter();
                case FontType.Small:
                    return new SpriteFontAdapter(Game1.smallFont);
                case FontType.Tiny:
                    return new SpriteFontAdapter(Game1.tinyFont);
                default:
                    return new SpriteFontAdapter(Game1.dialogueFont);
            }
        }
    }
}
