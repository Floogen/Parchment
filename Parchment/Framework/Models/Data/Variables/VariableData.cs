using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Parchment.Framework.Models.Data.Variables
{
    /// <summary>One named value a book can set and read back, declared so it has a default, a type and a lifetime before anything touches it.
    /// Declaring is required: an action or query naming a variable the book doesn't declare fails with a message rather than persisting a typo into a save.
    /// </summary>
    public class VariableData : BaseModel
    {
        /// <summary>The name actions and queries address this variable by, unique within the book.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>What the variable holds, which decides what values it accepts and how HasVariable compares it.</summary>
        public VariableType Type { get; set; } = VariableType.Boolean;

        /// <summary>The value before anything sets it, and what ClearVariable returns it to. When omitted this is false, 0 or empty text, whichever suits the type.</summary>
        public string? Default { get; set; }

        /// <summary>Optional. The only values SetVariable accepts, compared ignoring case. Leave it out to accept anything the type allows.</summary>
        public List<string>? AllowedValues { get; set; }

        /// <summary>Optional. The lowest value a Number variable can hold, inclusive. SetVariable turns away anything below it and IncrementVariable stops here rather than failing.</summary>
        public double? Min { get; set; }

        /// <summary>Optional. The highest value a Number variable can hold, inclusive. SetVariable turns away anything above it and IncrementVariable stops here rather than failing.</summary>
        public double? Max { get; set; }

        /// <summary>Whether the value belongs to the save file or to the whole installation.</summary>
        public VariableScope Scope { get; set; } = VariableScope.Save;

        /// <summary>The starting value, being the authored <see cref="Default"/> or the type's own when none was given.</summary>
        public string GetDefault()
        {
            if (Default is not null)
            {
                return Default;
            }

            return Type switch { VariableType.Boolean => "false", VariableType.Number => "0", _ => string.Empty };
        }

        /// <summary>Brings a number inside the declared bounds. This is what makes a stepper stop at the end of its range rather than fail on every further press.</summary>
        public double Clamp(double value)
        {
            if (Min is double minimum && value < minimum)
            {
                return minimum;
            }

            if (Max is double maximum && value > maximum)
            {
                return maximum;
            }

            return value;
        }

        /// <summary>Whether a value is one this variable can hold, checking it against the type first, then against the declared bounds and AllowedValues.</summary>
        public bool TryValidateValue(string value, out string error)
        {
            if (Type is VariableType.Boolean && bool.TryParse(value, out bool _) is false)
            {
                error = $"'{value}' is not true or false";
                return false;
            }

            if (Type is VariableType.Number)
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double number) is false)
                {
                    error = $"'{value}' is not a number";
                    return false;
                }

                if (Min is double minimum && number < minimum)
                {
                    error = $"'{value}' is below the minimum of {minimum.ToString(CultureInfo.InvariantCulture)}";
                    return false;
                }

                if (Max is double maximum && number > maximum)
                {
                    error = $"'{value}' is above the maximum of {maximum.ToString(CultureInfo.InvariantCulture)}";
                    return false;
                }
            }

            if (AllowedValues is not null && AllowedValues.Any(allowed => string.Equals(allowed, value, StringComparison.OrdinalIgnoreCase)) is false)
            {
                error = $"'{value}' is not one of the allowed values: {string.Join(", ", AllowedValues)}";
                return false;
            }

            error = string.Empty;

            return true;
        }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return (false, $"\"Id\" is required.");
            }

            if (AllowedValues is not null && AllowedValues.Count is 0)
            {
                return (false, $"\"AllowedValues\" cannot be empty when it is given.");
            }

            if (Type is not VariableType.Number && (Min is not null || Max is not null))
            {
                return (false, $"\"Min\" and \"Max\" only apply to a Number variable, and this one is a {Type}.");
            }

            if (Min is double declaredMinimum && Max is double declaredMaximum && declaredMinimum > declaredMaximum)
            {
                return (false, $"\"Min\" of {declaredMinimum.ToString(CultureInfo.InvariantCulture)} is above \"Max\" of {declaredMaximum.ToString(CultureInfo.InvariantCulture)}, so no value would be valid.");
            }

            // Checked through GetDefault so an omitted Default is caught too, which is what happens when AllowedValues rules out the type's own starting value
            if (TryValidateValue(GetDefault(), out string valueError) is false)
            {
                return (false, $"\"Default\" of {valueError}.");
            }

            return (true, string.Empty);
        }
    }
}
