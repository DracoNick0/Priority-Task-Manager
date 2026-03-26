using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for balancing the workload across available days.
    /// It attempts to fit tasks into days based on their complexity (load) and duration.
    /// If a task cannot fit, it enters a logic flow to split the task across multiple days.
    /// </summary>
    public class ComplexityBalancerAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks)
            {
                context.History.Add("ComplexityBalancerAgent: No valid task list found in context.");
                return context;
            }

            if (!context.SharedState.TryGetValue("TimeService", out var timeServiceObj) || timeServiceObj is not PriorityTaskManager.Services.ITimeService timeService)
            {
                context.History.Add("ComplexityBalancerAgent: No ITimeService found in context.");
                return context;
            }
            var now = timeService.GetCurrentTime();

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

            // Map available capacity
            var days = slots.Select(s => s.StartTime.Date).Distinct().OrderBy(d => d).ToList();
            var hoursPerDay = days.ToDictionary(
                d => d,
                d => slots.Where(s => s.StartTime.Date == d).Sum(s => s.Duration.TotalHours)
            );
            var initialCapacity = new Dictionary<DateTime, double>(hoursPerDay);

            var totalAvailableHours = hoursPerDay.Values.Sum();

            // Total Load Calculation: Complexity (Intensity) * Duration = Load
            var totalLoad = tasks.Sum(t => t.Complexity * t.EstimatedDuration.TotalHours);
            var targetAverageIntensity = totalAvailableHours > 0 ? totalLoad / totalAvailableHours : 0;

            var loadByDay = days.ToDictionary(d => d, d => 0.0);
            var scheduledTasks = new List<TaskItem>();

            // Sorting Strategy:
            // 1. Pinned tasks (Must be scheduled first)
            // 2. High Load (Hardest/Longest tasks)
            // 3. Due Date (Earliest due)
            var orderedTasks = tasks.OrderByDescending(t => t.IsPinned)
                                    .ThenByDescending(t => t.Complexity * t.EstimatedDuration.TotalHours)
                                    .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                                    .ToList();

            // Default fallback if no due date is present is the end of the scheduling window
            var windowEnd = days.Last();

            foreach (var task in orderedTasks)
            {
                var due = task.DueDate ?? windowEnd;
                var availableDays = days.Where(d => d <= due).ToList();
                var hoursNeeded = task.EstimatedDuration.TotalHours;
                var taskLoad = task.Complexity * hoursNeeded;

                // Try to fit the *entire* task into a single day first
                if (!task.IsDivisible || hoursNeeded <= hoursPerDay.Values.Max())
                {
                    // Find the day with the lowest current Average Intensity to balance the load.
                    var day = availableDays
                        .Where(d => hoursPerDay[d] >= hoursNeeded)
                        .OrderBy(d => loadByDay[d] / (initialCapacity[d] > 0 ? initialCapacity[d] : 1))
                        .ThenBy(d => d)
                        .FirstOrDefault();

                    if (day == default)
                    {
                        if (task.IsDivisible) 
                        {
                            goto SplitLogic;
                        }

                        if (task.IsPinned)
                        {
                            context.History.Add($"ComplexityBalancerAgent: Could not schedule Pinned Task '{task.Title}'. No day has enough capacity.");
                            continue;
                        }
                        else
                        {
                            context.History.Add($"ComplexityBalancerAgent: Could not schedule '{task.Title}' before due date.");
                            continue;
                        }
                    }

                    // Schedule whole task
                    var slotStart = slots.First(s => s.StartTime.Date == day).StartTime; // Simplified start time assignment
                    
                    var chunk = new ScheduledChunk
                    {
                        StartTime = slotStart,
                        EndTime = slotStart.AddHours(hoursNeeded)
                    };
                    task.ScheduledParts = new List<ScheduledChunk> { chunk };
                    hoursPerDay[day] -= hoursNeeded;
                    loadByDay[day] += taskLoad;
                    scheduledTasks.Add(task);
                    continue;
                }

                SplitLogic:
                // Split Logic: Distribute chunks to days with lowest density
                 {
                    var hoursLeft = hoursNeeded;
                    var chunks = new List<ScheduledChunk>();
                    
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

                    if (hoursLeft > 0.001 && !task.IsPinned)
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
