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

            // --- Step 2: The Constructive Fill Loop (Greedy Knapsack with Splitting) ---
            // We actively select tasks to fill each day's capacity.
            // If a task causes overflow, we split it to fill the remaining space.

            // Sort by Priority (Weights)
            var remainingTasks = tasks.OrderByDescending(t => weights != null && weights.ContainsKey(t.Id) ? weights[t.Id] : 0).ToList();
            
            for (int i = 0; i < windowDays.Count; i++)
            {
                var currentDay = windowDays[i];
                var dailyBucket = new List<TaskItem>();
                
                // Calculate Capacity (Water Pressure)
                double dayCapacity = scheduleWindow.AvailableSlots
                    .Where(s => s.StartTime.Date == currentDay)
                    .Sum(s => s.Duration.TotalHours);

                double currentLoad = 0;
                
                Console.WriteLine($"  -> Filling Day {currentDay.ToShortDateString()} (Capacity {dayCapacity:F1}h)...");

                // Iterate backwards to allow removal
                for (int j = remainingTasks.Count - 1; j >= 0; j--)
                {
                    var task = remainingTasks[j];
                    double taskDuration = task.EstimatedDuration.TotalHours;
                    double availableSpace = dayCapacity - currentLoad;

                    if (availableSpace <= 0.01) // Day is full
                    {
                        break;
                    }
                    
                    if (taskDuration <= availableSpace)
                    {
                        // It fits! Add it.
                        dailyBucket.Add(task);
                        currentLoad += taskDuration;
                        remainingTasks.RemoveAt(j);
                        Console.WriteLine($"    -> Added '{task.Title}' ({taskDuration:F1}h)");
                    }
                    else
                    {
                        // It doesn't fit entirely. Can we split it?
                        // Only split if the chunk is meaningful (e.g. > 15 mins)
                        if (availableSpace > 0.25) 
                        {
                            Console.WriteLine($"    -> Splitting '{task.Title}' to fill {availableSpace:F1}h gap.");
                            
                            // Create the part that stays (Part 1)
                            // We modify the current task instance (which is a clone from the Strategy)
                            var part1 = task.Clone(); 
                            // Create the remainder (Part 2) BEFORE modifying Part 1
                            var part2 = task.Clone();
                            
                            // Adjust durations
                            part1.EstimatedDuration = TimeSpan.FromHours(availableSpace);
                            part2.EstimatedDuration = TimeSpan.FromHours(taskDuration - availableSpace);
                            
                            // Add Part 1 to today
                            dailyBucket.Add(part1);
                            currentLoad += availableSpace;
                            
                            // Replace original in remaining list with Part 2
                            remainingTasks.RemoveAt(j);
                            remainingTasks.Insert(j, part2);
                            
                            // Since day is full, break inner loop to move to next day
                            break;
                        }
                        else
                        {
                            // Gap is too small to split a task into. Skip this task for today.
                            // Continue searching for a smaller task that might fit?
                            // For now, we just skip it.
                        }
                    }
                }
                
                buckets[currentDay] = dailyBucket;

                if (remainingTasks.Count == 0)
                {
                    Console.WriteLine("  -> All tasks scheduled.");
                    break;
                }
            }

            // --- Step 3: Handling Leftovers ---
            if (remainingTasks.Count > 0)
            {
                var lastDay = windowDays.Last();
                buckets[lastDay].AddRange(remainingTasks);
                Console.WriteLine($"  -> Warning: {remainingTasks.Count} tasks did not fit in window. Pushed to {lastDay.ToShortDateString()} (Overfill).");
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
