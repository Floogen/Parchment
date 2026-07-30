using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Pages;
using System.Collections.Generic;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Records how to build one page of a book.</summary>
    public class PageBuilder : IPageBuilder
    {
        private readonly string _pageId;
        private readonly string? _chapterId;
        private readonly List<(string Field, object? Value)> _fields = new List<(string Field, object? Value)>();
        private readonly List<ElementBuilder> _elements = new List<ElementBuilder>();
        private readonly List<ElementBuilder> _background = new List<ElementBuilder>();
        private readonly List<ElementBuilder> _foreground = new List<ElementBuilder>();
        private readonly List<(string Action, string? Condition)> _onView = new List<(string Action, string? Condition)>();

        public string PageId { get { return _pageId; } }

        internal PageBuilder(string pageId, string? chapterId)
        {
            _pageId = pageId ?? string.Empty;
            _chapterId = chapterId;
        }

        public IPageBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public IElementBuilder Add(string elementType)
        {
            var element = new ElementBuilder(elementType);
            _elements.Add(element);

            return element;
        }

        public IElementBuilder AddBackground(string elementType)
        {
            var element = new ElementBuilder(elementType);
            _background.Add(element);

            return element;
        }

        public IElementBuilder AddForeground(string elementType)
        {
            var element = new ElementBuilder(elementType);
            _foreground.Add(element);

            return element;
        }

        public IElementBuilder AddTitle(string text) { return Add("Title").Text(text); }
        public IElementBuilder AddHeading(string text) { return Add("Heading").Text(text); }
        public IElementBuilder AddParagraph(string text) { return Add("Paragraph").Text(text); }
        public IElementBuilder AddBanner(string text) { return Add("Banner").Text(text); }
        public IElementBuilder AddDivider() { return Add("Divider"); }
        public IElementBuilder AddPanel() { return Add("Panel"); }
        public IElementBuilder AddPageNumber() { return Add("PageNumber"); }
        public IElementBuilder AddImage(string texturePath) { return Add("Image").Texture(texturePath); }
        public IElementBuilder AddItemImage(string itemId) { return Add("Image").Item(itemId); }
        public IElementBuilder AddButton(string text, string action) { return Add("Button").Text(text).Action(action); }

        public IPageBuilder OnView(string action)
        {
            _onView.Add((action, null));

            return this;
        }

        public IPageBuilder OnView(string action, string condition)
        {
            _onView.Add((action, condition));

            return this;
        }

        /// <summary>Creates a fresh data object from the recorded fields.</summary>
        internal bool TryBuild(out PageData page, out string error)
        {
            page = null!;

            var data = new PageData() { Id = _pageId, ChapterId = _chapterId };

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = fieldError;
                    return false;
                }
            }

            if (TryBuildElements(_elements, out List<ElementData> elements, out error) is false)
            {
                return false;
            }
            data.Elements = elements;

            if (_background.Count > 0)
            {
                if (TryBuildElements(_background, out List<ElementData> background, out error) is false)
                {
                    return false;
                }

                data.Background = background;
            }

            if (_foreground.Count > 0)
            {
                if (TryBuildElements(_foreground, out List<ElementData> foreground, out error) is false)
                {
                    return false;
                }

                data.Foreground = foreground;
            }

            if (_onView.Count > 0)
            {
                var triggers = new List<PageTriggerData>();
                foreach (var trigger in _onView)
                {
                    triggers.Add(new PageTriggerData() { Condition = trigger.Condition, Actions = new List<string>() { trigger.Action } });
                }

                data.OnView = triggers;
            }

            page = data;
            error = string.Empty;

            return true;
        }

        private static bool TryBuildElements(List<ElementBuilder> builders, out List<ElementData> elements, out string error)
        {
            elements = new List<ElementData>();

            foreach (ElementBuilder builder in builders)
            {
                if (builder.TryBuild(out ElementData element, out error) is false)
                {
                    return false;
                }

                elements.Add(element);
            }

            error = string.Empty;

            return true;
        }
    }
}
