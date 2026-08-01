namespace Parchment.Framework.API
{
    /// <summary>Builds one variable a book declares. Obtained from <see cref="IBookBuilder.AddVariable"/>.</summary>
    public interface IVariableBuilder
    {
        /// <summary>The variable's ID.</summary>
        string VariableId { get; }

        /// <summary>Sets any field on the variable by name, for anything the methods below don't cover. Fields that don't exist are reported when the book is registered, along with the ones that do.</summary>
        IVariableBuilder Set(string field, object? value);

        /// <summary>What the variable holds: "Boolean", "Number" or "Text". Defaults to "Boolean", and decides which values Default and AllowedValue accept.</summary>
        IVariableBuilder Type(string variableType);

        /// <summary>The value before anything sets it, and what ClearVariable returns it to. Left out, it's false, 0 or empty text, whichever suits the type.</summary>
        IVariableBuilder Default(string defaultValue);

        /// <summary>How long the value lasts: "Save" to keep it on the player and in the save file, or "Global" to share it across every save. Defaults to "Save".</summary>
        IVariableBuilder Scope(string variableScope);

        /// <summary>The lowest value a Number variable can hold, inclusive. SetVariable turns away anything below it, and IncrementVariable stops here rather than failing.</summary>
        IVariableBuilder Min(double min);

        /// <summary>The highest value a Number variable can hold, inclusive. SetVariable turns away anything above it, and IncrementVariable stops here rather than failing.</summary>
        IVariableBuilder Max(double max);

        /// <summary>The lowest and highest values together, for the usual case of declaring a range in one go.</summary>
        IVariableBuilder Range(double min, double max);

        /// <summary>Adds a value SetVariable will accept, compared ignoring case. Calling this more than once builds the list, and leaving it alone accepts anything the type allows.
        /// Pair it with Default, as the type's own starting value is rarely one of the allowed ones and registration fails when it isn't.
        /// </summary>
        IVariableBuilder AllowedValue(string value);
    }
}
