using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parchment.Framework.Models.Data.Books
{
    public class BookAnimationData : BaseModel
    {
        public float SlideDuration { get; set; } = 350f;
        public float OpenDuration { get; set; } = 250f;
        public float CloseDuration { get; set; } = 400f;
        public float TurnDuration { get; set; } = 500f;
        public float CurlDuration { get; set; } = 250f;

        /// <summary>The point in the turn animation at which page content swaps, from 0 to 1.</summary>
        public float ContentSwapProgress { get; set; } = 0.5f;

        public string? OpenSound { get; set; } = "shwip";
        public string? CloseSound { get; set; } = null;
        public string? TurnSound { get; set; } = "shwip";

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
