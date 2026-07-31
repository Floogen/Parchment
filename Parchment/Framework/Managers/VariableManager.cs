using Parchment.Framework.Models.Data;
using Parchment.Framework.Models.Data.Variables;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.UI.Menus;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Parchment.Framework.Managers
{
    /// <summary>Holds the values of the variables books declare, which unlike a session flag outlive the book being put down.
    /// Save-scoped values live in the player's modData so the game saves them alongside everything else, and global ones live in a file of Parchment's own.
    /// </summary>
    public class VariableManager : BaseManager
    {
        public const string GLOBAL_DATA_KEY = "variables";

        // Save-scoped values are namespaced under the mod ID, as every mod writing to a farmer's modData shares one dictionary
        private const string MOD_DATA_PREFIX = "PeacefulEnd.Parchment/Variables/";

        private Dictionary<string, string> _globalValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private bool _hasUnsavedGlobalValues = false;

        public VariableManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {
            _globalValues = helper.Data.ReadGlobalData<Dictionary<string, string>>(GLOBAL_DATA_KEY) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The book an action, query or token is speaking about, being whichever one is open. Variables belong to a book, so there is nothing to address without one.</summary>
        public bool TryGetCurrentBookId(out string bookId)
        {
            if (Game1.activeClickableMenu is BookMenu bookMenu)
            {
                bookId = bookMenu.Book.Data.Id;

                return true;
            }

            bookId = string.Empty;

            return false;
        }

        /// <summary>Finds what a book declared about one of its variables. Fails when the book isn't loaded or never declared the name, which is what stops a typo becoming a stored value.</summary>
        public bool TryGetDeclaration(string bookId, string variableId, out VariableData declaration, out string error)
        {
            declaration = null!;

            if (Parchment.bookManager.Books.FirstOrDefault(book => book.Id.EqualsIgnoreCase(bookId)) is not BookData bookData)
            {
                error = $"no book with the ID \"{bookId}\" is loaded";
                return false;
            }

            if (bookData.Variables is null || bookData.Variables.Count is 0)
            {
                error = $"the book \"{bookId}\" declares no variables";
                return false;
            }

            if (bookData.Variables.FirstOrDefault(variable => variable.Id.EqualsIgnoreCase(variableId)) is not VariableData match)
            {
                error = $"the book \"{bookId}\" declares no variable named \"{variableId}\". It declares: {string.Join(", ", bookData.Variables.Select(variable => variable.Id))}";
                return false;
            }

            declaration = match;
            error = string.Empty;

            return true;
        }

        /// <summary>The variable's current value, or its default when nothing has set it yet. A declared variable always answers with something.</summary>
        /// <summary>The variable's current value for a player, or its default when nothing has set it yet. A declared variable always answers with something.</summary>
        /// <remarks>A Global variable is shared, so the player is ignored for one. A Save variable belongs to the farmer it was set on.</remarks>
        public string Get(Farmer who, string bookId, VariableData declaration)
        {
            string key = GetKey(bookId, declaration.Id);

            if (declaration.Scope is VariableScope.Global)
            {
                return _globalValues.TryGetValue(key, out string? globalValue) ? globalValue : declaration.GetDefault();
            }

            if (who is null || Context.IsWorldReady is false)
            {
                return declaration.GetDefault();
            }

            return who.modData.TryGetValue(MOD_DATA_PREFIX + key, out string? savedValue) ? savedValue : declaration.GetDefault();
        }

        public bool TryGet(Farmer who, string bookId, string variableId, out string value, out string error)
        {
            value = string.Empty;

            if (TryGetDeclaration(bookId, variableId, out VariableData declaration, out error) is false)
            {
                return false;
            }

            value = Get(who, bookId, declaration);

            return true;
        }

        public bool TrySet(Farmer who, string bookId, string variableId, string value, out string error)
        {
            if (TryGetDeclaration(bookId, variableId, out VariableData declaration, out error) is false)
            {
                return false;
            }

            if (declaration.TryValidateValue(value, out string valueError) is false)
            {
                error = $"\"{variableId}\" cannot hold {valueError}";
                return false;
            }

            return TryStore(who, bookId, declaration, value, out error);
        }

        /// <summary>Returns a variable to its declared default. A declared variable has no absent state, so this is a reset rather than a removal.</summary>
        public bool TryClear(Farmer who, string bookId, string variableId, out string error)
        {
            return TryClearAll(who, bookId, new string[] { variableId }, out error);
        }

        /// <summary>Returns several variables to their declared defaults, all of them or none.
        /// Every name is resolved and checked before anything is written, so one bad name can't leave the rest half applied.
        /// </summary>
        public bool TryClearAll(Farmer who, string bookId, IEnumerable<string> variableIds, out string error)
        {
            if (TryGetDeclarations(bookId, variableIds, out List<VariableData> declarations, out error) is false)
            {
                return false;
            }

            foreach (VariableData declaration in declarations)
            {
                if (CanStore(who, declaration, out error) is false)
                {
                    return false;
                }
            }

            foreach (VariableData declaration in declarations)
            {
                Store(who, bookId, declaration, declaration.GetDefault());
            }

            error = string.Empty;

            return true;
        }

        /// <summary>Flips a boolean variable, which is what a checkbox needs rather than a pair of conditioned SetVariable buttons.</summary>
        public bool TryToggle(Farmer who, string bookId, string variableId, out string error)
        {
            return TryToggleAll(who, bookId, new string[] { variableId }, out error);
        }

        /// <summary>Flips several boolean variables, all of them or none. A non-boolean anywhere in the list stops the whole thing before anything is written.</summary>
        public bool TryToggleAll(Farmer who, string bookId, IEnumerable<string> variableIds, out string error)
        {
            if (TryGetDeclarations(bookId, variableIds, out List<VariableData> declarations, out error) is false)
            {
                return false;
            }

            foreach (VariableData declaration in declarations)
            {
                if (declaration.Type is not VariableType.Boolean)
                {
                    error = $"\"{declaration.Id}\" is a {declaration.Type} variable, and only Boolean variables can be toggled";
                    return false;
                }

                if (CanStore(who, declaration, out error) is false)
                {
                    return false;
                }
            }

            foreach (VariableData declaration in declarations)
            {
                string flipped = bool.TryParse(Get(who, bookId, declaration), out bool current) is true && current is true ? "false" : "true";

                Store(who, bookId, declaration, flipped);
            }

            error = string.Empty;

            return true;
        }

        // Resolves a whole list of names up front, so a caller can check everything before it writes anything
        private bool TryGetDeclarations(string bookId, IEnumerable<string> variableIds, out List<VariableData> declarations, out string error)
        {
            declarations = new List<VariableData>();
            error = string.Empty;

            foreach (string variableId in variableIds)
            {
                if (TryGetDeclaration(bookId, variableId, out VariableData declaration, out error) is false)
                {
                    return false;
                }

                declarations.Add(declaration);
            }

            return true;
        }

        /// <summary>Whether a variable currently holds the given value, compared as the declared type rather than as text in every case.</summary>
        public bool Matches(Farmer who, string bookId, VariableData declaration, string value)
        {
            string current = Get(who, bookId, declaration);

            if (declaration.Type is VariableType.Number)
            {
                return double.TryParse(current, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentNumber) is true && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double targetNumber) is true && currentNumber == targetNumber;
            }

            return current.EqualsIgnoreCase(value);
        }

        /// <summary>Every declared variable across every loaded book, as "bookId/variableId=value" entries. This is what the Content Patcher token publishes.</summary>
        /// <param name="who">The player whose Save-scoped values are read, or null before a save is loaded, which reads their defaults instead.</param>
        public IEnumerable<string> GetAllValues(Farmer who)
        {
            var values = new List<string>();

            foreach (BookData bookData in Parchment.bookManager.Books)
            {
                if (bookData.Variables is null)
                {
                    continue;
                }

                foreach (VariableData declaration in bookData.Variables)
                {
                    values.Add($"{GetKey(bookData.Id, declaration.Id)}={Get(who, bookData.Id, declaration)}");
                }
            }

            // Ordered so a pack's valueAt index doesn't shift when a book is added or reloaded
            return values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Writes the global values out, if any have moved since the last time. Save-scoped ones need no flush, as the game saves modData itself.
        /// Called once a second as well as at the obvious moments, so a global set outside a book is never more than a second from disk. The dirty check makes the quiet case free.
        /// </summary>
        public void Save()
        {
            if (_hasUnsavedGlobalValues is false)
            {
                return;
            }

            helper.Data.WriteGlobalData(GLOBAL_DATA_KEY, _globalValues);
            _hasUnsavedGlobalValues = false;
        }

        private bool TryStore(Farmer who, string bookId, VariableData declaration, string value, out string error)
        {
            if (CanStore(who, declaration, out error) is false)
            {
                return false;
            }

            Store(who, bookId, declaration, value);

            return true;
        }

        /// <summary>Whether this variable has somewhere to be written right now, which is the only way storing can fail once the name has resolved.</summary>
        private static bool CanStore(Farmer who, VariableData declaration, out string error)
        {
            if (declaration.Scope is not VariableScope.Global && (who is null || Context.IsWorldReady is false))
            {
                error = $"\"{declaration.Id}\" is a Save variable, which has no player to be stored on until a save is loaded. Give it a Global scope if it should be settable from the title screen";
                return false;
            }

            error = string.Empty;

            return true;
        }

        private void Store(Farmer who, string bookId, VariableData declaration, string value)
        {
            string key = GetKey(bookId, declaration.Id);

            if (declaration.Scope is VariableScope.Global)
            {
                _globalValues[key] = value;
                _hasUnsavedGlobalValues = true;

                return;
            }

            who.modData[MOD_DATA_PREFIX + key] = value;
        }

        // Keyed by book so two books declaring the same name can't read each other's value
        private static string GetKey(string bookId, string variableId)
        {
            return $"{bookId}/{variableId}";
        }
    }
}
