using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Pages
{
    public class PageTriggerData : BaseModel
    {
        /// <summary>A game state query determining whether <see cref="Actions"/> run. When null, the actions always run.
        /// Unlike <see cref="Elements.ElementData.Condition"/> this is evaluated once, at the moment the page becomes visible, rather than polled while the book is open.
        /// </summary>
        public string? Condition { get; set; }

        /// <summary>The trigger actions to run, in order, when the page becomes visible and <see cref="Condition"/> passes.</summary>
        public List<string> Actions { get; set; } = new List<string>();

        public override (bool Result, string Error) IsValid()
        {
            if (Actions.Count is 0)
            {
                return (false, $"\"Actions\" requires at least one entry.");
            }

            if (Actions.Any(string.IsNullOrWhiteSpace))
            {
                return (false, $"\"Actions\" contains an empty entry.");
            }

            return (true, string.Empty);
        }
    }
}
