namespace Parchment.Framework.Models.Enums
{
    /// <summary>How an item property compares when a Grid's Source orders by it.</summary>
    public enum ItemPropertyKind
    {
        /// <summary>Compared as text, ignoring case.</summary>
        Text,

        /// <summary>Compared as a number, so 9 comes before 1000 rather than after it. A value that isn't a number is treated as missing and sorts last.</summary>
        Number
    }
}
