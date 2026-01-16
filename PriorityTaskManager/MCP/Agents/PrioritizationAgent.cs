using PriorityTaskManager.MCP;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    public class PrioritizationAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 3: Calculating Task Weights (Gold Panning)...");
            Console.WriteLine("--- PHASE 3: PRIORITIZATION (WEIGHING) ---");
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks || tasks.Count == 0)
            {
                Console.WriteLine("  -> No Tasks found in context.");
                return context;
            }

            var weights = new Dictionary<int, double>();
            var today = DateTime.Today; // Assuming local time for simple calculation, preferably use TimeService in future refactor

            // Provide access to TimeService if available for more accurate 'Today'
            if (context.SharedState.TryGetValue("TimeService", out var tsObj) && tsObj is Services.ITimeService timeService)
            {
                today = timeService.GetCurrentTime().Date;
            }

            Console.WriteLine($"  -> Calculating weights for {tasks.Count} tasks relative to {today.ToShortDateString()}.");

            foreach (var task in tasks)
            {
                double weight = CalculateGoldPanningWeight(task, today);
                weights[task.Id] = weight;
                Console.WriteLine($"    Task [{task.Id}] '{task.Title}': Weight = {weight:F2} (Due: {task.DueDate?.ToShortDateString() ?? "None"}, Imp: {task.Importance})");
            }

            // Store weights for the Spreader Agent
            context.SharedState["TaskWeights"] = weights;

            // Optional: Still return a roughly sorted list for UI display if needed before scheduling
            // But the Spreader will use the Weights, not this list order.
            var sortedTasks = tasks.OrderByDescending(t => weights[t.Id]).ToList();
            context.SharedState["Tasks"] = sortedTasks;
            
            context.History.Add($"  -> Calculated weights for {tasks.Count} tasks.");

            return context;
        }

        private double CalculateGoldPanningWeight(Models.TaskItem task, DateTime today)
        {
            // 1. Urgency Mass (Gold)
            // Formula: 100 / (DaysUntilDeadline + 1)^2
            double urgencyMass = 0;
            if (task.DueDate.HasValue)
            {
                double daysUntil = (task.DueDate.Value.Date - today).TotalDays;
                if (daysUntil < 0) daysUntil = 0; // Overdue is essentially 0 days (Max Urgency)
                
                urgencyMass = 100.0 / Math.Pow(daysUntil + 1, 2);
            }

            // 2. Importance Mass (Rock Size)
            // Logic: Weight = Importance * 5 (Scale 5-50)
            double importanceMass = (task.Importance) * 5.0;

            // 3. Relative Density (Silt vs Sand)
            // If no Due Date, it's Silt. 
            // We give it a base density multiplier.
            double densityMultiplier = 1.0;
            if (!task.DueDate.HasValue)
            {
                // Backlog items are lighter (0.5), meaning they wash away easier than Due items.
                // However, they still have Importance Mass, so a "High Importance Backlog" (50 * 0.5 = 25)
                // might beat a "Low Importance Due in 30 Days" (Urgency < 1 + Importance 5 = 6).
                densityMultiplier = 0.5;

                // User Overrides could go here (e.g. if User marked "Pinned", density = Infinity)
            }
            
            if (task.IsPinned)
            {
                return double.MaxValue; // Unmovable Object
            }

            return (urgencyMass + importanceMass) * densityMultiplier;
        }
    }
}
