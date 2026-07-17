using Microsoft.Xna.Framework;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class PageData : BaseModel
    {
        public string Id { get; set; } = string.Empty;

        public List<ElementData> Elements { get; set; } = new List<ElementData>();

        /// <summary>
        /// Elements drawn behind <see cref="Elements"/>, positioned absolutely via <see cref="ElementData.Position"/> rather than stacked. These do not affect the layout.
        /// </summary>
        public List<ElementData>? Background { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
