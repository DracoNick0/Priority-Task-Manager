using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    public class UserContextAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("UserContextAgent execution started.");
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks || tasks.Count == 0)
            {
                return context;
            }

            // Group tasks by the date part of ScheduledStartTime
            var grouped = tasks
                .Where(t => t.ScheduledStartTime.HasValue)
                .GroupBy(t => t.ScheduledStartTime.Value.Date);

            var finalReorderedSchedule = new List<Models.TaskItem>();

            foreach (var dayGroup in grouped)
            {
                // Sort by complexity descending
                var sorted = dayGroup.OrderByDescending(t => t.Complexity).ToList();
                // Find earliest start time for the day
                var earliest = dayGroup.Min(t => t.ScheduledStartTime.Value);
                var current = earliest;
                foreach (var task in sorted)
                {
                    task.ScheduledStartTime = current;
                    task.ScheduledEndTime = current + task.EstimatedDuration;
                    current = task.ScheduledEndTime.Value;
                    finalReorderedSchedule.Add(task);
                }
            }

            // Add any tasks that did not have a ScheduledStartTime (not scheduled)
            var unscheduled = tasks.Where(t => !t.ScheduledStartTime.HasValue).ToList();
            finalReorderedSchedule.AddRange(unscheduled);

            context.SharedState["Tasks"] = finalReorderedSchedule;
            return context;
        }
    }
}
