using Microsoft.Xna.Framework;

namespace Parchment.Framework.Models
{
    /// <summary>Somewhere the cursor can be sent under snappy menus, being either an element the reader could hover or click, or one of the book's page turn corners.
    /// The bounds are what the menu navigates by and the element is only carried so a target can be found again after a pass that replaced it.
    /// </summary>
    /// <param name="Bounds">Where the target sits on screen, whose middle is where the cursor is put.</param>
    /// <param name="Element">The element the target stands for, or null for a page turn corner, which is a hotspot on the book rather than anything on a page.</param>
    public readonly record struct SnapTarget(Rectangle Bounds, Element? Element);
}
