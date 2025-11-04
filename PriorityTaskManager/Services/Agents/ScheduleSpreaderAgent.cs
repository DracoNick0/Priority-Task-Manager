using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    /// <summary>
    /// Agent responsible for spreading scheduled tasks to avoid conflicts and optimize distribution.
    /// </summary>
    public class ScheduleSpreaderAgent : IAgent
    {
        /// <inheritdoc />
        public MCPContext Act(MCPContext context)
        {
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks || tasks.Count == 0)
                return context;
            if (!context.SharedState.TryGetValue("UserProfile", out var userProfileObj) || userProfileObj is not Models.UserProfile userProfile)
                return context;

            // Sort tasks by ScheduledStartTime
            var sortedTasks = tasks.OrderBy(t => t.ScheduledStartTime).ToList();
            var finalSpacedSchedule = new List<Models.TaskItem>();
            if (sortedTasks.Count > 0)
            {
                finalSpacedSchedule.Add(sortedTasks[0]); // First task stays as anchor
            }
            for (int i = 1; i < sortedTasks.Count; i++)
            {
                var prevTask = finalSpacedSchedule.Last();
                var currentTask = sortedTasks[i];
                if (prevTask.ScheduledEndTime.HasValue)
                {
                    var desiredStartTime = prevTask.ScheduledEndTime.Value + userProfile.DesiredBreatherDuration;
                    var newEndTime = desiredStartTime + currentTask.EstimatedDuration;
                    // Only update if new end time is before or equal to due date
                    if (newEndTime <= currentTask.DueDate)
                    {
                        currentTask.ScheduledStartTime = desiredStartTime;
                        currentTask.ScheduledEndTime = newEndTime;
                    }
                }
                finalSpacedSchedule.Add(currentTask);
            }
            context.SharedState["Tasks"] = finalSpacedSchedule;
            return context;
        }
    }
}
