using Parchment.Framework.Models.Data.Books;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class BookData : BaseModel
    {
        /// <summary>The schema version this book was authored against.</summary>
        public string Format { get; set; } = "1.0.0";


        public string Id { get; set; } = string.Empty;

        public BookAppearanceData Appearance { get; set; } = new BookAppearanceData();
        public PageCurlData PageCurl { get; set; } = new PageCurlData();
        public BookAnimationData Animation { get; set; } = new BookAnimationData();

        /// <summary>
        /// Sprite path for book item.
        /// </summary>
        public string? SpritePath { get; set; }

        /// <summary>The book's pages, in reading order. Pages are shown in the order they appear here,
        /// and pages sharing a <see cref="PageData.ChapterId"/> must be kept contiguous or they will be treated as separate chapters.</summary>
        public List<PageData> Pages { get; set; } = new List<PageData>();

        /// <summary>Elements drawn behind the book sprite, positioned via <see cref="ElementData.Position"/> relative to the book's top-left. Negative coordinates place content outside the book's edges.</summary>
        public List<ElementData>? Underlay { get; set; }

        /// <summary>Elements drawn in front of the book sprite and its pages, positioned via <see cref="ElementData.Position"/> relative to the book's top-left.
        /// Drawn in every menu state, so they ride in with the book and remain on the shut cover.</summary>
        public List<ElementData>? Overlay { get; set; }

        /// <summary>
        /// Key bindings active on every page of this book, each running its actions when pressed. A page's own <see cref="PageData.OnKeyPress"/> takes precedence:
        /// when a page binds the same button, only the page's entries run and these are left alone.
        /// </summary>
        public List<KeybindData>? OnKeyPress { get; set; }

        public BookLayoutData Layout { get; set; } = new BookLayoutData();

        /// <summary>The named values this book can set and read back, which unlike a session flag survive the book being put down.
        /// Every variable an action or query names has to be declared here, so a mistyped name fails visibly rather than being stored.
        /// </summary>
        public List<VariableData>? Variables { get; set; }

        /// <summary>Whether the book arrives shut, holding on its cover until the reader clicks it open, rather than opening itself once it
        /// has slid into place. Independent of <see cref="ExitToCover"/>, which governs the other end of the reading.</summary>
        public bool StartOnCover { get; set; } = false;

        /// <summary>Whether asking to close the book shuts it in place first, leaving its cover on screen, rather than leaving the menu.
        /// A second close request then leaves, and clicking the cover reopens to the page the reader left off on.
        /// This governs only the reader's own close request. The cover stays reachable from the ViewCover action either way.
        /// Decorate the cover by giving <see cref="Overlay"/> or <see cref="Underlay"/> elements a condition on the Cover book state.
        /// </summary>
        public bool ExitToCover { get; set; } = false;

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                return (false, $"\"Id\" is required.");
            }

            if (string.IsNullOrWhiteSpace(Format))
            {
                return (false, $"\"Format\" is required.");
            }

            if (Pages.Count == 0)
            {
                return (false, $"\"Pages\" must contain at least one page.");
            }

            foreach (PageData page in Pages)
            {
                var isValidData = page.IsValid();
                if (isValidData.Result is false)
                {
                    return (false, $"Page \"{page.Id}\": {isValidData.Error}");
                }
            }

            if (Overlay is not null)
            {
                foreach (var element in Overlay)
                {
                    var isValidData = element.IsValid();
                    if (isValidData.Result is false)
                    {
                        return (false, $"Element \"{element.Id}\" ({element.Type}): {isValidData.Error}");
                    }
                }
            }

            if (Underlay is not null)
            {
                foreach (var element in Underlay)
                {
                    var isValidData = element.IsValid();
                    if (isValidData.Result is false)
                    {
                        return (false, $"Element \"{element.Id}\" ({element.Type}): {isValidData.Error}");
                    }
                }
            }

            if (Variables is not null)
            {
                foreach (VariableData variable in Variables)
                {
                    var variableIsValidData = variable.IsValid();
                    if (variableIsValidData.Result is false)
                    {
                        return (false, $"[Variables] Variable \"{variable.Id}\": {variableIsValidData.Error}");
                    }

                    if (Variables.Count(other => other.Id.Equals(variable.Id, StringComparison.OrdinalIgnoreCase)) > 1)
                    {
                        return (false, $"[Variables] More than one variable is named \"{variable.Id}\".");
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
