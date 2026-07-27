using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using Parchment.Framework.Utilities.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.UI.Rendering.Elements
{
    public class PageNumberElementRenderer : TextElementRenderer<PageNumberElementData>
    {
        /// <summary>The page's position, made 1-based for the reader. Both indexes on the context are -1 outside of a page, such as in a book's Underlay or Overlay, where there is no single page to number.</summary>
        protected override string GetText(PageNumberElementData data, ElementRenderContext context)
        {
            int pageIndex = data.Scope is PageNumberScope.Chapter ? context.ChapterPageIndex : context.PageIndex;

            if (pageIndex < 0)
            {
                Parchment.monitor.LogOnce($"A {ElementType.PageNumber} element is not on a page (such as in a book's Underlay or Overlay), so it will not render.", LogLevel.Warn);
                return string.Empty;
            }

            int pageNumber = pageIndex + 1;
            if (string.IsNullOrEmpty(data.Format))
            {
                return pageNumber.ToString();
            }

            // The format is validated at load, so this only catches data built in code rather than authored as JSON
            if (PageNumberElementData.TryApplyFormat(data.Format, pageNumber, out string formattedNumber) is false)
            {
                Parchment.monitor.LogOnce($"A {ElementType.PageNumber} element has an unusable \"Format\" of '{data.Format}': {formattedNumber}", LogLevel.Warn);
                return pageNumber.ToString();
            }

            return formattedNumber;
        }

        protected override void Draw(SpriteBatch spriteBatch, PageNumberElementData data, Element element, Rectangle bounds, ElementRenderContext context)
        {
            if (this.TryGetWrappedText(element, out WrappedText wrappedText) is false || element.Font is null)
            {
                return;
            }

            StringHelper.DrawLines(spriteBatch, element, wrappedText, bounds, data.Alignment, element.TextColor, data.Scale);
        }
    }
}
