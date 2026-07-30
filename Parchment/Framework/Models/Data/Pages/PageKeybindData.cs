using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Models.Data.Pages
{
    public class PageKeybindData : BaseModel
    {
        /// <summary>The button or buttons running <see cref="Actions"/> while this page is visible, in SMAPI's keybind syntax such as "Escape", "LeftControl + S" or "Escape, Back".
        /// A combination only matches when its other buttons are already held, and a comma-separated list matches when any one of its combinations does.
        /// </summary>
        public string Keybind { get; set; } = string.Empty;

        /// <summary>A game state query determining whether <see cref="Actions"/> run. When null, the actions always run.
        /// Unlike <see cref="Elements.ElementData.Condition"/> this is evaluated once, at the moment the button is pressed, rather than polled while the book is open.
        /// </summary>
        public string? Condition { get; set; }

        /// <summary>A trigger action to run when the keybind is pressed. Shorthand for a single-entry <see cref="Actions"/>, and when both are given this one runs first.</summary>
        public string? Action { get; set; }

        /// <summary>The trigger actions to run, in order, when the keybind is pressed. Combined with <see cref="Action"/> rather than replacing it.</summary>
        public List<string>? Actions { get; set; }

        /// <summary>The sound to play when the keybind is pressed, played once regardless of how many actions run. When null, nothing plays.</summary>
        public string? Sound { get; set; }

        /// <summary>Whether a match stops the button reaching the menu's own handling, which is what lets a page take over the exit button.
        /// The reader can always leave by holding the exit button down for three seconds, so a page that claims it can't strand them.
        /// </summary>
        public bool SuppressDefault { get; set; } = true;

        // Parsing is cached against the string it came from, so a Content Patcher edit that rewrites the field is picked up without a stale bind surviving
        private string? _parsedKeybindSource;
        private KeybindList? _parsedKeybind;

        /// <summary>Whether this entry has at least one action, from either <see cref="Action"/> or <see cref="Actions"/>.</summary>
        internal bool HasActions => string.IsNullOrWhiteSpace(Action) is false || (Actions is not null && Actions.Any(action => string.IsNullOrWhiteSpace(action) is false));

        /// <summary>Every action on this entry, <see cref="Action"/> first and then <see cref="Actions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetActions()
        {
            if (string.IsNullOrWhiteSpace(Action) is false)
            {
                yield return Action;
            }

            if (Actions is null)
            {
                yield break;
            }

            foreach (string action in Actions)
            {
                if (string.IsNullOrWhiteSpace(action) is false)
                {
                    yield return action;
                }
            }
        }

        /// <summary>Whether the given button fires this entry, with every other button in the combination currently held.
        /// The pressed button is matched wherever it sits in the combination, so "LeftControl + S" fires on S while control is down and not the other way round.
        /// </summary>
        internal bool Matches(SButton button)
        {
            KeybindList? keybindList = GetKeybindList();
            if (keybindList is null)
            {
                return false;
            }

            foreach (Keybind keybind in keybindList.Keybinds)
            {
                if (keybind.Buttons.Contains(button) is false)
                {
                    continue;
                }

                if (keybind.Buttons.All(other => other == button || Parchment.modHelper.Input.IsDown(other)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The parsed form of <see cref="Keybind"/>, or null when it is empty or unparsable.</summary>
        private KeybindList? GetKeybindList()
        {
            if (string.IsNullOrWhiteSpace(Keybind))
            {
                return null;
            }

            if (_parsedKeybind is not null && string.Equals(_parsedKeybindSource, Keybind, StringComparison.Ordinal))
            {
                return _parsedKeybind;
            }

            if (KeybindList.TryParse(Keybind, out KeybindList parsed, out _) is false)
            {
                return null;
            }

            _parsedKeybindSource = Keybind;
            _parsedKeybind = parsed;

            return _parsedKeybind;
        }

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(Keybind))
            {
                return (false, $"\"Keybind\" is required.");
            }

            if (KeybindList.TryParse(Keybind, out _, out string[] keybindErrors) is false)
            {
                return (false, $"\"Keybind\" is not a valid key binding: {string.Join(", ", keybindErrors)}");
            }

            if (HasActions is false)
            {
                return (false, $"\"Action\" or \"Actions\" requires at least one entry.");
            }

            if (Actions is not null && Actions.Any(string.IsNullOrWhiteSpace))
            {
                return (false, $"\"Actions\" contains an empty entry.");
            }

            return (true, string.Empty);
        }
    }
}
