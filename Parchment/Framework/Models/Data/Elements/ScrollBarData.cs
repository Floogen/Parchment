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

        /// <summary>Whether the bar fades out while the reader leaves the text alone. It comes back while the cursor is over the text and whenever the text scrolls, by the reader or by a typewriter.
        /// Off by default, so a reader can always see that the text has more to it.
        /// </summary>
        public bool AutoHide { get; set; } = false;

        /// <summary>How long the bar stays up after the cursor leaves the text or the text stops scrolling before it starts to fade, in seconds. Only used with <see cref="AutoHide"/>.</summary>
        public float HideAfter { get; set; } = 1.5f;

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

            if (HideAfter < 0f)
            {
                return (false, $"\"ScrollBar.HideAfter\" can't be negative.");
            }

            return (true, string.Empty);
        }
    }
}
