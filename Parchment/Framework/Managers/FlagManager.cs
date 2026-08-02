using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Managers
{
    /// <summary>Holds the flags a book has set for the current reading session, as a plain set of names.
    /// These exist for state that has to outlive a single frame or page turn without outliving the book, such as an animation recording that it has already played.
    /// Anything that should survive the book being put down belongs in a mail flag instead, which the game saves.
    /// </summary>
    public class FlagManager : BaseManager
    {
        private readonly HashSet<string> _flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public FlagManager(IMonitor monitor, IModHelper helper) : base(monitor, helper)
        {

        }

        public bool Has(string? flag)
        {
            return string.IsNullOrWhiteSpace(flag) is false && _flags.Contains(flag);
        }

        public void Set(string? flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                return;
            }

            _flags.Add(flag);
        }

        public void Clear(string? flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                return;
            }

            _flags.Remove(flag);
        }

        public void ClearAll()
        {
            _flags.Clear();
        }
    }
}
