using Parchment.Framework.Models.Data;
using System.Collections.Generic;

namespace Parchment.Framework.API.Builders
{
    /// <summary>Records how to build one of a book's keybinds. The recipe is kept rather than the built data, for the same reason the other builders keep theirs:
    /// every asset load gets a fresh object and Content Patcher's edits can't accumulate on the registered original.
    /// </summary>
    public class KeybindBuilder : IKeybindBuilder
    {
        private readonly string _keybind;
        private readonly List<(string Field, object? Value)> _fields = new List<(string Field, object? Value)>();
        private readonly List<string> _actions = new List<string>();

        public string Keybind { get { return _keybind; } }

        internal KeybindBuilder(string keybind)
        {
            _keybind = keybind ?? string.Empty;
        }

        public IKeybindBuilder Set(string field, object? value)
        {
            _fields.Add((field, value));

            return this;
        }

        public IKeybindBuilder Action(string action)
        {
            _actions.Add(action);

            return this;
        }

        public IKeybindBuilder Condition(string condition) { return Set("Condition", condition); }
        public IKeybindBuilder Sound(string sound) { return Set("Sound", sound); }
        public IKeybindBuilder SuppressDefault(bool suppressDefault = true) { return Set("SuppressDefault", suppressDefault); }

        /// <summary>Creates a fresh keybind from the recipe. An unparsable Keybind is left to <see cref="KeybindData.IsValid"/>, which reports it alongside everything else wrong with the book.</summary>
        internal bool TryBuild(out KeybindData keybind, out string error)
        {
            keybind = null!;

            if (string.IsNullOrWhiteSpace(_keybind) is true)
            {
                error = "no keybind was given";
                return false;
            }

            if (_actions.Count is 0)
            {
                error = "no action was given, so pressing it would do nothing";
                return false;
            }

            var data = new KeybindData();

            foreach (var field in _fields)
            {
                if (ModelBinder.TrySet(data, field.Field, field.Value, out string fieldError) is false)
                {
                    error = fieldError;
                    return false;
                }
            }

            // Forced after the fields, so the built keybind always matches the buttons it was added under
            data.Keybind = _keybind;

            // Copied rather than handed over, so a second build can't inherit what a later Action call added
            data.Action = _actions[0];

            if (_actions.Count > 1)
            {
                data.Actions = _actions.GetRange(1, _actions.Count - 1);
            }

            keybind = data;
            error = string.Empty;

            return true;
        }
    }
}
