using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PageNumberElementData : ElementData, ITextContent
    {
        public override ElementType Type => ElementType.PageNumber;

        public string? TextColor { get; set; }
        public FontType FontType { get; set; } = FontType.Small;

        /// <summary>What the number counts from. <see cref="PageNumberScope.Chapter"/> restarts at 1 on each chapter's first page, rather than running through the book.
        /// Has no effect in a book without chapters, where the whole book is a single chapter.
        /// </summary>
        public PageNumberScope Scope { get; set; } = PageNumberScope.Book;

        /// <summary>An optional composite format string wrapping the number, where {0} is the number itself, such as "Page {0}" or "- {0} -". When null, the number is drawn on its own.</summary>
        public string? Format { get; set; }

        /// <summary>Not authored. The number comes from the page's position in the book, so this is implemented explicitly to keep "Text" out of the JSON schema and to ignore it if given.</summary>
        string? ITextContent.Text { get => null; set { } }

        public override (bool Result, string Error) IsValid()
        {
            if (Format is not null && TryApplyFormat(Format, 1, out string error) is false)
            {
                return (false, $"\"Format\" is not a valid format string: {error}");
            }

            return base.IsValid();
        }

        /// <summary>Applies <see cref="Format"/> to a page number. Kept here so the same call validates the format at load and produces the text at draw.</summary>
        internal static bool TryApplyFormat(string format, int pageNumber, out string result)
        {
            try
            {
                result = string.Format(format, pageNumber);

                return true;
            }
            catch (FormatException exception)
            {
                result = exception.Message;

                return false;
            }
        }
    }
}
