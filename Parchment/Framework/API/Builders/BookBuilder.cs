using Parchment.Framework.Models;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Variables;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Records how to build a book. The recipe is kept rather than the built data, so every asset load gets a fresh object graph
    /// and Content Patcher's edits can't accumulate on the registered original.</summary>
    public class BookBuilder : IBookBuilder
    {
        private readonly string _modId;
        private readonly string _bookId;
        private readonly List<(string Field, object? Value)> _fields = new List<(string Field, object? Value)>();
        private readonly List<PageBuilder> _pages = new List<PageBuilder>();
        private readonly List<ElementBuilder> _underlay = new List<ElementBuilder>();
        private readonly List<ElementBuilder> _overlay = new List<ElementBuilder>();
        private readonly List<VariableBuilder> _variables = new List<VariableBuilder>();
        private readonly List<(string Keybind, string Action, string? Condition)> _onKeyPress = new List<(string Keybind, string Action, string? Condition)>();

        public string BookId { get { return _bookId; } }

        internal string ModId { get { return _modId; } }

        internal BookBuilder(string modId, string bookId)
        {
            _modId = modId;
            _bookId = bookId ?? string.Empty;
        }

        public IBookBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public IBookBuilder Sprite(string spritePath) { return Set("SpritePath", spritePath); }

        public IBookBuilder OnKeyPress(string keybind, string action)
        {
            _onKeyPress.Add((keybind, action, null));

            return this;
        }

        public IBookBuilder OnKeyPress(string keybind, string action, string condition)
        {
            _onKeyPress.Add((keybind, action, condition));

            return this;
        }

        public IPageBuilder AddPage(string pageId)
        {
            return CreatePage(pageId, null);
        }

        public IPageBuilder AddPage(string pageId, string chapterId)
        {
            return CreatePage(pageId, chapterId);
        }

        private IPageBuilder CreatePage(string pageId, string? chapterId)
        {
            var page = new PageBuilder(pageId, chapterId, this);
            _pages.Add(page);

            return page;
        }

        /// <summary>Builds just the book's own fields, with no pages, so a page can be measured against the appearance and layout it will be drawn with.</summary>
        /// <remarks>Field errors are ignored here, as they are reported properly by <see cref="TryBuildValidated"/> at registration. A field that fails to bind simply leaves its default in place.</remarks>
        internal BookData GetLayoutData()
        {
            var data = new BookData() { Id = _bookId };

            foreach (var field in _fields)
            {
                ModelBinder.TrySet(data, field.Field, field.Value, out _);
            }

            return data;
        }

        public IElementBuilder AddUnderlay(string elementType)
        {
            var element = new ElementBuilder(elementType);
            _underlay.Add(element);

            return element;
        }

        public IElementBuilder AddOverlay(string elementType)
        {
            var element = new ElementBuilder(elementType);
            _overlay.Add(element);

            return element;
        }

        public IVariableBuilder AddVariable(string variableId)
        {
            var variable = new VariableBuilder(variableId);
            _variables.Add(variable);

            return variable;
        }

        public bool TryRegister(out string error)
        {
            return Parchment.bookManager.TryRegisterBook(_modId, this, out error);
        }

        public bool TryOpen(out string error)
        {
            if (TryBuildValidated(out BookData bookData, out error) is false)
            {
                Parchment.monitor.Log($"{_modId} failed to open the book \"{_bookId}\", because {error}.", LogLevel.Warn);
                return false;
            }

            var book = new Book(bookData, Parchment.bookManager.ElementRegistry, Parchment.bookManager.FontResolver);
            Game1.activeClickableMenu = new BookMenu(book);

            return true;
        }

        /// <summary>Creates a fresh data object from the recipe, then runs the same validation content pack books go through.</summary>
        internal bool TryBuildValidated(out BookData book, out string error)
        {
            if (TryBuild(out book, out error) is false)
            {
                return false;
            }

            var isValidData = book.IsValid();
            if (isValidData.Result is false)
            {
                error = isValidData.Error;
                return false;
            }

            return true;
        }

        private bool TryBuild(out BookData book, out string error)
        {
            book = null!;

            if (string.IsNullOrWhiteSpace(_bookId) is true)
            {
                error = "no book ID was given";
                return false;
            }

            var data = new BookData();

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = fieldError;
                    return false;
                }
            }

            // Forced after the fields, so the built book always matches the ID it's registered under
            data.Id = _bookId;

            var pageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pages = new List<PageData>();

            foreach (PageBuilder pageBuilder in _pages)
            {
                if (pageBuilder.TryBuild(out PageData page, out error) is false)
                {
                    error = $"page \"{pageBuilder.PageId}\": {error}";
                    return false;
                }

                if (pageIds.Add(page.Id) is false)
                {
                    error = $"there's more than one page with the ID \"{page.Id}\"";
                    return false;
                }

                pages.Add(page);
            }

            data.Pages = pages;

            if (_underlay.Count > 0)
            {
                if (TryBuildElements(_underlay, out List<ElementData> underlay, out error) is false)
                {
                    error = $"underlay: {error}";
                    return false;
                }

                data.Underlay = underlay;
            }

            if (_overlay.Count > 0)
            {
                if (TryBuildElements(_overlay, out List<ElementData> overlay, out error) is false)
                {
                    error = $"overlay: {error}";
                    return false;
                }

                data.Overlay = overlay;
            }

            if (_variables.Count > 0)
            {
                var variables = new List<VariableData>();

                foreach (VariableBuilder variableBuilder in _variables)
                {
                    if (variableBuilder.TryBuild(out VariableData variable, out error) is false)
                    {
                        error = $"variable \"{variableBuilder.VariableId}\": {error}";
                        return false;
                    }

                    variables.Add(variable);
                }

                data.Variables = variables;
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

            book = data;
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
