using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    public class ComplexityBalancerAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            // Get tasks from context
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
            {
                context.History.Add("ComplexityBalancerAgent: No valid task list found in context.");
                return context;
            }


            // Get ITimeService from context (required for all time logic)
            if (!context.SharedState.TryGetValue("TimeService", out var timeServiceObj) || timeServiceObj is not PriorityTaskManager.Services.ITimeService timeService)
            {
                context.History.Add("ComplexityBalancerAgent: No ITimeService found in context.");
                return context;
            }
            var now = timeService.GetCurrentTime();

            // Get available schedule windows from context
            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var windowObj) || windowObj is not ScheduleWindow scheduleWindow)
            {
                context.History.Add("ComplexityBalancerAgent: No available schedule window found in context.");
                return context;
            }
            var slots = scheduleWindow.AvailableSlots;
            if (slots.Count == 0)
            {
                context.History.Add("ComplexityBalancerAgent: No available time slots to schedule tasks.");
                return context;
            }

            // Group slots by day
            var days = slots.Select(s => s.StartTime.Date).Distinct().OrderBy(d => d).ToList();
            // Calculate available hours per day from slots
            var hoursPerDay = days.ToDictionary(
                d => d,
                d => slots.Where(s => s.StartTime.Date == d).Sum(s => s.Duration.TotalHours)
            );
            // Snapshot initial capacity for density calc
            var initialCapacity = new Dictionary<DateTime, double>(hoursPerDay);

            var totalAvailableHours = hoursPerDay.Values.Sum();

            // Calculate total load (Complexity * Duration)
            // Complexity (1-10) is treated as an Intensity Rate.
            // Load = Intensity * Hours.
            var totalLoad = tasks.Sum(t => t.Complexity * t.EstimatedDuration.TotalHours);
            var targetAverageIntensity = totalAvailableHours > 0 ? totalLoad / totalAvailableHours : 0;

            // Prepare per-day load tracker (Load = Complexity * Hours)
            var loadByDay = days.ToDictionary(d => d, d => 0.0);
            var scheduledTasks = new List<TaskItem>();

            // Sort: pinned first, then due date, then complexity (descending load)
            var orderedTasks = tasks.OrderByDescending(t => t.IsPinned)
                                    .ThenByDescending(t => t.Complexity * t.EstimatedDuration.TotalHours)
                                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                                    .ToList();

            // Determine the latest due date for fallback
            // FIX: Failing to define a window end means tasks are constrained by other tasks' due dates.
            // We should use the last available day in value window as the fallback.
            var windowEnd = days.Last();

            foreach (var task in orderedTasks)
            {
                var due = task.DueDate ?? windowEnd;
                var availableDays = days.Where(d => d <= due).ToList();
                var hoursNeeded = task.EstimatedDuration.TotalHours;
                
                // Load = Intensity (Complexity) * Duration
                var taskLoad = task.Complexity * hoursNeeded;

                // Try to fit task into available days
                if (!task.IsDivisible || hoursNeeded <= hoursPerDay.Values.Max())
                {
                    // Find day with lowest current Average Intensity
                    // Average Intensity = TotalLoad / InitialCapacity
                    var day = availableDays
                        .Where(d => hoursPerDay[d] >= hoursNeeded)
                        .OrderBy(d => loadByDay[d] / (initialCapacity[d] > 0 ? initialCapacity[d] : 1))
                        .ThenBy(d => d)
                        .FirstOrDefault();

                    if (day == default)
                    {
                        // If pinned, force schedule on earliest available (even if Overloaded? No, we stick to capacity)
                        // If it doesn't fit, we try to split?
                        if (task.IsDivisible) 
                        {
                            // Fallthrough to split logic
                            goto SplitLogic;
                        }

                        // If pinned and not divisible and effectively no room...
                        if (task.IsPinned)
                        {
                            // Desperate greedy fit? Or fail? 
                            // Current logic: Pinned forces earliest.
                            // But checked "Where ... >= hoursNeeded". If none, it means NO day fits.
                            context.History.Add($"ComplexityBalancerAgent: Could not schedule Pinned Task '{task.Title}'. No day has enough capacity.");
                            continue;
                        }
                        else
                        {
                            // Skip unschedulable unpinned task
                            context.History.Add($"ComplexityBalancerAgent: Could not schedule '{task.Title}' before due date.");
                            continue;
                        }
                    }
                    // Schedule whole task on that day
                    // Note: We don't assign specific Times here (StartTime), just the Day. 
                    // We assign the "Day Start" temporarily. The SchedulePreProcessor or User determines exact slot?
                    var slotStart = slots.First(s => s.StartTime.Date == day).StartTime; // Simplified
                    
                    var chunk = new ScheduledChunk
                    {
                        StartTime = slotStart, // Placeholder
                        EndTime = slotStart.AddHours(hoursNeeded)
                    };
                    task.ScheduledParts = new List<ScheduledChunk> { chunk };
                    hoursPerDay[day] -= hoursNeeded;
                    loadByDay[day] += taskLoad; // Accumulate Load
                    scheduledTasks.Add(task);
                    continue;
                }

                SplitLogic:
                // Split task across days
                // Logic: Distribute chunks to days with lowest density
                 {
                    var hoursLeft = hoursNeeded;
                    var chunks = new List<ScheduledChunk>();
                    
                    // We want to allocate chunks to best days.
                    // While hoursLeft > 0:
                    // Find best day with space. Take what we can.
                    
                    while (hoursLeft > 0)
                    {
                        var bestDay = availableDays
                            .Where(d => hoursPerDay[d] > 0)
                            .OrderBy(d => loadByDay[d] / (initialCapacity[d] > 0 ? initialCapacity[d] : 1))
                            .ThenBy(d => d)
                            .FirstOrDefault();

                        if (bestDay == default) break;

                        var allocatable = Math.Min(hoursPerDay[bestDay], hoursLeft);
                        var slotStart = slots.First(s => s.StartTime.Date == bestDay).StartTime;

                        var chunk = new ScheduledChunk
                        {
                            StartTime = slotStart,
                            EndTime = slotStart.AddHours(allocatable)
                        };
                        chunks.Add(chunk);
                        
                        var chunkLoad = task.Complexity * allocatable;
                        loadByDay[bestDay] += chunkLoad;
                        hoursPerDay[bestDay] -= allocatable;
                        hoursLeft -= allocatable;
                    }

                    if (hoursLeft > 0.001 && !task.IsPinned) // Tolerance
                    {
                         context.History.Add($"ComplexityBalancerAgent: Could not fully schedule '{task.Title}'. Remaining: {hoursLeft}h");
                    }
                    
                    if (chunks.Any())
                    {
                         task.ScheduledParts = chunks;
                         scheduledTasks.Add(task);
                    }
                }
            }

            context.SharedState["Tasks"] = scheduledTasks;
            context.History.Add("ComplexityBalancerAgent: Scheduling complete.");
            return context;
        }
    }
}
