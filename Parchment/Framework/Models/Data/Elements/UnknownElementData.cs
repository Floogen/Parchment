using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class UnknownElementData : ElementData
    {
        public override ElementType Type => ElementType.Unknown;
        public override int SpacingAfter => 0;

        public override (bool Result, string Error) IsValid()
        {
            return (false, "Unknown Type!");
        }
    }
}
