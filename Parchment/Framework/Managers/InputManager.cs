using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Managers
{
    /// <summary>Holds the text typed into <see cref="Models.Enums.ElementType.Input"/> elements, keyed by their InputId.
    /// The text lives here rather than on the element because element data is the shared cached asset instance and the runtime element is rebuilt whenever a book is created, while a game state query has neither to hand.
    /// State is per reading session: <see cref="ClearAll"/> runs when the book menu closes, so nothing needs saving.
    /// </summary>
    public class InputManager : BaseManager
    {
        private readonly Dictionary<string, string> _inputIdToText = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public InputManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        /// <summary>The text currently in an input, or an empty string when it has none or doesn't exist.</summary>
        public string GetText(string? inputId)
        {
            if (string.IsNullOrWhiteSpace(inputId) || _inputIdToText.TryGetValue(inputId, out string? text) is false)
            {
                return string.Empty;
            }

            return text ?? string.Empty;
        }

        public void SetText(string? inputId, string? text)
        {
            if (string.IsNullOrWhiteSpace(inputId))
            {
                return;
            }

            _inputIdToText[inputId] = text ?? string.Empty;
        }

        /// <summary>Whether an input with this ID has been reached this session, whatever its text. Used to tell an empty input apart from a mistyped ID.</summary>
        public bool IsKnown(string? inputId)
        {
            return string.IsNullOrWhiteSpace(inputId) is false && _inputIdToText.ContainsKey(inputId);
        }

        /// <summary>Records an input's starting text the first time it is laid out. A later call does nothing, so clearing an input doesn't put its authored text back.</summary>
        public void Seed(string? inputId, string? text)
        {
            if (string.IsNullOrWhiteSpace(inputId) || _inputIdToText.ContainsKey(inputId) is true)
            {
                return;
            }

            _inputIdToText[inputId] = text ?? string.Empty;
        }

        public void ClearAll()
        {
            _inputIdToText.Clear();
        }
    }
}
