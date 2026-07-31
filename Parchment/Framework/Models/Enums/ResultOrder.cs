using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Enums
{
    public enum ResultOrder
    {
        /// <summary>Whatever order the item query returned, which is the registry's order rather than anything meaningful to a reader.</summary>
        None,
        DisplayName,
        ItemId
    }
}
