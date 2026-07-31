using Microsoft.Xna.Framework;
using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Pages;
using Parchment.Framework.Utilities.Helpers;
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
        private readonly List<string> _tags = new List<string>();
        private readonly List<(string Action, string? Condition)> _onView = new List<(string Action, string? Condition)>();
        private readonly List<(string Keybind, string Action, string? Condition)> _onKeyPress = new List<(string Keybind, string Action, string? Condition)>();
        private readonly BookBuilder? _owner;

        public string PageId { get { return _pageId; } }

        internal PageBuilder(string pageId, string? chapterId, BookBuilder? owner = null)
        {
            _pageId = pageId ?? string.Empty;
            _chapterId = chapterId;
            _owner = owner;
        }

        public IPageBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public IPageBuilder Tag(string tag)
        {
            _tags.Add(tag);

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
        public IElementBuilder AddGrid(int cellWidth, int cellHeight, int columns, int? rows = null)
        {
            IElementBuilder grid = Add("Grid").CellWidth(cellWidth).CellHeight(cellHeight).Columns(columns);

            // Left unset rather than set to a default, so the grid stays as tall as its children need
            return rows is int rowCount ? grid.Rows(rowCount) : grid;
        }
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

        public IPageBuilder OnKeyPress(string keybind, string action)
        {
            _onKeyPress.Add((keybind, action, null));

            return this;
        }

        public IPageBuilder OnKeyPress(string keybind, string action, string condition)
        {
            _onKeyPress.Add((keybind, action, condition));

            return this;
        }

        public IPageBuilder RemoveLast()
        {
            if (_elements.Count > 0)
            {
                _elements.RemoveAt(_elements.Count - 1);
            }

            return this;
        }

        public float GetAvailableWidth()
        {
            return GetPageContentSize().X;
        }

        public float GetAvailableHeight()
        {
            return GetPageContentSize().Y;
        }

        public float GetContentHeight()
        {
            Point pageSize = GetPageContentSize();
            if (pageSize.X <= 0)
            {
                return 0f;
            }

            // Built fresh each call, since the point of measuring is to see the effect of whatever was just added
            if (TryBuild(out PageData pageData, out _) is false)
            {
                return 0f;
            }

            var page = new Page(pageData, 0, Parchment.bookManager.ElementRegistry, Parchment.bookManager.FontResolver);

            return Page.MeasureStack(page.Elements, pageSize.X);
        }

        public float GetRemainingHeight()
        {
            return GetAvailableHeight() - GetContentHeight();
        }

        public bool WouldOverflow()
        {
            float availableHeight = GetAvailableHeight();

            // A page whose size can't be worked out yet is never reported as overflowing, so callers degrade to a single page rather than to none
            return availableHeight > 0f && GetContentHeight() > availableHeight;
        }

        /// <summary>Get the page's content area, or an empty size when there is nothing to measure against yet.</summary>
        private Point GetPageContentSize()
        {
            if (_owner is null || Parchment.bookManager is null)
            {
                return Point.Zero;
            }

            return PageLayoutHelper.GetPageContentSize(_owner.GetLayoutData());
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

            if (_tags.Count > 0)
            {
                data.Tags = new List<string>(_tags);
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

            if (_onKeyPress.Count > 0)
            {
                var keybinds = new List<KeybindData>();
                foreach (var keybind in _onKeyPress)
                {
                    keybinds.Add(new KeybindData() { Keybind = keybind.Keybind, Condition = keybind.Condition, Actions = new List<string>() { keybind.Action } });
                }

                data.OnKeyPress = keybinds;
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
