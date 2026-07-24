using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Pages;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PageData : BaseModel
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>The chapter this page belongs to (pages sharing the same value belong in same chapter).
        /// Chapters are navigation-isolated and page turning never crosses a chapter boundary unless via button.
        /// </summary>
        public string? ChapterId { get; set; }

        public List<ElementData> Elements { get; set; } = new List<ElementData>();

        /// <summary>
        /// Elements drawn behind <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// </summary>
        public List<ElementData>? Background { get; set; }

        /// <summary>
        /// Elements drawn over <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// </summary>
        public List<ElementData>? Foreground { get; set; }

        /// <summary>
        /// Trigger actions run each time this page becomes visible, whether from the book opening, a page turn or a jump. Turning back to a page runs them again,
        /// so gate anything that should happen once behind a <see cref="PageTriggerData.Condition"/> such as PeacefulEnd.Parchment_HasSeenPageId.
        /// </summary>
        public List<PageTriggerData>? OnView { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return (false, $"\"Id\" is required.");
            }

            var elementsIsValidData = ValidateElements(Elements);
            if (elementsIsValidData.Result is false)
            {
                return (false, $"[Elements] {elementsIsValidData.Error}");
            }

            if (Background is not null)
            {
                var backgroundIsValidData = ValidateElements(Background);
                if (backgroundIsValidData.Result is false)
                {
                    return (false, $"[Background] {backgroundIsValidData.Error}");
                }
            }

            if (Foreground is not null)
            {
                var foregroundIsValidData = ValidateElements(Foreground);
                if (foregroundIsValidData.Result is false)
                {
                    return (false, $"[Foreground] {foregroundIsValidData.Error}");
                }
            }

            if (OnView is not null)
            {
                for (int i = 0; i < OnView.Count; i++)
                {
                    var triggerIsValidData = OnView[i].IsValid();
                    if (triggerIsValidData.Result is false)
                    {
                        return (false, $"[OnView] Trigger at index {i}: {triggerIsValidData.Error}");
                    }
                }
            }

            return (true, string.Empty);
        }

        private (bool Result, string Error) ValidateElements(List<ElementData> elements)
        {
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
