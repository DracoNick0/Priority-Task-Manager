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

            context.History.Add("Phase 2: Building optimal schedule based on importance and due dates...");

            // Retrieve tasks and available time from context
            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj) || tasksObj is not List<Models.TaskItem> allTasks)
                return context;
            if (!context.SharedState.TryGetValue("TotalAvailableTime", out var totalTimeObj) || totalTimeObj is not TimeSpan totalAvailableTime)
                return context;
            // Exclude completed tasks from scheduling
            var schedulableTasks = allTasks.Where(t => !t.IsCompleted).ToList();
            if (schedulableTasks == null || schedulableTasks.Count == 0 || totalAvailableTime <= TimeSpan.Zero)
                return context;

            // Try to get the schedule window (optional for now, fallback to continuous block)
            PriorityTaskManager.Models.ScheduleWindow? scheduleWindow = null;
            if (context.SharedState.TryGetValue("AvailableScheduleWindow", out var windowObj) && windowObj is PriorityTaskManager.Models.ScheduleWindow sw)
                scheduleWindow = sw;

            // Orchestrate scheduling
            var initialResult = TryScheduleTasks(schedulableTasks, totalAvailableTime, scheduleWindow);
            if (initialResult.IsSuccess)
            {
                context.SharedState["Tasks"] = initialResult.ScheduledTasks;
                context.SharedState["UnschedulableTasks"] = initialResult.UnscheduledTasks;
                return context;
            }
            // If failed due to pinned task, get the full chain and re-plan
            if (initialResult.FailedPinnedTask != null)
            {
                var fullChain = _dependencyGraphHelper.GetFullChain(schedulableTasks, initialResult.FailedPinnedTask.Id);
                context.History.Add($"  -> NOTICE: Task chain for '{initialResult.FailedPinnedTask.Title}' is impossible to schedule and has been excluded.");
                var remainingTasks = schedulableTasks.Except(fullChain).ToList();
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
                    var baseTime = DateTime.Now > DateTime.Today.AddHours(9) ? DateTime.Now : DateTime.Today.AddHours(9);
                    return baseTime.Add(offset);
                }
                var remaining = offset;
                var now = DateTime.Now;
                foreach (var slot in window.AvailableSlots.OrderBy(s => s.StartTime))
                {
                    var slotStart = slot.StartTime;
                    // For the first slot, ensure we never schedule before now
                    if (slotStart.Date == now.Date && now > slotStart && now < slot.EndTime)
                    {
                        slotStart = now;
                    }
                    var slotDuration = slot.EndTime - slotStart;
                    if (remaining <= slotDuration)
                        return slotStart.Add(remaining);
                    remaining -= slotDuration;
                }
                // If offset exceeds all slots, return end of last slot
                return window.AvailableSlots.Last().EndTime;
            }

            // Normalize all due dates to end-of-day (23:59:59) if time is 00:00:00
            var normalizedTasks = tasks.Select(t => {
                var due = t.DueDate;
                if (due.TimeOfDay == TimeSpan.Zero)
                {
                    // Set to end of day
                    t.DueDate = due.Date.AddDays(1).AddTicks(-1);
                }
                return t;
            }).ToList();
            var orderedTasks = normalizedTasks.OrderBy(t => t.DueDate).ToList();
            var currentSchedule = new List<Models.TaskItem>();
            var unscheduledTasks = new List<Models.TaskItem>();

            // Track running offset for scheduling
            TimeSpan runningOffset = TimeSpan.Zero;

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

            // Assign ScheduledStartTime and ScheduledEndTime for scheduled tasks
            runningOffset = TimeSpan.Zero;
            foreach (var task in currentSchedule)
            {
                var start = TranslateOffsetToRealTime(runningOffset, scheduleWindow);
                var end = start + task.EstimatedDuration;
                task.ScheduledStartTime = start;
                task.ScheduledEndTime = end;
                runningOffset += task.EstimatedDuration;
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
