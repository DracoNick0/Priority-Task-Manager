using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for the final sequencing of tasks within each day. This is the "Mosaic"
    /// phase, where the individual tasks (stones) from each daily bucket are arranged into the
    /// available time slots (the mosaic grid) for that day. It uses a "Front-Loading" or
    /// "Eat the Frog" strategy, scheduling the most complex tasks earlier in the day.
    /// </summary>
    public class DaySequencingAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 5: Sequencing tasks within days (Mosaic/Front-Loading)...");

            // Retrieve the daily buckets created by the ScheduleSpreaderAgent.
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
                // 1. Urgency: Tasks that are due on or before this day are prioritized to ensure they are completed in time.
                // 2. Complexity ("Eat the Frog"): High-complexity tasks are scheduled first, when energy levels are typically highest.
                // 3. Importance: If urgency and complexity are equal, the more important task goes first.
                var sequence = tasksForDay
                    .OrderByDescending(t => t.DueDate.HasValue && t.DueDate.Value.Date <= date.Date) // Critical for this day
                    .ThenByDescending(t => t.Complexity)
                    .ThenByDescending(t => t.EffectiveImportance) // Tiebreaker
                    .ToList();

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
                int currentSlotIndex = 0;
                TimeSpan currentSlotUsed = TimeSpan.Zero;
                
                foreach (var task in sequence)
                {
                    TimeSpan remainingTaskDuration = task.EstimatedDuration;
                    task.ScheduledParts.Clear(); // Clear any previous scheduling data before creating new chunks.

                    // Continue scheduling parts of the task until its full duration is accounted for.
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

                    // If a task has remaining duration, it means there wasn't enough capacity in the schedule.
                    // This can happen due to floating-point inaccuracies or if the SpreaderAgent's capacity calculation
                    // didn't perfectly align with the available slots.
                    if (remainingTaskDuration > TimeSpan.FromMinutes(1))
                    {
                        context.History.Add($"  -> Warning: Task '{task.Title}' could not fully fit on {date.ToShortDateString()} during sequencing.");
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
    }
}
