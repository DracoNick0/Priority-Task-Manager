using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for sequencing tasks within a specific day (The "Mosaic").
    /// Uses a "Front-Loading" strategy ("Eat the Frog") to schedule high complexity tasks
    /// earlier in the day when energy is high.
    /// </summary>
    public class DaySequencingAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 5: Sequencing tasks within days (Mosaic/Front-Loading)...");
            Console.WriteLine("--- PHASE 5: SEQUENCING (MOSAIC) ---");

            if (!context.SharedState.TryGetValue("DailyBuckets", out var bucketsObj) || 
                bucketsObj is not Dictionary<DateTime, List<TaskItem>> dailyBuckets)
            {
                // Fallback: If Spreader didn't run or produce buckets, we can't sequence.
                context.History.Add("  -> No daily buckets found. Skipping sequencing.");
                Console.WriteLine("  -> No Buckets Found.");
                return context;
            }

            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var scheduleWindowObj) || 
                scheduleWindowObj is not ScheduleWindow scheduleWindow)
            {
                return context;
            }

            // Iterate through each day that has work
            foreach (var date in dailyBuckets.Keys.OrderBy(d => d))
            {
                var tasksForDay = dailyBuckets[date];
                if (tasksForDay.Count == 0) continue;

                Console.WriteLine($"  -> Sequencing {date.ToShortDateString()} ({tasksForDay.Count} tasks)...");

                // Sort tasks for the day: High Complexity -> Low Complexity
                var sequence = tasksForDay
                    .OrderByDescending(t => t.Complexity)
                    .ThenByDescending(t => t.EffectiveImportance) // Tiebreaker: Importance
                    .ToList();
                
                Console.WriteLine($"    Order: {string.Join(" -> ", sequence.Select(t => $"{t.Title}({t.Complexity})"))}");

                // Find the TimeSlots for this specific date
                var slotsForDay = scheduleWindow.AvailableSlots
                    .Where(s => s.StartTime.Date == date)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                if (slotsForDay.Count == 0)
                {
                    context.History.Add($"  -> Warning: Work assigned to {date.ToShortDateString()} but no slots available.");
                    Console.WriteLine($"    ! Warning: Tasks assigned but no time slots found on this day.");
                    continue;
                }

                // Simple Sequencer: Fill slots linearly
                // NOTE: This assumes slots are contiguous or we just jump gaps.
                
                int currentSlotIndex = 0;
                TimeSpan currentSlotUsed = TimeSpan.Zero;
                
                foreach (var task in sequence)
                {
                    TimeSpan remainingTaskDuration = task.EstimatedDuration;
                    task.ScheduledParts.Clear(); // Clear old scheduling

                    while (remainingTaskDuration > TimeSpan.Zero && currentSlotIndex < slotsForDay.Count)
                    {
                        var slot = slotsForDay[currentSlotIndex];
                        var slotDuration = slot.Duration - currentSlotUsed;
                        var slotStartTime = slot.StartTime + currentSlotUsed;

                        if (slotDuration <= TimeSpan.Zero)
                        {
                            currentSlotIndex++;
                            currentSlotUsed = TimeSpan.Zero;
                            continue;
                        }

                        // Determine chunk size
                        var chunkDuration = (remainingTaskDuration < slotDuration) ? remainingTaskDuration : slotDuration;

                        var chunk = new ScheduledChunk
                        {
                            StartTime = slotStartTime,
                            EndTime = slotStartTime + chunkDuration
                        };
                        task.ScheduledParts.Add(chunk);
                        Console.WriteLine($"      [{chunk.StartTime:HH:mm}-{chunk.EndTime:HH:mm}] {task.Title}");

                        // Update counters
                        remainingTaskDuration -= chunkDuration;
                        currentSlotUsed += chunkDuration;

                        // If slot is full, move to next
                        if (currentSlotUsed >= slot.Duration)
                        {
                            currentSlotIndex++;
                            currentSlotUsed = TimeSpan.Zero;
                        }
                    }

                    if (remainingTaskDuration > TimeSpan.FromMinutes(1)) // Using distinct tolerance to avoid float errors
                    {
                        // Needs to push to next day? Or just mark incomplete?
                        // Spreader *should* have guaranteed capacity, but floating point math might cause tiny overflows.
                        context.History.Add($"  -> Warning: Task {task.Title} could not fully fit on {date.ToShortDateString()} during sequencing.");
                         Console.WriteLine($"      ! ERROR: Could not schedule remaining {remainingTaskDuration.TotalMinutes}m of {task.Title}");
                    }
                }
            }

            // Update main list order to reflect the new schedule
            var finalOrderedList = dailyBuckets.Values
                .SelectMany(list => list.OrderBy(t => t.ScheduledParts.FirstOrDefault()?.StartTime ?? DateTime.MaxValue))
                .ToList();

            context.SharedState["Tasks"] = finalOrderedList;
            context.History.Add("  -> Sequencing complete. Timestamps assigned.");

            return context;
        }
    }
}
