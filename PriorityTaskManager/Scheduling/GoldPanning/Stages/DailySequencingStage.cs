using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Scheduling.GoldPanning.Stages
{
    /// <summary>
    /// Stage responsible for the final sequencing of tasks within each day. This is the "Mosaic"
    /// phase, where the individual tasks (stones) from each daily bucket are arranged into the
    /// available time slots (the mosaic grid) for that day. It uses a "Front-Loading" or
    /// "Eat the Frog" strategy, scheduling the most complex tasks earlier in the day.
    /// </summary>
    public class DailySequencingStage : ISchedulingStage
    {
        public SchedulingContext Act(SchedulingContext context)
        {
            context.History.Add("Phase 5: Sequencing tasks within days (Mosaic/Front-Loading)...");

            // Retrieve the daily buckets created by the TaskDistributionStage.
            if (!context.SharedState.TryGetValue("DailyBuckets", out var bucketsObj) || 
                bucketsObj is not Dictionary<DateTime, List<TaskItem>> dailyBuckets)
            {
                context.History.Add("  -> No daily buckets found. Skipping sequencing.");
                return context;
            }

            // Retrieve the schedule window to get the actual time slots.
            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var scheduleWindowObj) || 
                scheduleWindowObj is not ScheduleWindow scheduleWindow)
            {
                context.History.Add("  -> No AvailableScheduleWindow found. Cannot determine time slots for sequencing.");
                return context;
            }

            // Iterate through each day that has tasks assigned to it.
            foreach (var date in dailyBuckets.Keys.OrderBy(d => d))
            {
                var tasksForDay = dailyBuckets[date];
                if (tasksForDay.Count == 0) continue;

                // Sort tasks for the day based on the sequencing strategy:
                // 1. Dependency order: a task can never be sequenced before a same-day prerequisite
                //    (a cross-day prerequisite is already guaranteed complete by TaskDistributionStage's
                //    dependency gate, so it does not need to be re-checked here).
                // 2. Urgency: Tasks that are due on or before this day are prioritized to ensure they are completed in time.
                // 3. Complexity ("Eat the Frog"): High-complexity tasks are scheduled first, when energy levels are typically highest.
                // 4. Importance: If urgency and complexity are equal, the more important task goes first.
                var sequence = BuildDependencySafeSequence(tasksForDay, date);

                // Get all available time slots for the current day, ordered chronologically.
                var slotsForDay = scheduleWindow.AvailableSlots
                    .Where(s => s.StartTime.Date == date)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                if (slotsForDay.Count == 0)
                {
                    context.History.Add($"  -> Warning: Work assigned to {date.ToShortDateString()} but no slots available.");
                    continue;
                }

                // This sequencer fills the available time slots linearly with the prioritized tasks.
                // It will fill one slot and, if a task is larger than the slot, continue into the next available slot.
                // At each step it picks the highest-priority task (in `sequence` order) that is both
                // dependency-ready and NotBefore-ready for the current cursor time, so a task blocked
                // by a future NotBefore does not waste the gap before it: lower-priority-but-ready
                // tasks fill that time instead, and the blocked task is placed once its time arrives.
                int currentSlotIndex = 0;
                TimeSpan currentSlotUsed = TimeSpan.Zero;

                var idsToday = tasksForDay.Select(t => t.Id).ToHashSet();
                var remainingForDay = new List<TaskItem>(sequence);
                var placedIdsToday = new HashSet<Guid>();

                foreach (var task in remainingForDay)
                {
                    task.ScheduledParts.Clear(); // Clear any previous scheduling data before creating new chunks.
                }

                while (remainingForDay.Count > 0 && currentSlotIndex < slotsForDay.Count)
                {
                    var currentCursorTime = slotsForDay[currentSlotIndex].StartTime + currentSlotUsed;

                    var candidate = remainingForDay.FirstOrDefault(t =>
                        IsDependencyReadyForToday(t, idsToday, placedIdsToday) &&
                        (!t.NotBefore.HasValue || t.NotBefore.Value.Date != date || t.NotBefore.Value <= currentCursorTime));

                    if (candidate == null)
                    {
                        // Nothing is ready to start right this moment. If at least one dependency-ready
                        // task is only blocked by a future NotBefore time today, jump the cursor forward
                        // to the earliest such time instead of stalling.
                        var readyButNotYetDue = remainingForDay
                            .Where(t => IsDependencyReadyForToday(t, idsToday, placedIdsToday) &&
                                        t.NotBefore.HasValue && t.NotBefore.Value.Date == date)
                            .Select(t => t.NotBefore!.Value)
                            .ToList();

                        if (readyButNotYetDue.Count > 0)
                        {
                            AdvanceCursorTo(slotsForDay, ref currentSlotIndex, ref currentSlotUsed, readyButNotYetDue.Min());
                            continue;
                        }

                        // Defensive fallback: nothing is dependency-ready at all (e.g. an undetected
                        // same-day dependency cycle). Fall back to the highest-priority remaining task
                        // rather than stalling forever.
                        candidate = remainingForDay.First();
                    }

                    PlaceTaskChunks(candidate, slotsForDay, ref currentSlotIndex, ref currentSlotUsed);

                    remainingForDay.Remove(candidate);
                    placedIdsToday.Add(candidate.Id);
                }

                // Any tasks left unplaced ran out of daily capacity before a valid slot for them arrived.
                // This can happen due to floating-point inaccuracies or if the distribution stage's
                // capacity calculation didn't perfectly align with the available slots.
                foreach (var unplaced in remainingForDay)
                {
                    var scheduledDuration = TimeSpan.FromTicks(unplaced.ScheduledParts.Sum(c => c.Duration.Ticks));
                    if (unplaced.EstimatedDuration - scheduledDuration > TimeSpan.FromMinutes(1))
                    {
                        context.History.Add($"  -> Warning: Task '{unplaced.Title}' could not fully fit on {date.ToShortDateString()} during sequencing.");
                    }
                }
            }

            // After sequencing, flatten the daily buckets back into a single list, ordered by their actual start time.
            var finalOrderedList = dailyBuckets.Values
                .SelectMany(list => list.OrderBy(t => t.ScheduledParts.FirstOrDefault()?.StartTime ?? DateTime.MaxValue))
                .ToList();

            context.SharedState["Tasks"] = finalOrderedList;
            context.History.Add("  -> Sequencing complete. Timestamps assigned.");

            return context;
        }

        /// <summary>
        /// Determines whether a task's same-day prerequisites (if any) have already been placed today.
        /// A dependency id that does not belong to today's bucket (e.g. it was satisfied on a prior day,
        /// per <see cref="TaskDistributionStage"/>'s dependency gate) is treated as already satisfied.
        /// </summary>
        private static bool IsDependencyReadyForToday(TaskItem task, HashSet<Guid> idsToday, HashSet<Guid> placedIdsToday)
        {
            return task.Dependencies == null || task.Dependencies.Count == 0 ||
                task.Dependencies.All(depId => depId == task.Id || !idsToday.Contains(depId) || placedIdsToday.Contains(depId));
        }

        /// <summary>
        /// Advances the shared slot cursor forward (skipping or partially consuming slots as needed)
        /// until it reaches <paramref name="targetTime"/>, without assigning any chunks. Used to jump
        /// past a gap that no currently-ready task can fill.
        /// </summary>
        private static void AdvanceCursorTo(List<TimeSlot> slotsForDay, ref int currentSlotIndex, ref TimeSpan currentSlotUsed, DateTime targetTime)
        {
            while (currentSlotIndex < slotsForDay.Count)
            {
                var slot = slotsForDay[currentSlotIndex];
                var slotStart = slot.StartTime + currentSlotUsed;
                if (slotStart >= targetTime)
                {
                    return;
                }

                var slotEnd = slot.StartTime + slot.Duration;
                if (slotEnd <= targetTime)
                {
                    // The rest of this slot falls entirely before the target time; skip it.
                    currentSlotIndex++;
                    currentSlotUsed = TimeSpan.Zero;
                }
                else
                {
                    // The target time falls within this slot; jump the cursor to it.
                    currentSlotUsed = targetTime - slot.StartTime;
                    return;
                }
            }
        }

        /// <summary>
        /// Assigns as many scheduled chunks as will fit for <paramref name="task"/> starting from the
        /// current slot cursor, splitting across subsequent slots as needed, and advances the cursor.
        /// Leaves any unassigned remainder of the task's duration unscheduled (caller reports it).
        /// </summary>
        private static void PlaceTaskChunks(TaskItem task, List<TimeSlot> slotsForDay, ref int currentSlotIndex, ref TimeSpan currentSlotUsed)
        {
            TimeSpan remainingTaskDuration = task.EstimatedDuration - TimeSpan.FromTicks(task.ScheduledParts.Sum(c => c.Duration.Ticks));

            while (remainingTaskDuration > TimeSpan.Zero && currentSlotIndex < slotsForDay.Count)
            {
                var slot = slotsForDay[currentSlotIndex];
                var availableSlotDuration = slot.Duration - currentSlotUsed;
                var slotStartTime = slot.StartTime + currentSlotUsed;

                // If the current slot is already full, move to the next one.
                if (availableSlotDuration <= TimeSpan.Zero)
                {
                    currentSlotIndex++;
                    currentSlotUsed = TimeSpan.Zero;
                    continue;
                }

                // The chunk to be scheduled is the smaller of the remaining task duration or the available slot space.
                var chunkDuration = (remainingTaskDuration < availableSlotDuration) ? remainingTaskDuration : availableSlotDuration;

                var chunk = new ScheduledChunk
                {
                    StartTime = slotStartTime,
                    EndTime = slotStartTime + chunkDuration
                };
                task.ScheduledParts.Add(chunk);

                // Update counters for the current task and slot.
                remainingTaskDuration -= chunkDuration;
                currentSlotUsed += chunkDuration;

                // If the current slot is now full, advance to the next slot.
                if (currentSlotUsed >= slot.Duration)
                {
                    currentSlotIndex++;
                    currentSlotUsed = TimeSpan.Zero;
                }
            }
        }

        /// <summary>
        /// Orders a single day's tasks so that a task is never sequenced before a same-day
        /// prerequisite, while otherwise preserving the existing "Eat the Frog" priority
        /// (due-today safety, then complexity, then importance) among tasks that are equally
        /// ready to be placed. Uses a priority-guided topological sort (Kahn's algorithm).
        /// </summary>
        /// <param name="tasksForDay">The tasks/fragments assigned to this day by <see cref="TaskDistributionStage"/>.</param>
        /// <param name="date">The day being sequenced, used to evaluate due-today safety.</param>
        /// <returns>The tasks for the day, ordered to respect same-day dependency order.</returns>
        private static List<TaskItem> BuildDependencySafeSequence(List<TaskItem> tasksForDay, DateTime date)
        {
            // Ids present in today's bucket. A dependency pointing outside this set belongs to a
            // prior day and is already guaranteed complete by TaskDistributionStage's dependency gate.
            var idsToday = tasksForDay.Select(t => t.Id).ToHashSet();

            var remaining = new List<TaskItem>(tasksForDay);
            var placedIdsToday = new HashSet<Guid>();
            var result = new List<TaskItem>(tasksForDay.Count);

            while (remaining.Count > 0)
            {
                // A task is "ready" if every same-day prerequisite it depends on has already been placed.
                var ready = remaining
                    .Where(t => t.Dependencies == null || t.Dependencies.Count == 0 ||
                                t.Dependencies.All(depId => depId == t.Id || !idsToday.Contains(depId) || placedIdsToday.Contains(depId)))
                    .ToList();

                // Defensive fallback: if nothing is "ready" (e.g. an undetected same-day dependency
                // cycle), fall back to the full remaining set rather than looping forever.
                if (ready.Count == 0)
                {
                    ready = remaining;
                }

                var next = ready
                    .OrderByDescending(t => t.DueDate.HasValue && t.DueDate.Value.Date <= date.Date)
                    .ThenByDescending(t => t.Complexity)
                    .ThenByDescending(t => t.EffectiveImportance)
                    .First();

                result.Add(next);
                placedIdsToday.Add(next.Id);
                remaining.Remove(next);
            }

            return result;
        }
    }
}
