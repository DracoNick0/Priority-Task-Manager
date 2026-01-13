using PriorityTaskManager.MCP;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.MCP.Agents
{
    public class PrioritizationAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 3: Prioritizing tasks by due date and complexity...");
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks || tasks.Count == 0)
            {
                return context;
            }

            // Sort the list of tasks that the SchedulingAgent will process.
            // Primary sort: DueDate ascending, with nulls (no due date) treated as the latest possible date.
            // Secondary sort: Complexity descending, to tackle more complex items earlier in a given day.
            var sortedTasks = tasks
                .OrderBy(t => t.DueDate.HasValue ? 0 : 1) // Ensures tasks with due dates come before those without.
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.Complexity)
                .ToList();

            context.SharedState["Tasks"] = sortedTasks;
            context.History.Add("  -> Task list sorted for scheduling agent.");

            return context;
        }
    }
}
