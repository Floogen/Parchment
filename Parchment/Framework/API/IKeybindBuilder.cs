namespace Parchment.Framework.API
{
    /// <summary>Builds one keybind, on a book or on a single page. Obtained from <see cref="IBookBuilder.OnKeyPress(string)"/> or <see cref="IPageBuilder.OnKeyPress(string)"/>.</summary>
    public interface IKeybindBuilder
    {
        /// <summary>The buttons this keybind was added under.</summary>
        string Keybind { get; }

        /// <summary>Sets any field on the keybind by name, for anything the methods below don't cover. Fields that don't exist are reported when the book is registered, along with the ones that do.</summary>
        IKeybindBuilder Set(string field, object? value);

        /// <summary>Adds a trigger action to run when the keybind is pressed. Calling this more than once runs them in the order they were added, and at least one is required.</summary>
        IKeybindBuilder Action(string action);

        /// <summary>A game state query deciding whether the actions run, checked at the moment the button is pressed rather than polled while the book is open.</summary>
        IKeybindBuilder Condition(string condition);

        /// <summary>The sound played when the keybind is pressed, once however many actions run. Left alone, nothing plays.</summary>
        IKeybindBuilder Sound(string sound);

        /// <summary>Whether a match stops the button reaching the menu's own handling, which is what lets a book take the exit button over. Defaults to true.
        /// The reader can always leave by holding the exit button down for three seconds, so claiming it can't strand them.
        /// </summary>
        IKeybindBuilder SuppressDefault(bool suppressDefault = true);
    }
}
