namespace Parchment.Framework.Models.Enums
{
    /// <summary>What a variable holds, which decides what values it accepts and how a query compares it.</summary>
    public enum VariableType
    {
        /// <summary>True or false. The only type ToggleVariable accepts.</summary>
        Boolean,
        /// <summary>A number, compared as one, so 9 and 9.0 are the same value.</summary>
        Number,
        /// <summary>Any text, compared ignoring case.</summary>
        Text
    }
}
