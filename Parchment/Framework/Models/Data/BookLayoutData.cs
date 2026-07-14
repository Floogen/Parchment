using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data
{
    public class BookLayoutData : BaseModel
    {
        public int MarginOuter { get; set; } = 58;
        public int MarginSpine { get; set; } = 32;
        public int MarginTop { get; set; } = 175;
        public int MarginBottom { get; set; } = 100;

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
