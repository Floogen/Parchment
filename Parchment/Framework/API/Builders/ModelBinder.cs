using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Assigns values onto the data models by property name, so the builders don't have to mirror every field the models expose
    /// and new fields work without an API change.</summary>
    internal static class ModelBinder
    {
        private const BindingFlags PROPERTY_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        /// <summary>Sets a property by name. The field may be a dotted path into a nested group, such as "Appearance.Scale".</summary>
        public static bool TrySet(object target, string field, object? value, out string error)
        {
            if (string.IsNullOrWhiteSpace(field) is true)
            {
                error = "no field name was given";
                return false;
            }

            string[] segments = field.Split('.');
            object current = target;

            for (int index = 0; index < segments.Length - 1; index++)
            {
                if (TryGetProperty(current, segments[index], out PropertyInfo? group, out error) is false)
                {
                    return false;
                }

                object? next = group!.GetValue(current);
                if (next is null)
                {
                    error = $"\"{segments[index]}\" isn't set on {DescribeType(current)}, so \"{field}\" can't be reached";
                    return false;
                }

                current = next;
            }

            string leaf = segments[segments.Length - 1];
            if (TryGetProperty(current, leaf, out PropertyInfo? property, out error) is false)
            {
                return false;
            }

            if (property!.CanWrite is false)
            {
                error = $"\"{leaf}\" on {DescribeType(current)} is read-only";
                return false;
            }

            if (TryCoerce(value, property.PropertyType, out object? coercedValue, out string coerceError) is false)
            {
                error = $"\"{field}\" {coerceError}";
                return false;
            }

            try
            {
                property.SetValue(current, coercedValue);
            }
            catch (Exception exception)
            {
                error = $"\"{field}\" couldn't be set ({exception.Message})";
                return false;
            }

            error = string.Empty;

            return true;
        }

        private static bool TryGetProperty(object target, string name, out PropertyInfo? property, out string error)
        {
            property = null;

            try
            {
                property = target.GetType().GetProperty(name, PROPERTY_FLAGS);
            }
            catch (AmbiguousMatchException)
            {
                // A property hidden by "new" in a subclass matches twice, so prefer the exact-case one on the most derived type
                property = target.GetType().GetProperties(PROPERTY_FLAGS).FirstOrDefault(candidate => candidate.Name.Equals(name, StringComparison.Ordinal));
            }

            if (property is null)
            {
                error = $"there's no field named \"{name}\" on {DescribeType(target)}. It accepts: {string.Join(", ", GetSettableNames(target))}";
                return false;
            }

            error = string.Empty;

            return true;
        }

        private static bool TryCoerce(object? value, Type targetType, out object? coercedValue, out string error)
        {
            coercedValue = null;
            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is null)
            {
                if (targetType.IsValueType is true && Nullable.GetUnderlyingType(targetType) is null)
                {
                    error = $"can't be null, it expects {DescribeTypeName(underlyingType)}";
                    return false;
                }

                error = string.Empty;

                return true;
            }

            if (underlyingType.IsInstanceOfType(value) is true)
            {
                coercedValue = value;
                error = string.Empty;

                return true;
            }

            if (underlyingType.IsEnum is true)
            {
                if (value is string text && Enum.TryParse(underlyingType, text, true, out object? parsedValue) is true && parsedValue is not null && IsValidEnumValue(underlyingType, parsedValue) is true)
                {
                    coercedValue = parsedValue;
                    error = string.Empty;

                    return true;
                }

                error = $"expects one of {string.Join(", ", Enum.GetNames(underlyingType))} but got \"{value}\"";
                return false;
            }

            try
            {
                coercedValue = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                error = string.Empty;

                return true;
            }
            catch (Exception)
            {
                error = $"expects {DescribeTypeName(underlyingType)} but got {DescribeTypeName(value.GetType())}";
                return false;
            }
        }

        // A numeric string parses into an enum even when no such value exists, so anything undefined is rejected. Flag enums are exempt,
        // since a combination such as "FlipHorizontally, FlipVertically" is valid without being a defined value on its own.
        private static bool IsValidEnumValue(Type enumType, object value)
        {
            if (enumType.IsDefined(typeof(FlagsAttribute), false) is true)
            {
                return true;
            }

            return Enum.IsDefined(enumType, value);
        }

        private static IEnumerable<string> GetSettableNames(object target)
        {
            return target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanWrite is true).Select(property => property.Name).OrderBy(name => name);
        }

        private static string DescribeType(object target)
        {
            return DescribeTypeName(target.GetType());
        }

        private static string DescribeTypeName(Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType.Name;
        }
    }
}
