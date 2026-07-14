using Microsoft.Xna.Framework;
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

        public List<PageElementData> Elements { get; set; } = new List<PageElementData>();

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
