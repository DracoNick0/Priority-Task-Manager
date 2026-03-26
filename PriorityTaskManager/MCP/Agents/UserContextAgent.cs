using PriorityTaskManager.MCP;
using PriorityTaskManager.Models;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Services.Agents
{
    /// <summary>
    /// A simple agent responsible for sorting tasks based on a predefined strategy.
    /// This agent serves as a pre-processor for other agents that require tasks to be in a specific order.
    /// The current strategy is to sort by due date, then by complexity.
    /// </summary>
    public class UserContextAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 3: Prioritizing tasks by due date and complexity...");
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<TaskItem> tasks || tasks.Count == 0)
            {
                context.History.Add("  -> No tasks found to sort.");
                return context;
            }

            // Sort the list of tasks that the next agent will process.
            // Primary sort: DueDate ascending, to handle the most urgent tasks first.
            // Secondary sort: Complexity descending, to tackle more complex items earlier within a given day.
            var sortedTasks = tasks
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.Complexity)
                .ToList();

            context.SharedState["Tasks"] = sortedTasks;
            context.History.Add("  -> Task list sorted for subsequent agents.");
            return context;
        }
    }
}
