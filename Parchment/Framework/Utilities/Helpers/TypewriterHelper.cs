using Parchment.Framework.Models;
using Parchment.Framework.Models.Data.Elements;
using Parchment.Framework.Models.Interfaces;
using Parchment.Framework.UI.Layouts;
using System;
using System.Collections.Generic;

namespace Parchment.Framework.Utilities.Helpers
{
    /// <summary>Schedules and reveals [typewriter] text. Unlike the other effects a typewriter can't be worked out from the clock alone, as it starts when its text comes into view and the reader can finish it early,
    /// so its progress is kept on the element in a <see cref="TypingState"/> per typewriter.
    /// </summary>
    public static class TypewriterHelper
    {
        // How far above its resting place a dropping character starts, in unscaled pixels multiplied by the text's scale
        public const float TYPEWRITER_DROP_HEIGHT = 6f;

        // How large a popping character starts, as a multiple of its own size
        public const float TYPEWRITER_POP_SCALE = 1.8f;

        /// <summary>Whether an element's authored text holds a typewriter, which is what puts it on its page's or book's list to be scheduled.</summary>
        public static bool HasTypewriterText(Element element)
        {
            return HasTypewriterText(element.Data);
        }

        /// <summary>Whether authored element data holds a typewriter in its text, being its Text or, for a PageNumber, its Format.</summary>
        public static bool HasTypewriterText(ElementData data)
        {
            if (data is ILinkHost linkHost && TextMarkupHelper.HasTypewriter(linkHost.GetLinkedText()))
            {
                return true;
            }

            return data is ITextContent textContent && TextMarkupHelper.HasTypewriter(textContent.Text);
        }

        /// <summary>Whether an element's authored text holds a typewriter or an effect with a duration, either of which needs a start time from when its text comes into view.</summary>
        public static bool HasScheduledText(Element element)
        {
            return HasScheduledText(element.Data);
        }

        /// <summary>Whether authored element data holds a typewriter or an effect with a duration in its text, being its Text or, for a PageNumber, its Format.</summary>
        public static bool HasScheduledText(ElementData data)
        {
            if (HasTypewriterText(data))
            {
                return true;
            }

            if (data is ILinkHost linkHost && TextMarkupHelper.HasTimedEffect(linkHost.GetLinkedText()))
            {
                return true;
            }

            return data is ITextContent textContent && TextMarkupHelper.HasTimedEffect(textContent.Text);
        }

        /// <summary>Gives every typewriter that has come into view a start time, in order. A typewriter waits for the ones before it to finish unless it was marked immediate.
        /// A hidden element is passed over without holding the others up. It is scheduled from whenever it appears. A start time is never moved once given, so text that has typed out stays typed out.
        /// </summary>
        /// <param name="elements">The elements to schedule, in the order their typewriters should run.</param>
        /// <param name="readyTime">When the elements came into view, being when the spread settled or the book first did for its own layers.</param>
        /// <param name="time">The animation clock, in milliseconds.</param>
        public static void Schedule(IReadOnlyList<Element> elements, double readyTime, double time)
        {
            double chainEnd = double.MinValue;

            foreach (Element element in elements)
            {
                if (element.TypewriterEffects.Count is 0 && element.TimedEffects.Count is 0)
                {
                    continue;
                }

                double? visibleSince = GetVisibleSince(element);

                foreach (TextEffect effect in element.TypewriterEffects)
                {
                    if (effect.Typing is not TypingOptions typing)
                    {
                        continue;
                    }

                    TypingState state = GetOrCreateState(element, effect.TypingOrdinal);

                    if (state.StartTime is null)
                    {
                        if (visibleSince is not double since)
                        {
                            continue;
                        }

                        double ready = Math.Max(readyTime, since);
                        state.StartTime = (typing.IsImmediate ? ready : Math.Max(ready, chainEnd)) + typing.Delay;

                        // Checked once, here, so the text can't appear and vanish as the condition comes and goes. A failed one shows its text in full when it would have started
                        state.IsInstant = typing.Condition is not null && ConditionHelper.Check(typing.Condition, element) is false;

                        // Pause conditions are settled at the same moment, so a pause can't come and go partway through the reveal either
                        state.PausesHeld.Clear();
                        foreach (TypingPause pause in effect.Pauses)
                        {
                            state.PausesHeld.Add(pause.Condition is null || ConditionHelper.Check(pause.Condition, element));
                        }
                    }

                    double end = state.IsInstant ? state.StartTime.Value : GetEndTime(effect, typing, state);

                    if (state.IsComplete is false && time >= end)
                    {
                        state.IsComplete = true;
                        state.CompletedAt = end;
                    }

                    if (typing.IsImmediate is false)
                    {
                        chainEnd = Math.Max(chainEnd, state.IsComplete ? state.CompletedAt : end);
                    }
                }

                // After the typewriters, as a timed effect inside one starts when the typewriter reaches it
                ScheduleTimedEffects(element, readyTime, visibleSince);
            }
        }

        /// <summary>Gives each of an element's timed effects a start time once its text is showing. One inside a typewriter starts as its first character appears (or straight away once the reader clicks to finish the typing).
        /// Any other starts when its element came into view, the same moment a typewriter in it would. A start time is never moved later, so an effect that has played stays played.
        /// </summary>
        private static void ScheduleTimedEffects(Element element, double readyTime, double? visibleSince)
        {
            foreach (TextEffect effect in element.TimedEffects)
            {
                bool hasStart = element.TimedEffectStarts.TryGetValue(effect.TimedOrdinal, out double start);

                if (GetCoveringTypewriter(element, effect) is TextEffect typewriter)
                {
                    if (element.TypingStates.TryGetValue(typewriter.TypingOrdinal, out TypingState? state) is false || state.StartTime is not double typingStart)
                    {
                        continue;
                    }

                    double appearsAt = state.IsInstant ? typingStart : GetAppearTime(typewriter, typewriter.Typing!, state, effect.Start - typewriter.Start);

                    // A click shows the text at once, so an effect still waiting on the typing starts then rather than where the typing would have reached
                    if (state.IsComplete)
                    {
                        appearsAt = Math.Min(appearsAt, state.CompletedAt);
                    }

                    if (hasStart is false || appearsAt < start)
                    {
                        element.TimedEffectStarts[effect.TimedOrdinal] = appearsAt;
                    }

                    continue;
                }

                if (hasStart is false && visibleSince is double since)
                {
                    element.TimedEffectStarts[effect.TimedOrdinal] = Math.Max(readyTime, since);
                }
            }
        }

        /// <summary>The innermost typewriter whose text holds the start of an effect. Null when the effect isn't inside one.</summary>
        private static TextEffect? GetCoveringTypewriter(Element element, TextEffect effect)
        {
            TextEffect? covering = null;

            foreach (TextEffect typewriter in element.TypewriterEffects)
            {
                if (typewriter.Typing is null || effect.Start < typewriter.Start || effect.Start >= typewriter.Start + Math.Max(1, typewriter.Length))
                {
                    continue;
                }

                if (covering is null || typewriter.Start >= covering.Start)
                {
                    covering = typewriter;
                }
            }

            return covering;
        }

        /// <summary>Whether every typewriter in an element's text has finished. False for an element with none, as there is nothing to have finished.</summary>
        public static bool HasFinishedTyping(Element element)
        {
            if (element.TypewriterEffects.Count is 0)
            {
                return false;
            }

            foreach (TextEffect effect in element.TypewriterEffects)
            {
                if (element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) is false || state.IsComplete is false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Whether any scheduled typewriter among the elements is still revealing its text, including one waiting on its delay or its turn.</summary>
        public static bool IsTyping(IReadOnlyList<Element> elements)
        {
            foreach (Element element in elements)
            {
                foreach (TextEffect effect in element.TypewriterEffects)
                {
                    if (element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) && state.StartTime is not null && state.IsComplete is false)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Finishes every scheduled typewriter among the elements at once, the way a click finishes the game's own dialogue.</summary>
        public static void Complete(IReadOnlyList<Element> elements, double time)
        {
            foreach (Element element in elements)
            {
                foreach (TextEffect effect in element.TypewriterEffects)
                {
                    if (element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) && state.StartTime is not null && state.IsComplete is false)
                    {
                        state.IsComplete = true;
                        state.CompletedAt = time;
                    }
                }
            }
        }

        /// <summary>How fully a character shows, from 0 before its typewriter reaches it to 1 once it has appeared (and faded in, when the typewriter fades). 1 for a character under no typewriter.
        /// A character under more than one typewriter shows only as fully as the least revealed of them.
        /// </summary>
        public static float GetRevealAlpha(Element element, IReadOnlyList<TextEffect> effects, int position, double time)
        {
            float alpha = 1f;

            foreach (TextEffect effect in effects)
            {
                if (effect.Typing is not null)
                {
                    alpha = Math.Min(alpha, GetRevealAlpha(element, effect, position, time));
                }
            }

            return alpha;
        }

        /// <summary>Marks each of the elements' links as reachable or not, by whether every typewriter over its text has revealed all of it.</summary>
        public static void RefreshLinkReveal(IReadOnlyList<Element> elements, double time)
        {
            foreach (Element element in elements)
            {
                foreach (Element link in element.Children)
                {
                    link.IsAwaitingReveal = link.LinkTextEnd > 0 && IsRevealed(element, link.LinkTextEnd - 1, time) is false;
                }
            }
        }

        /// <summary>The sound cue to play for characters that appeared since the last call, being the first typewriter's that revealed any. Null when none did or none has a sound.
        /// Only one is returned however many typewriters are revealing at once, so two in step don't play over each other.
        /// </summary>
        public static string? TakeSound(IReadOnlyList<Element> elements, double time)
        {
            string? sound = null;

            foreach (Element element in elements)
            {
                foreach (TextEffect effect in element.TypewriterEffects)
                {
                    if (effect.Typing?.Sound is not string effectSound || element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) is false || state.StartTime is not double start || state.IsComplete || state.IsInstant)
                    {
                        continue;
                    }

                    // Counted on from where the last call left off, as characters only ever appear in order and a pause can't be divided out of a single sum
                    int revealedCount = state.LastSoundedCount;
                    while (revealedCount < effect.Length && GetAppearTime(effect, effect.Typing!, state, revealedCount) <= time)
                    {
                        revealedCount++;
                    }

                    if (revealedCount > state.LastSoundedCount)
                    {
                        state.LastSoundedCount = revealedCount;
                        sound ??= effectSound;
                    }
                }
            }

            return sound;
        }

        private static float GetRevealAlpha(Element element, TextEffect effect, int position, double time)
        {
            if (effect.Typing is not TypingOptions typing || element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) is false || state.StartTime is not double start)
            {
                return 0f;
            }

            if (state.IsComplete || (state.IsInstant && time >= start))
            {
                return 1f;
            }

            double appearsAt = state.IsInstant ? start : GetAppearTime(effect, typing, state, position - effect.Start);

            if (time < appearsAt)
            {
                return 0f;
            }

            if (typing.FadeDuration <= 0f)
            {
                return 1f;
            }

            return (float)Math.Clamp((time - appearsAt) / typing.FadeDuration, 0d, 1d);
        }

        /// <summary>How a character is still moving into place after its typewriter revealed it, taken from whichever typewriter over it has the most of its drop or pop left.
        /// Both ease out, quick at first and slowing as they land, so a character settles rather than stopping with a jolt. A character that has settled (or was finished with a click) is at rest.
        /// </summary>
        /// <param name="dropHeight">How far above its resting place to draw the character, in unscaled pixels multiplied by the text's scale.</param>
        /// <param name="popScale">How much larger than its size to draw the character, being 1 once it has settled.</param>
        public static void GetRevealMotion(Element element, IReadOnlyList<TextEffect> effects, int position, double time, out float dropHeight, out float popScale)
        {
            float dropRemaining = 0f;
            float popRemaining = 0f;

            foreach (TextEffect effect in effects)
            {
                if (effect.Typing is not TypingOptions typing)
                {
                    continue;
                }

                dropRemaining = Math.Max(dropRemaining, GetRemainingMotion(element, effect, typing, position, time, typing.DropDuration));
                popRemaining = Math.Max(popRemaining, GetRemainingMotion(element, effect, typing, position, time, typing.PopDuration));
            }

            dropHeight = TYPEWRITER_DROP_HEIGHT * dropRemaining;
            popScale = 1f + (TYPEWRITER_POP_SCALE - 1f) * popRemaining;
        }

        /// <summary>How much of a character's reveal motion is left, from 1 as it appears to 0 once it has settled, already eased out. 0 for a motion the typewriter doesn't use.</summary>
        private static float GetRemainingMotion(Element element, TextEffect effect, TypingOptions typing, int position, double time, float duration)
        {
            if (duration <= 0f || element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) is false || state.StartTime is not double start)
            {
                return 0f;
            }

            if (state.IsComplete || state.IsInstant)
            {
                return 0f;
            }

            double appearsAt = GetAppearTime(effect, typing, state, position - effect.Start);
            float progress = (float)Math.Clamp((time - appearsAt) / duration, 0d, 1d);

            // Squared, which eases the motion out so it slows as it lands
            return (1f - progress) * (1f - progress);
        }

        private static bool IsRevealed(Element element, int position, double time)
        {
            foreach (TextEffect effect in element.TypewriterEffects)
            {
                if (position >= effect.Start && position < effect.Start + effect.Length && GetRevealAlpha(element, effect, position, time) < 1f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>When a typewriter's last character has fully appeared and settled, counting every pause that holds. A pause after the last character still counts, so whatever waits on the typewriter waits for it too.</summary>
        private static double GetEndTime(TextEffect effect, TypingOptions typing, TypingState state)
        {
            return state.StartTime!.Value + Math.Max(0, effect.Length - 1) * typing.Speed + GetPauseDelay(effect, state, int.MaxValue) + typing.SettleDuration;
        }

        /// <summary>When one of a typewriter's characters appears, held back by every pause before it that holds.</summary>
        /// <param name="index">The character's place in the typewriter, counting from its first.</param>
        private static double GetAppearTime(TextEffect effect, TypingOptions typing, TypingState state, int index)
        {
            return state.StartTime!.Value + index * typing.Speed + GetPauseDelay(effect, state, index);
        }

        /// <summary>How long the pauses at or before a place in a typewriter hold for, counting only those whose condition passed when it started.</summary>
        private static double GetPauseDelay(TextEffect effect, TypingState state, int index)
        {
            double delay = 0d;

            for (int pauseIndex = 0; pauseIndex < effect.Pauses.Count; pauseIndex++)
            {
                TypingPause pause = effect.Pauses[pauseIndex];

                // One the state has no record of was added by a relayout after the typewriter started, so it holds, as a pause without a condition would
                bool isHeld = pauseIndex >= state.PausesHeld.Count || state.PausesHeld[pauseIndex];

                if (pause.Position <= index && isHeld)
                {
                    delay += pause.Duration;
                }
            }

            return delay;
        }

        /// <summary>When the element last came into view, counting the containers it sits in, as a panel that appears brings everything inside it along. Null while it or anything around it is hidden.</summary>
        private static double? GetVisibleSince(Element element)
        {
            double visibleSince = double.MinValue;

            for (Element? current = element; current is not null; current = current.Parent)
            {
                if (current.IsVisible is false || current.IsWithinLifetime is false)
                {
                    return null;
                }

                visibleSince = Math.Max(visibleSince, current.VisibleSince ?? double.MinValue);
            }

            return visibleSince;
        }

        private static TypingState GetOrCreateState(Element element, int ordinal)
        {
            if (element.TypingStates.TryGetValue(ordinal, out TypingState? state) is false)
            {
                state = new TypingState();
                element.TypingStates[ordinal] = state;
            }

            return state;
        }
    }
}
