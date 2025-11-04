using PriorityTaskManager.MCP;

using PriorityTaskManager.Services.Helpers;
namespace PriorityTaskManager.Services.Agents
{
    public class PrioritizationAgent : IAgent
    {
        // Helper class to hold scheduling results
        private class ScheduleResult
        {
            public bool IsSuccess { get; set; }
            public List<Models.TaskItem> ScheduledTasks { get; set; } = new List<Models.TaskItem>();
            public List<Models.TaskItem> UnscheduledTasks { get; set; } = new List<Models.TaskItem>();
            public Models.TaskItem? FailedPinnedTask { get; set; } = null;
        }
        private readonly DependencyGraphHelper _dependencyGraphHelper;

        public PrioritizationAgent(DependencyGraphHelper? dependencyGraphHelper = null)
        {
            _dependencyGraphHelper = dependencyGraphHelper ?? new DependencyGraphHelper();
        }

        public MCPContext Act(MCPContext context)
        {
            context.History.Add("PrioritizationAgent execution started.");

            // Retrieve tasks and available time from context
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> allTasks)
                return context;
            if (!context.SharedState.TryGetValue("TotalAvailableTime", out var totalTimeObj) || totalTimeObj is not TimeSpan totalAvailableTime)
                return context;
            if (allTasks == null || allTasks.Count == 0 || totalAvailableTime <= TimeSpan.Zero)
                return context;

            // Try to get the schedule window (optional for now, fallback to continuous block)
            PriorityTaskManager.Models.ScheduleWindow? scheduleWindow = null;
            if (context.SharedState.TryGetValue("AvailableScheduleWindow", out var windowObj) && windowObj is PriorityTaskManager.Models.ScheduleWindow sw)
                scheduleWindow = sw;

            // Orchestrate scheduling
            var initialResult = TryScheduleTasks(allTasks, totalAvailableTime, scheduleWindow);
            if (initialResult.IsSuccess)
            {
                context.SharedState["Tasks"] = initialResult.ScheduledTasks;
                context.SharedState["UnschedulableTasks"] = initialResult.UnscheduledTasks;
                return context;
            }
            // If failed due to pinned task, get the full chain and re-plan
            if (initialResult.FailedPinnedTask != null)
            {
                var fullChain = _dependencyGraphHelper.GetFullChain(allTasks, initialResult.FailedPinnedTask.Id);
                var remainingTasks = allTasks.Except(fullChain).ToList();
                var finalResult = TryScheduleTasks(remainingTasks, totalAvailableTime, scheduleWindow);
                context.SharedState["Tasks"] = finalResult.ScheduledTasks;
                context.SharedState["UnschedulableTasks"] = fullChain;
                return context;
            }
            // Fallback: just return what we have
            context.SharedState["Tasks"] = initialResult.ScheduledTasks;
            context.SharedState["UnschedulableTasks"] = initialResult.UnscheduledTasks;
            return context;
        }

        private ScheduleResult TryScheduleTasks(List<Models.TaskItem> tasks, TimeSpan totalAvailableTime, PriorityTaskManager.Models.ScheduleWindow? scheduleWindow)
        {
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
            var unscheduledTasks = new List<Models.TaskItem>();

            foreach (var taskToTry in orderedTasks)
            {
                var tentativeSchedule = new List<Models.TaskItem>(currentSchedule) { taskToTry };
                bool pinnedFailure = false;
                while (true)
                {
                    var totalDurationTicks = tentativeSchedule.Sum(t => t.EstimatedDuration.Ticks);
                    var totalDuration = TimeSpan.FromTicks(totalDurationTicks);
                    if (totalDuration > totalAvailableTime)
                    {
                        // Remove lowest-importance unpinned task
                        var unpinned = tentativeSchedule.Where(t => !t.IsPinned).ToList();
                        if (unpinned.Count == 0)
                        {
                            // All tasks are pinned, cannot remove any
                            pinnedFailure = taskToTry.IsPinned;
                            break;
                        }
                        var minImportance = unpinned.Min(t => t.Importance);
                        var toRemove = unpinned.First(t => t.Importance == minImportance);
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
                    // Due date violated: remove lowest-importance unpinned task
                    var unpinnedDue = tentativeSchedule.Where(t => !t.IsPinned).ToList();
                    if (unpinnedDue.Count == 0)
                    {
                        // All tasks are pinned, cannot remove any
                        pinnedFailure = taskToTry.IsPinned;
                        break;
                    }
                    var minImp = unpinnedDue.Min(t => t.Importance);
                    var removeTask = unpinnedDue.First(t => t.Importance == minImp);
                    tentativeSchedule.Remove(removeTask);
                    if (removeTask == taskToTry)
                        break; // Can't schedule this task
                }
                if (pinnedFailure && taskToTry.IsPinned)
                {
                    // Impossible to schedule a pinned task
                    return new ScheduleResult
                    {
                        IsSuccess = false,
                        FailedPinnedTask = taskToTry,
                        ScheduledTasks = new List<Models.TaskItem>(currentSchedule),
                        UnscheduledTasks = tasks.Except(currentSchedule).ToList()
                    };
                }
                if (tentativeSchedule.Contains(taskToTry))
                {
                    currentSchedule = new List<Models.TaskItem>(tentativeSchedule);
                }
                else
                {
                    unscheduledTasks.Add(taskToTry);
                }
            }
            return new ScheduleResult
            {
                IsSuccess = true,
                ScheduledTasks = currentSchedule,
                UnscheduledTasks = unscheduledTasks
            };
        }
        }
    }
