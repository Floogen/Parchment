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
    public class PanelElementData : PageElementData
    {
        /// <summary>
        /// Optional. If not given, fill the entire Page's width
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Optional. If not given, increase based on total height needed by Children
        /// </summary>
        public int Height { get; set; }

        // Sub-elements
        public List<PageElementData>? Children { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
