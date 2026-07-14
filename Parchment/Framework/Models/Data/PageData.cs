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

        // What kind of page this is
        public PageType Type { get; set; } = PageType.Text;

        // Content
        public string? Title { get; set; }
        public string? Text { get; set; }

        // Image paths
        public string? ImagePath { get; set; }
        public Rectangle? ImageSourceRectangle { get; set; }
        public float ImageScale { get; set; } = 4f;

        // Explicit ordering (ties broken by array position?)
        public int Order { get; set; } = 0;

        public override (bool Result, string Error) IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
