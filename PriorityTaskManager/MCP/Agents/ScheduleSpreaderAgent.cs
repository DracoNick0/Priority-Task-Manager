using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for distributing tasks across the available schedule horizon.
    /// This agent implements a "Constructive Fill" strategy, which is a form of greedy
    /// knapsack algorithm with item splitting. It iterates through each day, filling it
    /// with the highest-priority tasks. If a task is too large to fit in the remaining
    /// capacity of a day, it is split into two parts: one that fills the day, and a
    /// remainder that is carried over to be scheduled on subsequent days.
    /// </summary>
    public class ScheduleSpreaderAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 4: Spreading tasks (Constructive Fill)...");

            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks || tasks.Count == 0)
            {
                return context;
            }

            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var scheduleWindowObj) || scheduleWindowObj is not ScheduleWindow scheduleWindow)
            {
                return context;
            }

            var weights = context.SharedState.TryGetValue("TaskWeights", out var weightsObj) 
                ? weightsObj as Dictionary<int, double> 
                : new Dictionary<int, double>();

            // --- Step 1: Prepare Daily Buckets ---
            // Create a dictionary to hold the list of tasks scheduled for each day.
            var buckets = new Dictionary<DateTime, List<TaskItem>>();
            var windowDays = scheduleWindow.AvailableSlots
                .Select(s => s.StartTime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (windowDays.Count == 0) 
            {
                 return context;
            }

            // Initialize a list for each day in the scheduling window.
            foreach (var day in windowDays)
            {
                buckets[day] = new List<TaskItem>();
            }

            // --- Step 2: The Constructive Fill Loop ---
            // Sort tasks by their calculated weight to ensure high-priority items are scheduled first.
            var remainingTasks = tasks.OrderByDescending(t => weights != null && weights.ContainsKey(t.Id) ? weights[t.Id] : 0).ToList();
            
            // Iterate through each day in the scheduling window.
            for (int i = 0; i < windowDays.Count; i++)
            {
                var currentDay = windowDays[i];
                var dailyBucket = new List<TaskItem>();
                
                // Calculate the total available work time (capacity) for the current day.
                double dayCapacity = scheduleWindow.AvailableSlots
                    .Where(s => s.StartTime.Date == currentDay)
                    .Sum(s => s.Duration.TotalHours);

                double currentLoad = 0;

                // Iterate through the prioritized list of remaining tasks.
                for (int j = 0; j < remainingTasks.Count; j++)
                {
                    var task = remainingTasks[j];
                    double taskDuration = task.EstimatedDuration.TotalHours;
                    double availableSpace = dayCapacity - currentLoad;

                    // If the day is full, stop trying to add more tasks.
                    if (availableSpace <= 0.01) // Using a small tolerance for floating point inaccuracies.
                    {
                        break;
                    }
                    
                    // If the task fits completely in the remaining space, add it.
                    if (taskDuration <= availableSpace)
                    {
                        dailyBucket.Add(task);
                        currentLoad += taskDuration;
                        remainingTasks.RemoveAt(j);
                        j--; // Adjust index as the list size has changed.
                    }
                    else
                    {
                        // If the task is too big, split it if the remaining space is meaningful.
                        if (availableSpace > 0.25) // e.g., Don't create a 5-minute sub-task.
                        {
                            
                            // Create the part that fits in today's remaining time.
                            var part1 = task.Clone();
                            part1.EstimatedDuration = TimeSpan.FromHours(availableSpace);
                            
                            // Create the remainder part to be scheduled later.
                            var part2 = task.Clone();
                            part2.EstimatedDuration = TimeSpan.FromHours(taskDuration - availableSpace);
                            
                            // Add Part 1 to today's schedule.
                            dailyBucket.Add(part1);
                            currentLoad += availableSpace;
                            
                            // Replace the original task in the list with the remainder.
                            remainingTasks.RemoveAt(j);
                            remainingTasks.Insert(j, part2);
                            
                            // The day is now full, so break to move to the next day.
                            break;
                        }
                        // If the remaining space is too small, skip this task and try to find
                        // a smaller one that might fit.
                    }
                }
                
                buckets[currentDay] = dailyBucket;

                // If all tasks have been scheduled, exit the loop.
                if (remainingTasks.Count == 0)
                {
                    break;
                }
            }

            // --- Step 3: Handling Leftovers ---
            // If any tasks remain after the loop, they could not fit in the schedule.
            // As a fallback, add them to the last day, which may cause over-scheduling.
            if (remainingTasks.Count > 0)
            {
                var lastDay = windowDays.Last();
                buckets[lastDay].AddRange(remainingTasks);
            }

            // --- Step 4: Commit to Shared State ---
            // The 'DailyBuckets' are passed to the next agent (DaySequencingAgent) for fine-grained scheduling.
            context.SharedState["DailyBuckets"] = buckets;
            
            // Update the main 'Tasks' list to reflect the new reality of split and scheduled tasks.
            var allScheduled = buckets.Values.SelectMany(t => t).ToList();
            context.SharedState["Tasks"] = allScheduled;

            context.History.Add("  -> Gold Pan Complete. Tasks distributed across days.");

            return context;
        }
    }
}
