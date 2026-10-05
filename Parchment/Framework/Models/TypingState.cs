namespace Parchment.Framework.Models
{
    /// <summary>How far along one [typewriter] in an element's text is. Kept on the element rather than the effect, as the effect is rebuilt whenever the text is laid out while the typing carries on.</summary>
    public class TypingState
    {
        /// <summary>When the first character appears, on the animation clock, with any delay already added. Null until the typewriter has been scheduled. None of its text is drawn until then.</summary>
        public double? StartTime { get; set; }

        /// <summary>Whether every character is showing, whether because it typed them all or because the reader clicked to finish it.</summary>
        public bool IsComplete { get; set; }

        /// <summary>When the typewriter finished, on the animation clock. Earlier than it would have typed out when the reader clicked to finish it, which is what lets the typewriters waiting on it start straight away.</summary>
        public double CompletedAt { get; set; }

        /// <summary>Whether the typewriter's condition failed when it would have started, so its text showed in full at once rather than typing out.</summary>
        public bool IsInstant { get; set; }

        /// <summary>Whether each of the typewriter's pauses holds, by its place in <see cref="UI.Layouts.TextEffect.Pauses"/>. Taken once when the typewriter starts, the same moment its own condition is checked.</summary>
        public List<bool> PausesHeld { get; } = new List<bool>();

        /// <summary>How many characters were showing when the sound was last played for it, so each newly revealed character can be told apart from one already counted.</summary>
        public int LastSoundedCount { get; set; }
    }
}
