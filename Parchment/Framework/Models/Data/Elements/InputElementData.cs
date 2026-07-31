using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Parchment.Framework.Models.Data.Elements
{
    /// <summary>A text box the reader types into. The typed text is held for the reading session and read back by the Parchment_InputMatches, Parchment_InputEquals and Parchment_HasInputText queries,
    /// which is what lets a book filter a list of elements against what has been typed.
    /// </summary>
    public class InputElementData : ElementData, ISprite, ITextContent
    {
        public override ElementType Type => ElementType.Input;

        /// <summary>The handle conditions and actions use to reach this input's text. Required, and expected to be unique within the book.</summary>
        public string? InputId { get; set; }

        /// <summary>Text shown while the input is empty. Purely a prompt, so conditions see an empty input rather than this.</summary>
        public string? Placeholder { get; set; }

        /// <summary>The colour of <see cref="Placeholder"/>, as a name such as "Gray" or a value such as "128 128 128". Falls back to a faded <see cref="TextColor"/>.</summary>
        public string? PlaceholderColor { get; set; }

        /// <summary>The most characters the reader can type. When null the length is unbounded.</summary>
        public int? MaxLength { get; set; }

        public string? TexturePath { get; set; }
        public Rectangle? TextureSourceRectangle { get; set; }
        public Rectangle? HoverTextureSourceRectangle { get; set; }
        public string? TintColor { get; set; }
        public SpriteEffects SpriteEffects { get; set; }

        public FontType FontType { get; set; }

        /// <summary>The text the input starts with. The reader can edit it, and clearing the input does not bring it back.</summary>
        public string? Text { get; set; }
        public string? TextColor { get; set; }
        public float TextScale { get; set; } = 1f;

        public int Padding { get; set; } = 0;
        public SizingMode Sizing { get; set; } = SizingMode.Fill;
        public int? Width { get; set; }

        /// <summary>The content height in unscaled pixels, multiplied by <see cref="ElementData.Scale"/>. Gives the box a scale-driven term of its own, so both axes answer to Scale at the same rate.
        /// When null the box is only as tall as a line of text, which leaves height responding to Scale through the frame and padding alone.
        /// </summary>
        public int? Height { get; set; }

        /// <summary>A trigger action to run when the reader presses enter. Shorthand for a single-entry <see cref="SubmitActions"/>, and when both are given this one runs first.</summary>
        public string? SubmitAction { get; set; }

        /// <summary>The trigger actions to run, in order, when the reader presses enter. Combined with <see cref="SubmitAction"/> rather than replacing it.</summary>
        public List<string>? SubmitActions { get; set; }

        /// <summary>An input always claims the cursor, since it has to be clickable to be typed into even when it carries no tooltip or action.</summary>
        public override bool IsAlwaysInteractive => true;

        /// <summary>Whether this input has at least one submit action, from either <see cref="SubmitAction"/> or <see cref="SubmitActions"/>.</summary>
        internal bool HasSubmitActions => HasAny(SubmitAction, SubmitActions);

        /// <summary>Every submit action on this input, <see cref="SubmitAction"/> first and then <see cref="SubmitActions"/> in order, skipping empty entries.</summary>
        public IEnumerable<string> GetSubmitActions() => Combine(SubmitAction, SubmitActions);

        public override (bool Result, string Error) IsValid()
        {
            if (string.IsNullOrWhiteSpace(InputId))
            {
                return (false, $"\"InputId\" is required.");
            }

            if (SubmitActions is not null && SubmitActions.Any(string.IsNullOrWhiteSpace))
            {
                return (false, $"\"SubmitActions\" contains an empty entry.");
            }

            if (MaxLength is int maximumLength && maximumLength <= 0)
            {
                return (false, $"\"MaxLength\" must be positive.");
            }

            if (Sizing is SizingMode.Fixed && Width is null)
            {
                return (false, $"\"Width\" is required when \"Sizing\" is {nameof(SizingMode.Fixed)}.");
            }

            if (Width is int width && width <= 0)
            {
                return (false, $"\"Width\" must be positive.");
            }

            if (Height is int height && height <= 0)
            {
                return (false, $"\"Height\" must be positive.");
            }

            if (Padding < 0)
            {
                return (false, $"\"Padding\" cannot be negative.");
            }

            return base.IsValid();
        }
    }
}
