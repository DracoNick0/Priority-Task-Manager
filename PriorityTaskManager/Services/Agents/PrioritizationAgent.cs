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

            // Exclude tasks that are late (due today and after work hours)
            if (context.SharedState.TryGetValue("UserProfile", out var userProfileObj) && userProfileObj is PriorityTaskManager.Models.UserProfile userProfile)
            {
                var now = DateTime.Now;
                var today = DateTime.Today;
                var workEnd = today.Add(userProfile.WorkEndTime.ToTimeSpan());
                if (now > workEnd)
                {
                    // After work hours: tasks due today are late
                    var lateTasks = schedulableTasks.Where(t => t.DueDate.Date <= today).ToList();
                    foreach (var late in lateTasks)
                    {
                        // Mark as unscheduled (late)
                        late.ScheduledStartTime = null;
                        late.ScheduledEndTime = null;
                    }
                    // Remove from schedulable
                    schedulableTasks = schedulableTasks.Except(lateTasks).ToList();
                    // Add to unschedulable in context
                    if (!context.SharedState.ContainsKey("UnschedulableTasks"))
                        context.SharedState["UnschedulableTasks"] = new List<Models.TaskItem>();
                    var unschedulable = (List<Models.TaskItem>)context.SharedState["UnschedulableTasks"];
                    foreach (var late in lateTasks)
                        if (!unschedulable.Contains(late)) unschedulable.Add(late);

                    // Also add to PrioritizationResult.UnscheduledTasks if present
                    if (context.SharedState.TryGetValue("PrioritizationResult", out var prObj) && prObj is PriorityTaskManager.Models.PrioritizationResult pr)
                    {
                        foreach (var late in lateTasks)
                            if (!pr.UnscheduledTasks.Contains(late)) pr.UnscheduledTasks.Add(late);
                    }
                }
            }

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
                // Append to UnschedulableTasks, avoiding duplicates
                if (!context.SharedState.ContainsKey("UnschedulableTasks"))
                    context.SharedState["UnschedulableTasks"] = new List<Models.TaskItem>();
                var unschedulable = (List<Models.TaskItem>)context.SharedState["UnschedulableTasks"];
                foreach (var t in initialResult.UnscheduledTasks)
                    if (!unschedulable.Contains(t)) unschedulable.Add(t);
                // Also set UnscheduledTasks in PrioritizationResult if present
                if (context.SharedState.TryGetValue("PrioritizationResult", out var prObj) && prObj is PriorityTaskManager.Models.PrioritizationResult pr)
                {
                    foreach (var t in initialResult.UnscheduledTasks)
                        if (!pr.UnscheduledTasks.Contains(t)) pr.UnscheduledTasks.Add(t);
                }
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
                // Append to UnschedulableTasks, avoiding duplicates
                if (!context.SharedState.ContainsKey("UnschedulableTasks"))
                    context.SharedState["UnschedulableTasks"] = new List<Models.TaskItem>();
                var unschedulable = (List<Models.TaskItem>)context.SharedState["UnschedulableTasks"];
                foreach (var t in fullChain)
                    if (!unschedulable.Contains(t)) unschedulable.Add(t);
                if (context.SharedState.TryGetValue("PrioritizationResult", out var prObj) && prObj is PriorityTaskManager.Models.PrioritizationResult pr)
                {
                    foreach (var t in fullChain)
                        if (!pr.UnscheduledTasks.Contains(t)) pr.UnscheduledTasks.Add(t);
                }
                return context;
            }
            // Fallback: just return what we have
            context.SharedState["Tasks"] = initialResult.ScheduledTasks;
            // Append to UnschedulableTasks, avoiding duplicates
            if (!context.SharedState.ContainsKey("UnschedulableTasks"))
                context.SharedState["UnschedulableTasks"] = new List<Models.TaskItem>();
            var unschedulableFallback = (List<Models.TaskItem>)context.SharedState["UnschedulableTasks"];
            foreach (var t in initialResult.UnscheduledTasks)
                if (!unschedulableFallback.Contains(t)) unschedulableFallback.Add(t);
            if (context.SharedState.TryGetValue("PrioritizationResult", out var prObj2) && prObj2 is PriorityTaskManager.Models.PrioritizationResult pr2)
            {
                foreach (var t in initialResult.UnscheduledTasks)
                    if (!pr2.UnscheduledTasks.Contains(t)) pr2.UnscheduledTasks.Add(t);
            }
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

            // For rollback: keep a stack of unscheduled tasks per schedule state
            var unscheduledStack = new Stack<List<Models.TaskItem>>();

            foreach (var taskToTry in orderedTasks)
            {
                var tentativeSchedule = new List<Models.TaskItem>(currentSchedule) { taskToTry };
                var localUnscheduled = new List<Models.TaskItem>();
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
                        // If the lowest importance is the same as the task to try, remove the task to try
                        if (taskToTry.Importance == minImportance)
                        {
                            tentativeSchedule.Remove(taskToTry);
                            localUnscheduled.Add(taskToTry);
                            break; // Can't schedule this task
                        }
                        var toRemove = unpinned.First(t => t.Importance == minImportance);
                        tentativeSchedule.Remove(toRemove);
                        localUnscheduled.Add(toRemove);
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
                    // If the lowest importance is the same as the task to try, remove the task to try
                    if (taskToTry.Importance == minImp)
                    {
                        tentativeSchedule.Remove(taskToTry);
                        localUnscheduled.Add(taskToTry);
                        break; // Can't schedule this task
                    }
                    var removeTask = unpinnedDue.First(t => t.Importance == minImp);
                    tentativeSchedule.Remove(removeTask);
                    localUnscheduled.Add(removeTask);
                    if (removeTask == taskToTry)
                        break; // Can't schedule this task
                }
                if (pinnedFailure && taskToTry.IsPinned)
                {
                    // Impossible to schedule a pinned task
                    // Roll back any local unscheduled tasks
                    foreach (var t in localUnscheduled)
                        unscheduledTasks.Remove(t);
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
                    // Commit local unscheduled tasks
                    foreach (var t in localUnscheduled)
                        if (!unscheduledTasks.Contains(t)) unscheduledTasks.Add(t);
                }
                else
                {
                    // Only add taskToTry if it was not already unscheduled
                    if (!unscheduledTasks.Contains(taskToTry))
                        unscheduledTasks.Add(taskToTry);
                    // Also commit any other local unscheduled tasks
                    foreach (var t in localUnscheduled)
                        if (!unscheduledTasks.Contains(t)) unscheduledTasks.Add(t);
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

            // Clear ScheduledStartTime and ScheduledEndTime for unscheduled tasks
            foreach (var task in unscheduledTasks)
            {
                task.ScheduledStartTime = null;
                task.ScheduledEndTime = null;
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
