using PriorityTaskManager.MCP;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.Services.Agents
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
            // Primary sort: DueDate ascending, to handle most urgent tasks first.
            // Secondary sort: Complexity descending, to tackle more complex items earlier in a given day.
            var sortedTasks = tasks
                .OrderBy(t => t.DueDate)
                .ThenByDescending(t => t.Complexity)
                .ToList();

            context.SharedState["Tasks"] = sortedTasks;
            context.History.Add("  -> Task list sorted for scheduling agent.");
            return context;
        }
    }
}
