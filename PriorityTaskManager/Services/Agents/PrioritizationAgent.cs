using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    public class PrioritizationAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("PrioritizationAgent execution started.");

            // Retrieve tasks and available time from context
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks)
                return context;
            if (!context.SharedState.TryGetValue("TotalAvailableTime", out var totalTimeObj) || totalTimeObj is not TimeSpan totalAvailableTime)
                return context;
            if (tasks == null || tasks.Count == 0 || totalAvailableTime <= TimeSpan.Zero)
                return context;

            var scheduledTasks = new List<Models.TaskItem>();
            var timeUsed = TimeSpan.Zero;
            var orderedTasks = tasks.OrderBy(t => t.DueDate).ToList();
            foreach (var task in orderedTasks)
            {
                if (timeUsed + task.EstimatedDuration <= totalAvailableTime)
                {
                    scheduledTasks.Add(task);
                    timeUsed += task.EstimatedDuration;
                }
            }
            context.SharedState["Tasks"] = scheduledTasks;
            return context;
        }
    }
}
