using Microsoft.Xna.Framework.Input;
using StardewValley;
using System;

namespace Parchment.Framework.UI.Menus
{
    /// <summary>Receives typed characters on behalf of a focused Input element, writing straight through to the input manager so the text survives the relayout that each keystroke triggers.
    /// The misspelled method names come from the game's own <see cref="IKeyboardSubscriber"/>.
    /// </summary>
    internal class InputTextSubscriber : IKeyboardSubscriber
    {
        private readonly Action _onTextChanged;
        private readonly Action _onSubmit;

        public string InputId { get; }
        public int? MaxLength { get; }

        public bool Selected { get; set; }

        internal InputTextSubscriber(string inputId, int? maxLength, Action onTextChanged, Action onSubmit)
        {
            InputId = inputId;
            MaxLength = maxLength;
            _onTextChanged = onTextChanged;
            _onSubmit = onSubmit;
        }

        public void RecieveTextInput(char inputChar)
        {
            // Control characters arrive through RecieveCommandInput instead, so anything that reaches here and isn't printable is not ours to append
            if (char.IsControl(inputChar) is true)
            {
                return;
            }

            Append(inputChar.ToString());
        }

        public void RecieveTextInput(string text)
        {
            Append(text);
        }

        public void RecieveCommandInput(char command)
        {
            switch (command)
            {
                case '\b':
                    Backspace();
                    break;
                case '\r':
                    _onSubmit();
                    break;
            }
        }

        public void RecieveSpecialInput(Keys key)
        {

        }

        private void Append(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string currentText = Parchment.inputManager.GetText(InputId);
            string appendedText = string.Concat(currentText, text);

            if (MaxLength is int maximumLength && appendedText.Length > maximumLength)
            {
                appendedText = appendedText.Substring(0, maximumLength);
            }

            if (appendedText == currentText)
            {
                return;
            }

            Parchment.inputManager.SetText(InputId, appendedText);
            _onTextChanged();
        }

        private void Backspace()
        {
            string currentText = Parchment.inputManager.GetText(InputId);
            if (currentText.Length is 0)
            {
                return;
            }

            Parchment.inputManager.SetText(InputId, currentText.Substring(0, currentText.Length - 1));
            _onTextChanged();
        }
    }
}
