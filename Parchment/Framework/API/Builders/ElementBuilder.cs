using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Animations;
using Parchment.Framework.Models.Data.Elements;
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
        private readonly List<FrameRecipe> _frames = new List<FrameRecipe>();
        private readonly List<FrameRecipe> _hoverFrames = new List<FrameRecipe>();
        private readonly List<string> _actions = new List<string>();
        private readonly List<string> _hoverActions = new List<string>();

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
        public IElementBuilder Condition(string condition) { return Set("Condition", condition); }
        public IElementBuilder Sizing(string sizingMode) { return Set("Sizing", sizingMode); }
        public IElementBuilder Scope(string scope) { return Set("Scope", scope); }
        public IElementBuilder Format(string format) { return Set("Format", format); }
        public IElementBuilder Width(int width) { return Set("Width", width); }
        public IElementBuilder Height(int height) { return Set("Height", height); }
        public IElementBuilder Padding(int padding) { return Set("Padding", padding); }
        public IElementBuilder Spacing(int spacingAfter) { return Set("SpacingAfter", spacingAfter); }
        public IElementBuilder Margin(int left, int right) { return Set("MarginLeft", left).Set("MarginRight", right); }
        public IElementBuilder Tooltip(string displayName, string description) { return Set("DisplayName", displayName).Set("Description", description); }

        public IElementBuilder AddFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null)
        {
            _frames.Add(new FrameRecipe(x, y, duration > 0f ? (float?)duration : null, scale, condition));

            return this;
        }

        public IElementBuilder AddHoverFrame(int x, int y, float duration = 0f, float scale = 1f, string? condition = null)
        {
            _hoverFrames.Add(new FrameRecipe(x, y, duration > 0f ? (float?)duration : null, scale, condition));

            return this;
        }

        public IElementBuilder AddChild(string elementType)
        {
            var child = new ElementBuilder(elementType);
            _children.Add(child);

            return child;
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

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = $"[{_elementType}] {fieldError}";
                    return false;
                }
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

            element = data;
            error = string.Empty;

            return true;
        }

        private static List<AnimationFrameData> CreateFrames(List<FrameRecipe> recipes)
        {
            var frames = new List<AnimationFrameData>();

            foreach (FrameRecipe recipe in recipes)
            {
                frames.Add(new AnimationFrameData() { SourcePoint = new Point(recipe.X, recipe.Y), Duration = recipe.Duration, Scale = recipe.Scale, Condition = recipe.Condition });
            }

            return frames;
        }

        private class FrameRecipe
        {
            public int X { get; }
            public int Y { get; }
            public float? Duration { get; }
            public float Scale { get; }
            public string? Condition { get; }

            public FrameRecipe(int x, int y, float? duration, float scale, string? condition)
            {
                X = x;
                Y = y;
                Duration = duration;
                Scale = scale;
                Condition = condition;
            }
        }
    }
}
