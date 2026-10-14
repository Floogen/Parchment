namespace Parchment.Framework.Models.Data
{
    /// <summary>How the scroll bar beside a scrolling element looks. A flat bar in a single color, drawn only while there is more text than fits.
    /// Every color is optional and follows the element's text color when left unset, so the bar matches the page it sits on without any of it being given.
    /// </summary>
    public class ScrollBarData : BaseModel
    {
        /// <summary>How wide the bar is drawn, in unscaled pixels multiplied by the element's Scale.</summary>
        public int Width { get; set; } = 6;

        /// <summary>The room left between the text and the bar, in unscaled pixels multiplied by the element's Scale. The text wraps short of the bar by this much plus <see cref="Width"/>.</summary>
        public int Gap { get; set; } = 6;

        /// <summary>The color of the thumb, the part that moves. Left unset, it is the element's text color at a low strength.</summary>
        public string? Color { get; set; }

        /// <summary>The color of the thumb while the cursor is over the bar. Left unset, it is <see cref="Color"/> when that is given and a stronger take on the text color when it isn't.</summary>
        public string? HoverColor { get; set; }

        /// <summary>The color of the thumb while it is being dragged. Left unset, it is the same as <see cref="HoverColor"/>.</summary>
        public string? ActiveColor { get; set; }

        /// <summary>The color of the channel the thumb runs along. Left unset, no channel is drawn and only the thumb shows.</summary>
        public string? TrackColor { get; set; }

        public override (bool Result, string Error) IsValid()
        {
            if (Width <= 0)
            {
                return (false, $"\"ScrollBar.Width\" must be positive.");
            }

            if (Gap < 0)
            {
                return (false, $"\"ScrollBar.Gap\" can't be negative.");
            }

            return (true, string.Empty);
        }
    }
}
