using Parchment.Framework.Models.Data.Links;
using System.Collections.Generic;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Records how to build one of an element's links. The recipe is kept rather than the built data, for the same reason the other builders keep theirs:
    /// every asset load gets a fresh object and Content Patcher's edits can't accumulate on the registered original.
    /// </summary>
    public class LinkBuilder : ILinkBuilder
    {
        private readonly string _linkId;
        private readonly List<(string Field, object? Value)> _fields = new List<(string Field, object? Value)>();
        private readonly List<string> _actions = new List<string>();
        private readonly List<string> _hoverActions = new List<string>();
        private readonly List<string> _tags = new List<string>();

        public string LinkId { get { return _linkId; } }

        internal LinkBuilder(string linkId)
        {
            _linkId = linkId ?? string.Empty;
        }

        public ILinkBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public ILinkBuilder TextColor(string color) { return Set("TextColor", color); }
        public ILinkBuilder HoverTextColor(string color) { return Set("HoverTextColor", color); }
        public ILinkBuilder Tooltip(string displayName, string description) { return Set("DisplayName", displayName).Set("Description", description); }

        public ILinkBuilder Action(string action)
        {
            _actions.Add(action);

            return this;
        }

        public ILinkBuilder HoverAction(string action)
        {
            _hoverActions.Add(action);

            return this;
        }

        public ILinkBuilder WithTag(string tag)
        {
            _tags.Add(tag);

            return this;
        }

        /// <summary>Creates a fresh link from the recipe. The lists are copied rather than handed over, so a second build can't inherit what a later call added.</summary>
        internal bool TryBuild(out LinkData link, out string error)
        {
            link = null!;

            if (string.IsNullOrWhiteSpace(_linkId) is true)
            {
                error = "a link was added without an id, so no [link] markup could name it";
                return false;
            }

            var data = new LinkData();

            // Set before the recorded fields, the same as an element's, so an explicit Set on either still wins
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

            if (_tags.Count > 0)
            {
                data.Tags = new List<string>(_tags);
            }

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = fieldError;
                    return false;
                }
            }

            link = data;
            error = string.Empty;

            return true;
        }
    }
}
