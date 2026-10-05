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
                if (element.TypewriterEffects.Count is 0)
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
                    }

                    double end = GetEndTime(effect, typing, state.StartTime.Value);

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
            }
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
                    if (effect.Typing?.Sound is not string effectSound || element.TypingStates.TryGetValue(effect.TypingOrdinal, out TypingState? state) is false || state.StartTime is not double start || state.IsComplete)
                    {
                        continue;
                    }

                    int revealedCount = Math.Clamp((int)Math.Floor((time - start) / effect.Typing.Speed) + 1, 0, effect.Length);

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

            if (state.IsComplete)
            {
                return 1f;
            }

            double appearsAt = start + (position - effect.Start) * typing.Speed;

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

        /// <summary>When a typewriter's last character has fully appeared.</summary>
        private static double GetEndTime(TextEffect effect, TypingOptions typing, double start)
        {
            return start + Math.Max(0, effect.Length - 1) * typing.Speed + typing.FadeDuration;
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
