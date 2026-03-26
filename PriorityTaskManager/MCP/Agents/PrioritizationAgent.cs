using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    /// <summary>
    /// Agent responsible for the "Weighing" phase of the Gold Panning Strategy.
    /// It calculates a "weight" for each task, representing its scheduling priority. This weight
    /// is a combination of urgency (proximity to due date) and user-defined importance.
    /// High-weight tasks are considered "Gold" (heavy, sinks to the top), while low-weight
    /// tasks are "Silt" (light, easily washed downstream to later dates).
    /// </summary>
    public class PrioritizationAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 3: Calculating Task Weights (Gold Panning)...");
            Console.WriteLine("--- PHASE 3: PRIORITIZATION (WEIGHING) ---");
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks || tasks.Count == 0)
            {
                Console.WriteLine("  -> No Tasks found in context.");
                return context;
            }

            var weights = new Dictionary<int, double>();
            
            // Use TimeService to get the current date for accurate urgency calculations.
            DateTime today;
            if (context.SharedState.TryGetValue("TimeService", out var tsObj) && tsObj is Services.ITimeService timeService)
            {
                today = timeService.GetCurrentTime().Date;
            }
            else
            {
                today = DateTime.Today;
            }

            Console.WriteLine($"  -> Calculating weights for {tasks.Count} tasks relative to {today.ToShortDateString()}.");

            foreach (var task in tasks)
            {
                double weight = CalculateGoldPanningWeight(task, today);
                weights[task.Id] = weight;
                Console.WriteLine($"    Task [{task.Id}] '{task.Title}': Weight = {weight:F2} (Due: {task.DueDate?.ToShortDateString() ?? "None"}, Imp: {task.Importance})");
            }

            // Store the calculated weights in the shared context for the Spreader Agent to use.
            context.SharedState["TaskWeights"] = weights;

            // The Spreader Agent relies on the 'TaskWeights' dictionary for its logic.
            // This sorting is for potential display purposes or for agents that might run
            // between this one and the spreader.
            var sortedTasks = tasks.OrderByDescending(t => weights[t.Id]).ToList();
            context.SharedState["Tasks"] = sortedTasks;
            
            context.History.Add($"  -> Calculated weights for {tasks.Count} tasks.");

            return context;
        }

        /// <summary>
        /// Calculates the "Gold Panning" weight of a task.
        /// </summary>
        /// <param name="task">The task to weigh.</param>
        /// <param name="today">The current date for urgency calculation.</param>
        /// <returns>A double representing the task's scheduling weight.</returns>
        private double CalculateGoldPanningWeight(TaskItem task, DateTime today)
        {
            // Pinned tasks are "Unmovable Boulders" and get maximum priority.
            if (task.IsPinned)
            {
                return double.MaxValue;
            }

            // 1. Urgency Mass (The "Gold"): How close is the deadline?
            // The formula creates an exponential curve, making tasks due very soon significantly
            // heavier than tasks due far in the future.
            double urgencyMass = 0;
            if (task.DueDate.HasValue)
            {
                double daysUntil = (task.DueDate.Value.Date - today).TotalDays;
                if (daysUntil < 0) daysUntil = 0;
                
                urgencyMass = 100.0 / Math.Pow(daysUntil + 1, 2);
            }

            // 2. Importance Mass (The "Rock Size"): How important did the user say it was?
            // This provides a baseline weight. A high-importance task is a "big rock"
            // and has significant weight even if its deadline is far away.
            double importanceMass = (task.Importance) * 5.0;

            // 3. Relative Density (Silt vs. Sand): Is this an active task or a backlog item?
            // Tasks without a due date are considered "Silt". They are lighter and more easily
            // displaced by tasks with concrete deadlines ("Sand" and "Rocks").
            double densityMultiplier = 1.0;
            if (!task.DueDate.HasValue)
            {
                // This multiplier reduces the final weight, making backlog items secondary
                // to items with due dates, assuming similar importance.
                densityMultiplier = 0.5;
            }
            
            // Final Weight = (Urgency + Importance) * Density
            return (urgencyMass + importanceMass) * densityMultiplier;
        }
    }
}
