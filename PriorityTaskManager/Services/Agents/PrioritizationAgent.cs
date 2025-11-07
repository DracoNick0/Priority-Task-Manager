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

            if (!context.SharedState.TryGetValue("Tasks", out var tasksObj))
                return context;
            var allTasks = tasksObj as List<Models.TaskItem>;
            if (allTasks == null)
                return context;
            if (!context.SharedState.TryGetValue("TotalAvailableTime", out var totalTimeObj))
                return context;
            if (totalTimeObj is not TimeSpan totalAvailableTime)
                return context;

            var schedulableTasks = HandleLateTasks(context, allTasks.Where(t => !t.IsCompleted).ToList());

            if (!schedulableTasks.Any() || totalAvailableTime <= TimeSpan.Zero)
                return context;

            Models.ScheduleWindow? scheduleWindow = null;
            if (context.SharedState.TryGetValue("AvailableScheduleWindow", out var windowObj) && windowObj is Models.ScheduleWindow sw)
                scheduleWindow = sw;

            var result = TryScheduleTasks(schedulableTasks, totalAvailableTime, scheduleWindow);

            if (result.IsSuccess)
            {
                context.SharedState["Tasks"] = result.ScheduledTasks;
                AppendToUnscheduledContext(context, result.UnscheduledTasks);
            }
            else if (result.FailedPinnedTask != null)
            {
                context.History.Add($"  -> NOTICE: Task chain for '{result.FailedPinnedTask.Title}' is impossible to schedule and has been excluded.");
                var fullChain = _dependencyGraphHelper.GetFullChain(schedulableTasks, result.FailedPinnedTask.Id);
                var remainingTasks = schedulableTasks.Except(fullChain).ToList();
                
                var finalResult = TryScheduleTasks(remainingTasks, totalAvailableTime, scheduleWindow);
                context.SharedState["Tasks"] = finalResult.ScheduledTasks;
                AppendToUnscheduledContext(context, fullChain);
                AppendToUnscheduledContext(context, finalResult.UnscheduledTasks);
            }
            else
            {
                context.SharedState["Tasks"] = result.ScheduledTasks;
                AppendToUnscheduledContext(context, result.UnscheduledTasks);
            }
            
            return context;
        }

        private List<Models.TaskItem> HandleLateTasks(MCPContext context, List<Models.TaskItem> tasks)
        {
            if (context.SharedState.TryGetValue("UserProfile", out var userProfileObj) && userProfileObj is Models.UserProfile userProfile)
            {
                var now = DateTime.Now;
                var today = DateTime.Today;
                var workEnd = today.Add(userProfile.WorkEndTime.ToTimeSpan());

                if (now > workEnd)
                {
                    var lateTasks = tasks.Where(t => t.DueDate.Date <= today).ToList();
                    if (lateTasks.Any())
                    {
                        foreach (var late in lateTasks)
                        {
                            late.ScheduledStartTime = null;
                            late.ScheduledEndTime = null;
                        }
                        AppendToUnscheduledContext(context, lateTasks);
                        return tasks.Except(lateTasks).ToList();
                    }
                }
            }
            return tasks;
        }

        private void AppendToUnscheduledContext(MCPContext context, IEnumerable<Models.TaskItem> tasksToAdd)
        {
            if (!tasksToAdd.Any()) return;

            if (!context.SharedState.ContainsKey("UnschedulableTasks"))
            {
                context.SharedState["UnschedulableTasks"] = new List<Models.TaskItem>();
            }
            var unscheduledList = (List<Models.TaskItem>)context.SharedState["UnschedulableTasks"];
            
            foreach (var task in tasksToAdd)
            {
                if (!unscheduledList.Contains(task))
                {
                    unscheduledList.Add(task);
                }
            }
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
