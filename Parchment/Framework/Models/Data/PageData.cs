using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Pages;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Utilities.Helpers;
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

        /// <summary>Keywords describing what is on this page, matched by the Parchment_PageHasTag, Parchment_CurrentPageHasTag and Parchment_PageTagMatchesInput queries.
        /// Never shown to the reader, so they exist for a contents page or a search box to filter against rather than as anything the page displays. Matching ignores case.
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>Whether this page carries a tag, ignoring case.</summary>
        public bool HasTag(string? tag)
        {
            return string.IsNullOrWhiteSpace(tag) is false && Tags is not null && Tags.Any(pageTag => string.Equals(pageTag, tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Whether any of this page's tags contains the given text, ignoring case. Empty text matches a page with any tag at all, which is what leaves an untouched search box showing everything.</summary>
        public bool HasTagMatching(string? text)
        {
            if (Tags is null || Tags.Count is 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(text) is true)
            {
                return true;
            }

            return Tags.Any(pageTag => string.IsNullOrWhiteSpace(pageTag) is false && pageTag.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        public List<ElementData> Elements { get; set; } = new List<ElementData>();

        /// <summary>
        /// Elements drawn behind <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// An element here is only reachable by the cursor when it has something to offer, such as <see cref="ElementData.Description"/>, <see cref="ElementData.DisplayName"/> or <see cref="ElementData.Action"/> / <see cref="ElementData.Actions"/>,
        /// which keeps decorative art from covering the stacked elements above it.
        /// </summary>
        public List<ElementData>? Background { get; set; }

        /// <summary>
        /// Elements drawn over <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// An element here is only reachable by the cursor when it has something to offer, such as <see cref="ElementData.Description"/>, <see cref="ElementData.DisplayName"/> or <see cref="ElementData.Action"/> / <see cref="ElementData.Actions"/>,
        /// which keeps decorative art from covering the stacked elements below it.
        /// </summary>
        public List<ElementData>? Foreground { get; set; }

        /// <summary>
        /// Trigger actions run each time this page becomes visible, whether from the book opening, a page turn or a jump. Turning back to a page runs them again,
        /// so gate anything that should happen once behind a <see cref="PageTriggerData.Condition"/> such as PeacefulEnd.Parchment_HasSeenPageId.
        /// </summary>
        public List<PageTriggerData>? OnView { get; set; }

        /// <summary>
        /// Key bindings active while this page is on screen, each running its actions when pressed. Every matching entry runs, and a match can take the button over from the menu
        /// through <see cref="KeybindData.SuppressDefault"/>, which is what lets a page redirect the exit button somewhere other than out of the book.
        /// A page's binds take a button off <see cref="BookData.OnKeyPress"/> for as long as the page is on screen.
        /// </summary>
        public List<KeybindData>? OnKeyPress { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return (false, $"\"Id\" is required.");
            }

            if (Tags is not null && Tags.Any(string.IsNullOrWhiteSpace))
            {
                return (false, $"\"Tags\" contains an empty entry.");
            }

            var elementsIsValidData = ElementValidationHelper.ValidateElements(Elements);
            if (elementsIsValidData.Result is false)
            {
                return (false, $"[Elements] {elementsIsValidData.Error}");
            }

            if (Background is not null)
            {
                var backgroundIsValidData = ElementValidationHelper.ValidateElements(Background);
                if (backgroundIsValidData.Result is false)
                {
                    return (false, $"[Background] {backgroundIsValidData.Error}");
                }
            }

            if (Foreground is not null)
            {
                var foregroundIsValidData = ElementValidationHelper.ValidateElements(Foreground);
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

            if (OnKeyPress is not null)
            {
                for (int i = 0; i < OnKeyPress.Count; i++)
                {
                    var keybindIsValidData = OnKeyPress[i].IsValid();
                    if (keybindIsValidData.Result is false)
                    {
                        return (false, $"[OnKeyPress] Keybind at index {i}: {keybindIsValidData.Error}");
                    }
                }
            }

            return (true, string.Empty);
        }
    }
}
