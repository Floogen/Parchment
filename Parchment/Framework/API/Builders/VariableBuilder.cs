using Parchment.Framework.Models.Data.Variables;
using System.Collections.Generic;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Records how to build one of a book's variables. The recipe is kept rather than the built data, for the same reason the other builders keep theirs:
    /// every asset load gets a fresh object and Content Patcher's edits can't accumulate on the registered original.
    /// </summary>
    public class VariableBuilder : IVariableBuilder
    {
        private readonly string _variableId;
        private readonly List<(string Field, object? Value)> _fields = new List<(string Field, object? Value)>();
        private readonly List<string> _allowedValues = new List<string>();

        public string VariableId { get { return _variableId; } }

        internal VariableBuilder(string variableId)
        {
            _variableId = variableId ?? string.Empty;
        }

        public IVariableBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public IVariableBuilder Type(string variableType) { return Set("Type", variableType); }
        public IVariableBuilder Default(string defaultValue) { return Set("Default", defaultValue); }
        public IVariableBuilder Scope(string variableScope) { return Set("Scope", variableScope); }

        public IVariableBuilder AllowedValue(string value)
        {
            _allowedValues.Add(value);

            return this;
        }

        /// <summary>Creates a fresh variable from the recipe. Duplicate IDs and an out of range Default are left to <see cref="Models.Data.BookData.IsValid"/>, which reports them alongside everything else wrong with the book.</summary>
        internal bool TryBuild(out VariableData variable, out string error)
        {
            variable = null!;

            if (string.IsNullOrWhiteSpace(_variableId) is true)
            {
                error = "no variable ID was given";
                return false;
            }

            var data = new VariableData();

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = fieldError;
                    return false;
                }
            }

            // Forced after the fields, so the built variable always matches the ID it was added under
            data.Id = _variableId;

            // Copied rather than handed over, so a second build can't inherit what a later AllowedValue call added
            if (_allowedValues.Count > 0)
            {
                data.AllowedValues = new List<string>(_allowedValues);
            }

            variable = data;
            error = string.Empty;

            return true;
        }
    }
}
