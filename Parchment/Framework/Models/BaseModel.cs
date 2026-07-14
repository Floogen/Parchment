using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models
{
    public abstract class BaseModel
    {
        public abstract (bool Result, string Error) IsValid();
    }
}
