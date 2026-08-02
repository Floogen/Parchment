using Parchment.Framework.Models.Data.Elements;
using System.Collections.Generic;

namespace Parchment.Framework.Utilities.Helpers
{
    public static class ElementValidationHelper
    {
        /// <summary>Validates every element in a list, reporting the first failure. A container element validates its own nested lists inside its
        /// <see cref="ElementData.IsValid"/>, so calling this on a page's top-level list walks the whole tree.
        /// </summary>
        public static (bool Result, string Error) ValidateElements(List<ElementData>? elements)
        {
            if (elements is null)
            {
                return (true, string.Empty);
            }

            foreach (ElementData element in elements)
            {
                var isValidData = element.IsValid();
                if (isValidData.Result is false)
                {
                    return (false, $"Element \"{element.Id}\" ({element.Type}): {isValidData.Error}");
                }
            }

            return (true, string.Empty);
        }
    }
}
