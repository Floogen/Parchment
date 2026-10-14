namespace Parchment.Framework.Models.Enums
{
    public enum OverflowMode
    {
        /// <summary>Text past the bottom can be scrolled into view with the mouse wheel, the right stick or the scroll bar drawn beside it.</summary>
        Scroll,
        /// <summary>Only the lines that fit are drawn and the rest are left out, with no way to reach them.</summary>
        Truncate
    }
}
