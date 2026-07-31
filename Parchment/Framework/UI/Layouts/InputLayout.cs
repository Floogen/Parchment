using Microsoft.Xna.Framework;

namespace Parchment.Framework.UI.Layouts
{
    public class InputLayout
    {
        public int Inset { get; }
        public float TextScale { get; }
        public float LineHeight { get; }
        public Color PlaceholderColor { get; }

        public InputLayout(int inset, float textScale, float lineHeight, Color placeholderColor)
        {
            Inset = inset;
            TextScale = textScale;
            LineHeight = lineHeight;
            PlaceholderColor = placeholderColor;
        }
    }
}
