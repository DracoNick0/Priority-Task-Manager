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

            // Try to get the schedule window (optional for now, fallback to continuous block)
            PriorityTaskManager.Models.ScheduleWindow? scheduleWindow = null;
            if (context.SharedState.TryGetValue("AvailableScheduleWindow", out var windowObj) && windowObj is PriorityTaskManager.Models.ScheduleWindow sw)
                scheduleWindow = sw;

            // Helper to translate offset to real DateTime using the schedule window
            DateTime TranslateOffsetToRealTime(TimeSpan offset, PriorityTaskManager.Models.ScheduleWindow? window)
            {
                if (window == null || window.AvailableSlots.Count == 0)
                {
                    // Fallback: assume now + offset
                    return DateTime.Today.AddHours(9).Add(offset); // Assume workday starts at 9 AM
                }
                var remaining = offset;
                foreach (var slot in window.AvailableSlots.OrderBy(s => s.StartTime))
                {
                    var slotDuration = slot.EndTime - slot.StartTime;
                    if (remaining <= slotDuration)
                        return slot.StartTime.Add(remaining);
                    remaining -= slotDuration;
                }
                // If offset exceeds all slots, return end of last slot
                return window.AvailableSlots.Last().EndTime;
            }

            var orderedTasks = tasks.OrderBy(t => t.DueDate).ToList();
            var currentSchedule = new List<Models.TaskItem>();

            foreach (var taskToTry in orderedTasks)
            {
                var tentativeSchedule = new List<Models.TaskItem>(currentSchedule) { taskToTry };
                while (true)
                {
                    var totalDurationTicks = tentativeSchedule.Sum(t => t.EstimatedDuration.Ticks);
                    var totalDuration = TimeSpan.FromTicks(totalDurationTicks);
                    if (totalDuration > totalAvailableTime)
                    {
                        // Remove lowest-importance task
                        var minImportance = tentativeSchedule.Min(t => t.Importance);
                        var toRemove = tentativeSchedule.First(t => t.Importance == minImportance);
                        tentativeSchedule.Remove(toRemove);
                        if (toRemove == taskToTry)
                            break; // Can't schedule this task
                        continue;
                    }
                    // Translate offset to real end time
                    var realEndTime = TranslateOffsetToRealTime(totalDuration, scheduleWindow);
                    if (realEndTime <= taskToTry.DueDate)
                    {
                        // Valid schedule
                        break;
                    }
                    // Due date violated: remove lowest-importance task
                    var minImp = tentativeSchedule.Min(t => t.Importance);
                    var removeTask = tentativeSchedule.First(t => t.Importance == minImp);
                    tentativeSchedule.Remove(removeTask);
                    if (removeTask == taskToTry)
                        break; // Can't schedule this task
                }
                if (tentativeSchedule.Contains(taskToTry))
                {
                    currentSchedule = new List<Models.TaskItem>(tentativeSchedule);
                }
            }
            context.SharedState["Tasks"] = currentSchedule;
            return context;
        }
    }
}
