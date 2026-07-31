using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Data.Results;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Rendering;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Records how to build one element. The recorded fields are replayed onto a fresh data object each time the book is built,
    /// so a registered book can be rebuilt without the previous build's edits carrying over.</summary>
    public class ElementBuilder : IElementBuilder
    {
        private readonly string _elementType;
        private readonly List<(string Field, object? Value)> _fields = new List<(string Field, object? Value)>();
        private readonly List<ElementBuilder> _children = new List<ElementBuilder>();
        private readonly List<ElementBuilder> _background = new List<ElementBuilder>();
        private readonly List<ElementBuilder> _foreground = new List<ElementBuilder>();
        private readonly List<FrameRecipe> _frames = new List<FrameRecipe>();
        private readonly List<FrameRecipe> _hoverFrames = new List<FrameRecipe>();

        // The frame FrameOffset applies to, being whichever was added last from either list
        private FrameRecipe? _lastFrame;
        private bool _hasOrphanFrameModifier = false;
        private readonly List<string> _actions = new List<string>();
        private readonly List<string> _hoverActions = new List<string>();
        private readonly List<string> _submitActions = new List<string>();
        private readonly List<string> _textChangedActions = new List<string>();

        // A grid's results, kept as parts rather than as a built block so the recipe survives an asset reload the way every other field does
        private string? _resultSource;
        private string? _resultInputId;
        private string? _resultCondition;
        private string? _resultOrder;
        private int? _resultCount;
        private ElementBuilder? _resultTemplate;

        public string ElementType { get { return _elementType; } }

        internal ElementBuilder(string elementType)
        {
            _elementType = elementType ?? string.Empty;
        }

        public IElementBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public IElementBuilder WithId(string id) { return Set("Id", id); }
        public IElementBuilder Text(string text) { return Set("Text", text); }
        public IElementBuilder Alignment(string alignment) { return Set("Alignment", alignment); }
        public IElementBuilder VerticalAlignment(string alignment) { return Set("VerticalAlignment", alignment); }
        public IElementBuilder TextAlignment(string alignment) { return Set("TextAlignment", alignment); }
        public IElementBuilder Font(string fontType) { return Set("FontType", fontType); }
        public IElementBuilder TextColor(string color) { return Set("TextColor", color); }
        public IElementBuilder TextScale(float scale) { return Set("TextScale", scale); }
        public IElementBuilder Scale(float scale) { return Set("Scale", scale); }
        public IElementBuilder Rotation(float rotation) { return Set("Rotation", rotation); }
        public IElementBuilder Origin(float x, float y) { return Set("Origin", new Vector2(x, y)); }
        public IElementBuilder Position(int x, int y) { return Set("Position", new Point(x, y)); }
        public IElementBuilder Texture(string texturePath) { return Set("TexturePath", texturePath); }
        public IElementBuilder TextureSource(int x, int y, int width, int height) { return Set("TextureSourceRectangle", new Rectangle(x, y, width, height)); }
        public IElementBuilder HoverTextureSource(int x, int y, int width, int height) { return Set("HoverTextureSourceRectangle", new Rectangle(x, y, width, height)); }
        public IElementBuilder Tint(string tintColor) { return Set("TintColor", tintColor); }
        public IElementBuilder Item(string itemId) { return Set("ItemId", itemId); }
        public IElementBuilder Sound(string sound) { return Set("Sound", sound); }

        public IElementBuilder Action(string action)
        {
            _actions.Add(action);

            return this;
        }

        public IElementBuilder Action(string action, string sound)
        {
            _actions.Add(action);

            return Set("Sound", sound);
        }

        public IElementBuilder HoverAction(string action)
        {
            _hoverActions.Add(action);

            return this;
        }

        public IElementBuilder SubmitAction(string action)
        {
            _submitActions.Add(action);

            return this;
        }

        public IElementBuilder TextChangedAction(string action)
        {
            _textChangedActions.Add(action);

            return this;
        }

        public IElementBuilder TextChangedDelay(float textChangedDelay) { return Set("TextChangedDelay", textChangedDelay); }

        public IElementBuilder InputId(string inputId) { return Set("InputId", inputId); }
        public IElementBuilder Placeholder(string placeholder) { return Set("Placeholder", placeholder); }
        public IElementBuilder MaxLength(int maxLength) { return Set("MaxLength", maxLength); }
        public IElementBuilder Condition(string condition) { return Set("Condition", condition); }
        public IElementBuilder Sizing(string sizingMode) { return Set("Sizing", sizingMode); }
        public IElementBuilder Scope(string scope) { return Set("Scope", scope); }
        public IElementBuilder Format(string format) { return Set("Format", format); }
        public IElementBuilder Width(int width) { return Set("Width", width); }
        public IElementBuilder Height(int height) { return Set("Height", height); }
        public IElementBuilder Padding(int padding) { return Set("Padding", padding); }
        public IElementBuilder Columns(int columns) { return Set("Columns", columns); }
        public IElementBuilder Rows(int rows) { return Set("Rows", rows); }
        public IElementBuilder CellWidth(int cellWidth) { return Set("CellWidth", cellWidth); }
        public IElementBuilder CellHeight(int cellHeight) { return Set("CellHeight", cellHeight); }
        public IElementBuilder CellSpacing(int columnSpacing, int rowSpacing) { return Set("ColumnSpacing", columnSpacing).Set("RowSpacing", rowSpacing); }

        public IElementBuilder Results(string source)
        {
            _resultSource = source;

            return this;
        }

        public IElementBuilder ResultFilter(string inputId)
        {
            _resultInputId = inputId;

            return this;
        }

        public IElementBuilder ResultCondition(string perItemCondition)
        {
            _resultCondition = perItemCondition;

            return this;
        }

        public IElementBuilder ResultOrder(string order)
        {
            _resultOrder = order;

            return this;
        }

        public IElementBuilder ResultCount(int count)
        {
            _resultCount = count;

            return this;
        }

        public IElementBuilder AddResultTemplate(string elementType)
        {
            _resultTemplate = new ElementBuilder(elementType);

            return _resultTemplate;
        }
        public IElementBuilder Spacing(int spacingAfter) { return Set("SpacingAfter", spacingAfter); }
        public IElementBuilder Margin(int left, int right) { return Set("MarginLeft", left).Set("MarginRight", right); }
        public IElementBuilder Tooltip(string displayName, string description) { return Set("DisplayName", displayName).Set("Description", description); }

        public IElementBuilder AddFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null)
        {
            return RecordFrame(_frames, new Point(x, y), duration, scale, condition);
        }

        public IElementBuilder AddFrameInPlace(float duration = 0f, float scale = 1f, string? condition = null)
        {
            return RecordFrame(_frames, null, duration, scale, condition);
        }

        public IElementBuilder AddHoverFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null)
        {
            return RecordFrame(_hoverFrames, new Point(x, y), duration, scale, condition);
        }

        public IElementBuilder AddHoverFrameInPlace(float duration = 0f, float scale = 1f, string? condition = null)
        {
            return RecordFrame(_hoverFrames, null, duration, scale, condition);
        }

        public IElementBuilder FrameOffset(int x, int y)
        {
            // Nothing to hang the offset on. Recorded rather than thrown, so it surfaces as a registration error alongside every other authoring mistake
            if (_lastFrame is null)
            {
                _hasOrphanFrameModifier = true;

                return this;
            }

            _lastFrame.Offset = x is 0 && y is 0 ? null : new Point(x, y);

            return this;
        }

        public IElementBuilder FrameAction(string action)
        {
            if (_lastFrame is null)
            {
                _hasOrphanFrameModifier = true;

                return this;
            }

            _lastFrame.Actions.Add(action);

            return this;
        }

        /// <summary>Records a frame in one of the two lists and remembers it, so <see cref="FrameOffset"/> knows which frame it is modifying.</summary>
        private IElementBuilder RecordFrame(List<FrameRecipe> frames, Point? sourcePoint, float duration, float scale, string? condition)
        {
            _lastFrame = new FrameRecipe(sourcePoint, duration > 0f ? (float?)duration : null, scale, condition);
            frames.Add(_lastFrame);

            return this;
        }

        public IElementBuilder AddChild(string elementType)
        {
            var child = new ElementBuilder(elementType);
            _children.Add(child);

            return child;
        }

        public IElementBuilder AddBackground(string elementType)
        {
            var placedElement = new ElementBuilder(elementType);
            _background.Add(placedElement);

            return placedElement;
        }

        public IElementBuilder AddForeground(string elementType)
        {
            var placedElement = new ElementBuilder(elementType);
            _foreground.Add(placedElement);

            return placedElement;
        }

        /// <summary>Creates a fresh data object from the recorded fields.</summary>
        internal bool TryBuild(out ElementData element, out string error)
        {
            element = null!;

            if (string.IsNullOrWhiteSpace(_elementType) is true)
            {
                error = "an element was added without a type";
                return false;
            }

            if (Parchment.bookManager.ElementRegistry.TryResolve(_elementType, out ElementRegistration registration) is false || registration is null)
            {
                error = $"there's no element type named \"{_elementType}\"";
                return false;
            }

            object? instance;
            try
            {
                instance = Activator.CreateInstance(registration.Renderer.DataType);
            }
            catch (Exception exception)
            {
                error = $"the element type \"{_elementType}\" couldn't be created ({exception.Message})";
                return false;
            }

            if (instance is not ElementData data)
            {
                error = $"the element type \"{_elementType}\" isn't backed by element data";
                return false;
            }

            // The first action goes to the singular field, which keeps a one-action element reading the way it always has, and any
            // beyond that go to the list. Set before the recorded fields, so an explicit Set on either still wins.
            if (_actions.Count > 0)
            {
                data.Action = _actions[0];

                if (_actions.Count > 1)
                {
                    data.Actions = _actions.GetRange(1, _actions.Count - 1);
                }
            }

            if (_hoverActions.Count > 0)
            {
                data.HoverAction = _hoverActions[0];

                if (_hoverActions.Count > 1)
                {
                    data.HoverActions = _hoverActions.GetRange(1, _hoverActions.Count - 1);
                }
            }

            if (_submitActions.Count > 0 || _textChangedActions.Count > 0)
            {
                if (data is not InputElementData inputData)
                {
                    error = $"[{_elementType}] submit and text changed actions can only be added to an Input";
                    return false;
                }

                if (_submitActions.Count > 0)
                {
                    inputData.SubmitAction = _submitActions[0];

                    if (_submitActions.Count > 1)
                    {
                        inputData.SubmitActions = _submitActions.GetRange(1, _submitActions.Count - 1);
                    }
                }

                if (_textChangedActions.Count > 0)
                {
                    inputData.TextChangedAction = _textChangedActions[0];

                    if (_textChangedActions.Count > 1)
                    {
                        inputData.TextChangedActions = _textChangedActions.GetRange(1, _textChangedActions.Count - 1);
                    }
                }
            }

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = $"[{_elementType}] {fieldError}";
                    return false;
                }
            }

            if (_hasOrphanFrameModifier is true)
            {
                error = $"[{_elementType}] FrameOffset or FrameAction was called before any frame was added";
                return false;
            }

            if (_frames.Count > 0 || _hoverFrames.Count > 0)
            {
                if (data is not ImageElementData imageData)
                {
                    error = $"[{_elementType}] frames can only be added to an Image";
                    return false;
                }

                if (_frames.Count > 0)
                {
                    imageData.Frames = CreateFrames(_frames);
                }

                if (_hoverFrames.Count > 0)
                {
                    imageData.HoverFrames = CreateFrames(_hoverFrames);
                }
            }

            if (_resultSource is not null || _resultTemplate is not null)
            {
                if (data is not GridElementData gridData)
                {
                    error = $"[{_elementType}] results can only be added to a Grid";
                    return false;
                }

                if (_resultTemplate is null)
                {
                    error = $"[{_elementType}] results need a template, added through AddResultTemplate";
                    return false;
                }

                if (_resultTemplate.TryBuild(out ElementData templateElement, out error) is false)
                {
                    return false;
                }

                var results = new ResultsData() { Source = _resultSource ?? "ALL_ITEMS (O)", InputId = _resultInputId, PerItemCondition = _resultCondition, Count = _resultCount, Template = templateElement };
                if (_resultOrder is not null)
                {
                    if (Enum.TryParse(_resultOrder, ignoreCase: true, out ResultOrder order) is false)
                    {
                        error = $"[{_elementType}] '{_resultOrder}' is not a valid result order";
                        return false;
                    }

                    results.OrderBy = order;
                }

                gridData.Results = results;
            }

            if (_children.Count > 0)
            {
                if (data is not IContainer container)
                {
                    error = $"[{_elementType}] children can only be added to a container such as a Panel";
                    return false;
                }

                var children = new List<ElementData>();
                foreach (ElementBuilder child in _children)
                {
                    if (child.TryBuild(out ElementData childElement, out error) is false)
                    {
                        return false;
                    }

                    children.Add(childElement);
                }

                container.Children = children;
            }

            if (_background.Count > 0 || _foreground.Count > 0)
            {
                if (data is not ILayeredContainer layeredContainer)
                {
                    error = $"[{_elementType}] a background or foreground can only be added to a layered container such as a Panel";
                    return false;
                }

                if (_background.Count > 0)
                {
                    if (TryBuildList(_background, out List<ElementData> background, out error) is false)
                    {
                        return false;
                    }

                    layeredContainer.Background = background;
                }

                if (_foreground.Count > 0)
                {
                    if (TryBuildList(_foreground, out List<ElementData> foreground, out error) is false)
                    {
                        return false;
                    }

                    layeredContainer.Foreground = foreground;
                }
            }

            element = data;
            error = string.Empty;

            return true;
        }

        private static bool TryBuildList(List<ElementBuilder> builders, out List<ElementData> elements, out string error)
        {
            elements = new List<ElementData>();

            foreach (ElementBuilder builder in builders)
            {
                if (builder.TryBuild(out ElementData builtElement, out error) is false)
                {
                    return false;
                }

                elements.Add(builtElement);
            }

            error = string.Empty;

            return true;
        }

        private static List<AnimationFrameData> CreateFrames(List<FrameRecipe> recipes)
        {
            var frames = new List<AnimationFrameData>();

            foreach (FrameRecipe recipe in recipes)
            {
                frames.Add(new AnimationFrameData() { SourcePoint = recipe.SourcePoint, Duration = recipe.Duration, Scale = recipe.Scale, Condition = recipe.Condition, Offset = recipe.Offset, Actions = recipe.Actions.Count > 0 ? new List<string>(recipe.Actions) : null });
            }

            return frames;
        }

        private class FrameRecipe
        {
            // Null when the frame keeps whatever the element already draws, which is how an item's icon is animated
            public Point? SourcePoint { get; }
            public float? Duration { get; }
            public float Scale { get; }
            public string? Condition { get; }

            // Set after construction by FrameOffset, and null when the frame draws where the element was laid out, which is every frame that isn't moving
            public Point? Offset { get; set; }

            // Filled after construction by FrameAction, in the order the calls were made
            public List<string> Actions { get; } = new List<string>();

            public FrameRecipe(Point? sourcePoint, float? duration, float scale, string? condition)
            {
                SourcePoint = sourcePoint;
                Duration = duration;
                Scale = scale;
                Condition = condition;
            }
        }
    }
}
