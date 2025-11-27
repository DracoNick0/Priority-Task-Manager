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
            context.History.Add("Phase 4: Spreading tasks across available time slots...");

            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> tasks || tasks.Count == 0)
                return context;

            if (!context.SharedState.TryGetValue("AvailableScheduleWindow", out var scheduleWindowObj) || scheduleWindowObj is not Models.ScheduleWindow scheduleWindow)
                return context;

            var scheduledTasks = new List<Models.TaskItem>();
            var unschedulableTasks = new List<Models.TaskItem>();
            var availableSlots = scheduleWindow.AvailableSlots.OrderBy(s => s.StartTime).ToList();

            foreach (var task in tasks)
            {
                bool scheduled = false;
                for (int i = 0; i < availableSlots.Count; i++)
                {
                    var slot = availableSlots[i];
                    if (slot.EndTime - slot.StartTime >= task.EstimatedDuration)
                    {
                        // Schedule the task in this slot
                        task.ScheduledStartTime = slot.StartTime;
                        task.ScheduledEndTime = slot.StartTime + task.EstimatedDuration;
                        
                        // Update the slot
                        slot.StartTime += task.EstimatedDuration;

                        // If the slot is now used up, remove it
                        if (slot.StartTime >= slot.EndTime)
                        {
                            availableSlots.RemoveAt(i);
                            i--; // Adjust index after removal
                        }

                        scheduledTasks.Add(task);
                        scheduled = true;
                        break; // Move to the next task
                    }
                }

                if (!scheduled)
                {
                    unschedulableTasks.Add(task);
                }
            }

            context.SharedState["Tasks"] = scheduledTasks;
            context.SharedState["UnschedulableTasks"] = unschedulableTasks;
            
            return context;
        }
    }
}
