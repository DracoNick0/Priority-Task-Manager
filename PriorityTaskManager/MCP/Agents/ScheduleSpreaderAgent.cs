using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for spreading work across days using the "Gold Panning" algorithm.
    /// It balances Daily Capacity ("Water Pressure") by displacing low-weight tasks ("Silt")
    /// to future dates, while keeping high-weight tasks ("Gold") in their ideal slots.
    /// </summary>
    public class ScheduleSpreaderAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 4: Spreading tasks (Gold Panning Algorithm)...");
            Console.WriteLine("--- PHASE 4: SPREADER (PANNING) ---");

            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks || tasks.Count == 0)
            {
                Console.WriteLine("  -> No Tasks to spread.");
                return context;
            }

            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var scheduleWindowObj) || scheduleWindowObj is not ScheduleWindow scheduleWindow)
            {
                 Console.WriteLine("  -> No Schedule Window found.");
                return context;
            }

            var weights = context.SharedState.TryGetValue("TaskWeights", out var weightsObj) 
                ? weightsObj as Dictionary<int, double> 
                : new Dictionary<int, double>();

            Console.WriteLine($"  -> Spreading {tasks.Count} tasks over window.");

            // --- Step 1: The Dump ---
            // Group tasks by their "Ideal Date".
            // For now, Ideal Date is effectively "First Available Slot" (Today).
            // In future, this could be 'DependenciesCompletedDate'.
            var buckets = new Dictionary<DateTime, List<TaskItem>>();
            
            // Get all unique days in window
            var windowDays = scheduleWindow.AvailableSlots
                .Select(s => s.StartTime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (windowDays.Count == 0) 
            {
                 Console.WriteLine("  -> No Available Days in Window.");
                 return context; // No window
            }

            // Initial Pour: Everything goes into the first bucket (Day 1)
            // or if it has a specific constraint like `LatestPossibleStartDate`, it might affect this,
            // but for Gold Panning, we dump upstream and let it wash down.
            buckets[windowDays[0]] = new List<TaskItem>(tasks);
            Console.WriteLine($"  -> Dumped all {tasks.Count} tasks into Paydirt (Day 1: {windowDays[0].ToShortDateString()}).");

            // Initialize other buckets
            foreach (var day in windowDays.Skip(1))
            {
                buckets[day] = new List<TaskItem>();
            }

            // --- Step 2: The Sift Loop ---
            // Iterate through the river of time
            for (int i = 0; i < windowDays.Count; i++)
            {
                var currentDay = windowDays[i];
                var nextDay = (i + 1 < windowDays.Count) ? windowDays[i + 1] : (DateTime?)null;
                
                var dailyTasks = buckets[currentDay];
                
                // Calculate Capacity (Water Pressure)
                double dayCapacity = scheduleWindow.AvailableSlots
                    .Where(s => s.StartTime.Date == currentDay)
                    .Sum(s => s.Duration.TotalHours);

                double currentLoad = dailyTasks.Sum(t => t.EstimatedDuration.TotalHours);
                
                Console.WriteLine($"  -> Day {currentDay.ToShortDateString()}: Load {currentLoad:F1}h / Capacity {dayCapacity:F1}h. (Tasks: {dailyTasks.Count})");

                // While Pressure is too high (Overflow)
                while (currentLoad > dayCapacity && dailyTasks.Count > 0)
                {
                    // Find the lightest task (Silt) to displace
                    // Sort by Weight Ascending
                    var lightestTask = dailyTasks
                        .OrderBy(t => weights != null && weights.ContainsKey(t.Id) ? weights[t.Id] : 0)
                        .First();
                    
                    double weight = weights != null && weights.ContainsKey(lightestTask.Id) ? weights[lightestTask.Id] : 0;

                    // If we can't move it (e.g. End of Window), we have an Overflow problem.
                    if (nextDay == null)
                    {
                        context.History.Add($"WARNING: Schedule Sluice Overflow. Task '{lightestTask.Title}' pushed off the end of the calendar.");
                        Console.WriteLine($"    ! OVERFLOW: Dropping '{lightestTask.Title}' (Weight {weight:F1}) - End of Window.");
                        dailyTasks.Remove(lightestTask);
                        // Mark as unscheduled?
                        lightestTask.ScheduledParts.Clear(); 
                        break; 
                    }

                    // Wash it downstream
                    dailyTasks.Remove(lightestTask);
                    buckets[nextDay.Value].Add(lightestTask);
                    Console.WriteLine($"    -> Washing '{lightestTask.Title}' (Weight {weight:F1}) to {nextDay.Value.ToShortDateString()}");

                    // Recalculate Load
                    currentLoad = dailyTasks.Sum(t => t.EstimatedDuration.TotalHours);
                }
            }

            // --- Step 3: Commit to Shared State ---
            // At this point, `buckets` contains the final decision of WHICH Date each task belongs to.
            // We flatten this back into a list, but attached with metadata if needed.
            // For now, we update the `ScheduledParts` locally just to persist the DATE decision,
            // but the TIME decision is left for the next agent (Mosaic).
            
            // We can just leave the grouped tasks in SharedState for the Sequencing Agent.
            context.SharedState["DailyBuckets"] = buckets;
            
            // Also update the main list effectively (remove unscheduled ones)
            var allScheduled = buckets.Values.SelectMany(t => t).ToList();
            context.SharedState["Tasks"] = allScheduled;

            context.History.Add("  -> Gold Pan Complete. Tasks distributed across days.");

            return context;
        }
    }
}
