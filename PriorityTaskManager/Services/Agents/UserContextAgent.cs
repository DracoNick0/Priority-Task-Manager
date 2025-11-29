using PriorityTaskManager.MCP;

namespace PriorityTaskManager.Services.Agents
{
    public class UserContextAgent : IAgent
    {
        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Phase 3: Refining schedule for user context (Complexity)...");
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks || tasks.Count == 0)
            {
                return context;
            }

            // Group tasks by the date part of their first ScheduledChunk (if any)
            var grouped = tasks
                .Where(t => t.ScheduledParts.Any())
                .GroupBy(t => t.ScheduledParts.Min(p => p.StartTime).Date);

            var finalReorderedSchedule = new List<Models.TaskItem>();

            foreach (var dayGroup in grouped)
            {
                // Sort by complexity descending
                var sorted = dayGroup.OrderByDescending(t => t.Complexity).ToList();
                // Find earliest start time for the day
                var earliest = dayGroup.Min(t => t.ScheduledParts.Min(p => p.StartTime));
                var current = earliest;
                foreach (var task in sorted)
                {
                    // Reassign all chunks for this day in order, preserving duration
                    var chunksForDay = task.ScheduledParts.Where(p => p.StartTime.Date == earliest.Date).OrderBy(p => p.StartTime).ToList();
                    foreach (var chunk in chunksForDay)
                    {
                        var duration = chunk.Duration;
                        chunk.StartTime = current;
                        chunk.EndTime = current + duration;
                        current = chunk.EndTime;
                    }
                    finalReorderedSchedule.Add(task);
                }
            }

            // Add any tasks that did not have any ScheduledParts (not scheduled)
            var unscheduled = tasks.Where(t => !t.ScheduledParts.Any()).ToList();
            finalReorderedSchedule.AddRange(unscheduled);

            context.SharedState["Tasks"] = finalReorderedSchedule;
            return context;
        }
    }
}
