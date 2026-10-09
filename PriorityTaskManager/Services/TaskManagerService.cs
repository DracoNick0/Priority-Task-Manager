using PriorityTaskManager.Models;
using PriorityTaskManager.Services.Helpers;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        /// <summary>
        /// Returns the prioritized (processed) list of tasks and agent logs for the given list, using the active urgency strategy.
        /// </summary>
        /// <param name="listId">The ID of the list to prioritize tasks for.</param>
        /// <returns>The prioritization result (tasks and history).</returns>
        public PrioritizationResult GetPrioritizedTasks(Guid listId, ITimeService timeService)
        {
            var now = timeService.GetCurrentTime();
            var recurrenceStateChanged = false;
            foreach (var recurringTask in _data.Tasks.Where(t => t.ListId == listId && t.RecurrenceRule != null))
                recurrenceStateChanged |= ReconcileTaskOccurrences(recurringTask, now);
            if (recurrenceStateChanged)
                SaveData();

            var currentList = _data.Lists.FirstOrDefault(l => l.Id == listId);
            var effectiveProfile = BuildEffectiveUserProfile(currentList);

            IUrgencyStrategy strategy;
            if (effectiveProfile.SchedulingMode == SchedulingMode.ConstraintOptimization)
            {
                // The Constraint Solver strategy is still under development. Route to its stub
                // implementation, which returns a graceful "not yet implemented" result instead of
                // letting an unhandled exception propagate up through the CLI dashboard refresh path.
                strategy = new PriorityTaskManager.Scheduling.Optimization.ConstraintOptimizationStrategy(effectiveProfile, _data.Events, timeService);
            }
            else
            {
                strategy = new PriorityTaskManager.Scheduling.GoldPanning.GoldPanningStrategy(effectiveProfile, _data.Events, timeService, new RecurrenceExpansionService());
            }
            
            var rawTasks = GetAllTasks(listId).Where(task => !task.IsCompleted).ToList();

            // Apply the list's intrinsic sort option before scheduling so tie-breakers align with user intent
            var effectiveSortOption = currentList?.SortOption ?? _data.UserProfile.DefaultListSortOption;
            if (effectiveSortOption != SortOption.Default)
            {
                rawTasks = effectiveSortOption switch
                {
                    SortOption.Alphabetical => rawTasks.OrderBy(t => t.Title).ToList(),
                    SortOption.DueDate => rawTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList(),
                    SortOption.Id => rawTasks.OrderBy(t => t.Id).ToList(),
                    _ => rawTasks
                };
            }

            var result = strategy.CalculateUrgency(rawTasks);
            return result;
        }


        public UserProfile GetUserProfile()
        {
            return _data.UserProfile;
        }
        
        /// <summary>
        /// Updates the user profile and persists the change.
        /// </summary>
        /// <param name="updatedProfile">The new user profile to persist.</param>
        public void UpdateUserProfile(UserProfile updatedProfile)
        {
            ValidateWorkHours(updatedProfile.WorkStartTime, updatedProfile.WorkEndTime);
            _data.UserProfile = updatedProfile;
            SaveData();
        }

        private readonly IUrgencyStrategy _urgencyStrategy;
        private readonly IPersistenceService _persistenceService;
        private readonly IEventService _eventService;
        private readonly DependencyGraphHelper _dependencyGraphHelper = new DependencyGraphHelper();
        private DataContainer _data;
        public UserProfile UserProfile => _data.UserProfile;


        /// <summary>
        /// Initializes a new instance of the TaskManagerService class with the given persistence and urgency strategies.
        /// </summary>
        /// <param name="urgencyStrategy">The urgency strategy used to calculate task urgency.</param>
        /// <param name="persistenceService">The persistence service for loading and saving data.</param>
        public TaskManagerService(IUrgencyStrategy urgencyStrategy, IPersistenceService persistenceService, DataContainer data)
        {
            _urgencyStrategy = urgencyStrategy;
            _persistenceService = persistenceService;
            _data = data;
            _eventService = new EventService(_persistenceService, _data);
            // Ensure at least one default list exists
            if (_data.Lists == null || _data.Lists.Count == 0)
            {
                _data.Lists = new List<TaskList>
                {
                    new TaskList { Id = Guid.NewGuid(), Name = "General" }
                };
            }

            var changed = false;
            foreach (var list in _data.Lists)
            {
                changed |= ApplyDefaultsIfNeeded(list);
            }

            // Ensure an active list is set
            if (_data.ActiveListId == Guid.Empty)
            {
                _data.ActiveListId = _data.Lists.First().Id;
                changed = true;
            }

            if (changed)
            {
                SaveData();
            }
        }

        public void SaveData() => _persistenceService.SaveData(_data);

        /// <summary>
        /// Adjusts NextDisplayId if needed.
        /// </summary>
        public void SyncNextDisplayId()
        {
            int maxId = _data.Tasks.Any() ? _data.Tasks.Max(t => t.DisplayId) : 0;
            _data.NextDisplayId = maxId + 1;
            SaveData();
        }

        /// <summary>
        /// Calculates urgency for all tasks using the urgency strategy.
        /// </summary>
        public void CalculateUrgencyForAllTasks()
        {
            _data.Tasks = _urgencyStrategy.CalculateUrgency(_data.Tasks).Tasks;
        }

        /// <summary>
        /// Adds a new task to the collection and saves the changes.
        /// </summary>
        /// <param name="task">The TaskItem object to add.</param>
        public void AddTask(TaskItem task)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                throw new ArgumentException("Task title cannot be empty.");
            }
            if (task.RecurrenceRule != null)
                ValidateRecurringTask(task);
            task.Id = Guid.NewGuid();
            if (task.RecurrenceRule != null)
                task.SeriesId = task.Id;
            task.EffectiveImportance = task.Importance;
            task.DisplayId = _data.NextDisplayId++;
            _data.Tasks.Add(task);
            SaveData();
        }

        /// <summary>
        /// Retrieves all tasks associated with a specific list ID.
        /// </summary>
        /// <param name="listId">The ID of the list.</param>
        /// <returns>An enumerable collection of tasks.</returns>
        public IEnumerable<TaskItem> GetAllTasks(Guid listId)
        {
            return _data.Tasks.Where(task => task.ListId == listId);
        }

        public List<TaskItem> GetAllTasks()
        {
            return new List<TaskItem>(_data.Tasks);
        }

        /// <summary>
        /// Retrieves a task by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task.</param>
        /// <returns>The task if found; otherwise, null.</returns>
        public TaskItem? GetTaskById(Guid id)
        {
            return _data.Tasks.Find(t => t.Id == id);
        }

        /// <summary>Gets recurring task occurrences and lazily applies missed-date transitions.</summary>
        public IReadOnlyList<TaskOccurrence> GetTaskOccurrences(Guid listId, DateTime rangeStart, DateTime rangeEnd, ITimeService timeService)
        {
            if (timeService == null) throw new ArgumentNullException(nameof(timeService));
            if (rangeEnd.Date < rangeStart.Date)
                throw new ArgumentException("Occurrence window end must not precede its start.", nameof(rangeEnd));

            var now = timeService.GetCurrentTime();
            var changed = false;
            var result = new List<TaskOccurrence>();
            foreach (var task in _data.Tasks.Where(t => t.ListId == listId && t.RecurrenceRule != null))
            {
                changed |= ReconcileTaskOccurrences(task, now);
                result.AddRange(ProjectTaskOccurrences(task, rangeStart.Date, rangeEnd.Date, now.Date));
            }

            if (changed)
                SaveData();
            return result.OrderBy(o => o.ScheduledDate).ThenBy(o => o.Series.Title).ToList();
        }

        /// <summary>Increments completion progress for one recurrence-rule date.</summary>
        public bool CompleteTaskOccurrence(Guid seriesId, DateTime occurrenceDate, ITimeService timeService)
        {
            var task = GetRecurringTask(seriesId);
            if (task == null)
                return false;

            var now = timeService.GetCurrentTime();
            var date = occurrenceDate.Date;
            if (!IsScheduledOccurrence(task, date))
                return false;

            ReconcileTaskOccurrences(task, now);
            var state = GetOrCreateOccurrenceState(task, date);
            EnsureSequentialOccurrence(task, state);
            if (state.Status is TaskOccurrenceStatus.Completed or TaskOccurrenceStatus.Skipped or TaskOccurrenceStatus.Disregarded)
                throw new InvalidOperationException("This occurrence is already resolved.");

            state.CompletionCount = Math.Min(task.RequiredCompletions, state.CompletionCount + 1);
            if (state.CompletionCount == task.RequiredCompletions)
            {
                state.Status = TaskOccurrenceStatus.Completed;
                state.CompletedAt = now;
            }
            else if (date < now.Date)
            {
                state.Status = TaskOccurrenceStatus.Missed;
            }

            UpdateStreak(task);
            UpdateSeriesCompletion(task, now.Date);
            SaveData();
            return true;
        }

        /// <summary>Undoes one completion action on the specified occurrence without changing other occurrences.</summary>
        public bool UndoTaskOccurrenceCompletion(Guid seriesId, DateTime occurrenceDate, ITimeService timeService)
        {
            var task = GetRecurringTask(seriesId);
            if (task == null)
                return false;

            var date = occurrenceDate.Date;
            var state = task.OccurrenceStates?.FirstOrDefault(o => o.ScheduledDate.Date == date);
            if (state == null || state.CompletionCount <= 0)
                return false;

            state.CompletionCount--;
            if (state.CompletionCount >= task.RequiredCompletions)
            {
                state.Status = TaskOccurrenceStatus.Completed;
            }
            else
            {
                state.CompletedAt = null;
                state.Status = date < timeService.GetCurrentTime().Date
                    ? TaskOccurrenceStatus.Missed
                    : TaskOccurrenceStatus.Pending;
            }
            task.IsCompleted = false;
            UpdateStreak(task);
            SaveData();
            return true;
        }

        /// <summary>Manually skips one unresolved occurrence.</summary>
        public bool SkipTaskOccurrence(Guid seriesId, DateTime occurrenceDate, ITimeService timeService)
        {
            var task = GetRecurringTask(seriesId);
            if (task == null)
                return false;

            var now = timeService.GetCurrentTime();
            var date = occurrenceDate.Date;
            if (!IsScheduledOccurrence(task, date))
                return false;

            ReconcileTaskOccurrences(task, now);
            var state = GetOrCreateOccurrenceState(task, date);
            EnsureSequentialOccurrence(task, state);
            if (state.Status is TaskOccurrenceStatus.Completed or TaskOccurrenceStatus.Skipped or TaskOccurrenceStatus.Disregarded)
                throw new InvalidOperationException("This occurrence is already resolved.");

            state.CompletionCount = 0;
            state.CompletedAt = null;
            state.Status = TaskOccurrenceStatus.Skipped;
            UpdateStreak(task);
            UpdateSeriesCompletion(task, now.Date);
            SaveData();
            return true;
        }

        private static readonly RecurrenceExpansionService TaskRecurrenceExpansion = new();

        private static TaskItem? GetRecurringTaskBySeriesId(DataContainer data, Guid seriesId) =>
            data.Tasks.FirstOrDefault(t => t.RecurrenceRule != null && (t.SeriesId ?? t.Id) == seriesId);

        private TaskItem? GetRecurringTask(Guid seriesId) => GetRecurringTaskBySeriesId(_data, seriesId);

        private static bool IsScheduledOccurrence(TaskItem task, DateTime date) =>
            TaskRecurrenceExpansion.GetOccurrences(
                task.RecurrenceRule!,
                Array.Empty<RecurrenceException>(),
                date.Date,
                date.Date).Count > 0;

        private static TaskOccurrenceState GetOrCreateOccurrenceState(TaskItem task, DateTime date)
        {
            task.OccurrenceStates ??= new List<TaskOccurrenceState>();
            var state = task.OccurrenceStates.FirstOrDefault(o => o.ScheduledDate.Date == date.Date);
            if (state != null)
                return state;

            state = new TaskOccurrenceState { ScheduledDate = date.Date };
            task.OccurrenceStates.Add(state);
            return state;
        }

        private static bool ReconcileTaskOccurrences(TaskItem task, DateTime now)
        {
            if (task.RequiredCompletions < 1)
                throw new InvalidOperationException($"Recurring task '{task.Id}' has an invalid RequiredCompletions value.");

            task.OccurrenceStates ??= new List<TaskOccurrenceState>();
            var changed = false;
            var generated = TaskRecurrenceExpansion.GetOccurrences(
                task.RecurrenceRule!,
                Array.Empty<RecurrenceException>(),
                task.RecurrenceRule!.SeriesStartDate.Date,
                now.Date);

            foreach (var date in generated)
            {
                var state = task.OccurrenceStates.FirstOrDefault(o => o.ScheduledDate.Date == date.Date);
                if (state == null)
                {
                    state = new TaskOccurrenceState
                    {
                        ScheduledDate = date.Date,
                        Status = date.Date < now.Date &&
                            task.ProgressionMode == TaskProgressionMode.RollForwardDisregard
                                ? TaskOccurrenceStatus.Disregarded
                                : date.Date < now.Date
                                    ? TaskOccurrenceStatus.Missed
                                    : TaskOccurrenceStatus.Pending
                    };
                    task.OccurrenceStates.Add(state);
                    changed = true;
                }
                else if (date.Date < now.Date)
                {
                    if (task.ProgressionMode == TaskProgressionMode.RollForwardDisregard &&
                        state.Status is TaskOccurrenceStatus.Pending or TaskOccurrenceStatus.Missed)
                    {
                        state.Status = TaskOccurrenceStatus.Disregarded;
                        state.CompletionCount = 0;
                        state.CompletedAt = null;
                        changed = true;
                    }
                    else if (task.ProgressionMode != TaskProgressionMode.RollForwardDisregard &&
                        state.Status == TaskOccurrenceStatus.Pending)
                    {
                        state.Status = TaskOccurrenceStatus.Missed;
                        changed = true;
                    }
                }
            }

            foreach (var state in task.OccurrenceStates)
            {
                if (state.Status is TaskOccurrenceStatus.Skipped or TaskOccurrenceStatus.Disregarded)
                    continue;

                if (state.CompletionCount >= task.RequiredCompletions && state.Status != TaskOccurrenceStatus.Completed)
                {
                    state.Status = TaskOccurrenceStatus.Completed;
                    state.CompletedAt ??= now;
                    changed = true;
                }
                else if (state.CompletionCount < task.RequiredCompletions && state.Status == TaskOccurrenceStatus.Completed)
                {
                    state.Status = state.ScheduledDate.Date < now.Date
                        ? TaskOccurrenceStatus.Missed
                        : TaskOccurrenceStatus.Pending;
                    state.CompletedAt = null;
                    changed = true;
                }
            }

            var previousCurrent = task.CurrentStreak;
            var previousBest = task.BestStreak;
            UpdateStreak(task);
            changed |= previousCurrent != task.CurrentStreak || previousBest != task.BestStreak;
            var wasCompleted = task.IsCompleted;
            UpdateSeriesCompletion(task, now.Date, generated);
            changed |= wasCompleted != task.IsCompleted;
            return changed;
        }

        private static List<TaskOccurrence> ProjectTaskOccurrences(TaskItem task, DateTime from, DateTime to, DateTime today)
        {
            var generated = TaskRecurrenceExpansion.GetOccurrences(
                task.RecurrenceRule!,
                Array.Empty<RecurrenceException>(),
                task.RecurrenceRule!.SeriesStartDate.Date,
                to > today ? to : today);
            var byDate = (task.OccurrenceStates ?? new List<TaskOccurrenceState>())
                .ToDictionary(o => o.ScheduledDate.Date);
            var selected = generated
                .Where(d => d.Date >= from && d.Date <= to)
                .Select(d => d.Date)
                .ToHashSet();

            if (task.ProgressionMode == TaskProgressionMode.SequentialCatchUp)
            {
                var firstUnresolved = generated
                    .Select(d => d.Date)
                    .FirstOrDefault(date => !byDate.TryGetValue(date, out var state) ||
                        state.Status is TaskOccurrenceStatus.Pending or TaskOccurrenceStatus.Missed);
                if (firstUnresolved != default)
                    selected.Add(firstUnresolved);
                selected.RemoveWhere(date => date != firstUnresolved &&
                    (!byDate.TryGetValue(date, out var state) ||
                        state.Status is TaskOccurrenceStatus.Pending or TaskOccurrenceStatus.Missed));
            }
            else if (task.ProgressionMode == TaskProgressionMode.RollForwardKeepBacklog)
            {
                selected.UnionWith((task.OccurrenceStates ?? new List<TaskOccurrenceState>())
                    .Where(o => o.Status == TaskOccurrenceStatus.Missed)
                    .Select(o => o.ScheduledDate.Date));
            }

            var currentStreak = task.TrackStreak ? task.CurrentStreak : 0;
            var bestStreak = task.TrackStreak ? task.BestStreak : 0;
            return selected.OrderBy(d => d).Select(date =>
            {
                byDate.TryGetValue(date, out var state);
                var status = state?.Status ?? TaskOccurrenceStatus.Pending;
                return new TaskOccurrence(
                    task,
                    date,
                    status,
                    state?.CompletionCount ?? 0,
                    status == TaskOccurrenceStatus.Missed,
                    currentStreak,
                    bestStreak);
            }).ToList();
        }

        private static void EnsureSequentialOccurrence(TaskItem task, TaskOccurrenceState target)
        {
            if (task.ProgressionMode != TaskProgressionMode.SequentialCatchUp)
                return;

            var oldestUnresolved = task.OccurrenceStates
                .Where(o => o.Status is TaskOccurrenceStatus.Pending or TaskOccurrenceStatus.Missed)
                .OrderBy(o => o.ScheduledDate)
                .FirstOrDefault();
            if (oldestUnresolved != target)
                throw new InvalidOperationException("Resolve the oldest occurrence before advancing this series.");
        }

        private static void UpdateStreak(TaskItem task)
        {
            if (!task.TrackStreak)
                return;

            var current = 0;
            var best = task.BestStreak;
            var blockedByUnresolvedOccurrence = false;
            foreach (var occurrence in task.OccurrenceStates.OrderBy(o => o.ScheduledDate))
            {
                switch (occurrence.Status)
                {
                    case TaskOccurrenceStatus.Completed:
                        if (blockedByUnresolvedOccurrence)
                            current = 0;
                        else
                            current++;
                        best = Math.Max(best, current);
                        break;
                    case TaskOccurrenceStatus.Pending:
                    case TaskOccurrenceStatus.Missed:
                        blockedByUnresolvedOccurrence = true;
                        break;
                    case TaskOccurrenceStatus.Skipped:
                    case TaskOccurrenceStatus.Disregarded:
                        current = 0;
                        blockedByUnresolvedOccurrence = false;
                        break;
                }
            }

            task.CurrentStreak = current;
            task.BestStreak = best;
        }

        private static void UpdateSeriesCompletion(TaskItem task, DateTime today, IReadOnlyList<DateTime>? generated = null)
        {
            var rule = task.RecurrenceRule!;
            generated ??= TaskRecurrenceExpansion.GetOccurrences(
                rule,
                Array.Empty<RecurrenceException>(),
                rule.SeriesStartDate.Date,
                today.Date);

            var endReached = rule.EndCondition switch
            {
                AfterOccurrencesEndCondition after => generated.Count >= after.OccurrenceCount,
                UntilDateEndCondition until => today.Date > until.UntilDate.Date,
                _ when rule is ExplicitDatesRecurrenceRule explicitDates =>
                    explicitDates.Dates.All(date => date.Date <= today.Date),
                _ => false
            };
            if (!endReached)
            {
                task.IsCompleted = false;
                return;
            }

            var states = task.OccurrenceStates ?? new List<TaskOccurrenceState>();
            task.IsCompleted = generated.All(date =>
            {
                var state = states.FirstOrDefault(o => o.ScheduledDate.Date == date.Date);
                return state?.Status is TaskOccurrenceStatus.Completed or TaskOccurrenceStatus.Skipped or TaskOccurrenceStatus.Disregarded;
            });
        }

        private static void ValidateRecurringTask(TaskItem task)
        {
            if (task.RequiredCompletions < 1)
                throw new ArgumentOutOfRangeException(nameof(task.RequiredCompletions), "Required completions must be at least one.");
            if (task.RecurrenceRule == null || task.RecurrenceRule.EndCondition == null)
                throw new ArgumentException("A recurring task requires a recurrence rule and end condition.");
            if (task.RecurrenceRule.SeriesStartDate == default)
                throw new ArgumentException("A recurring task requires a valid series start date.");
            if (task.Dependencies.Count > 0)
                throw new ArgumentException("Recurring task dependencies are not supported.");
            if (task.RecurrenceRule.EndCondition is AfterOccurrencesEndCondition after && after.OccurrenceCount < 1)
                throw new ArgumentOutOfRangeException(nameof(task.RecurrenceRule), "An after-occurrences condition must include at least one occurrence.");
            if (!Enum.IsDefined(task.ProgressionMode))
                throw new ArgumentOutOfRangeException(nameof(task.ProgressionMode));
        }

        /// <summary>
        /// Retrieves a task by its display ID and list ID.
        /// </summary>
        /// <param name="displayId">The display ID of the task.</param>
        /// <param name="listId">The ID of the list the task belongs to.</param>
        /// <returns>The task if found; otherwise, null.</returns>
        public TaskItem? GetTaskByDisplayId(int displayId, Guid listId)
        {
            return _data.Tasks.FirstOrDefault(t => t.DisplayId == displayId && t.ListId == listId);
        }

        /// <summary>
        /// Updates an existing task with new details.
        /// </summary>
        /// <param name="updatedTask">The updated task object.</param>
        /// <returns>True if the task was updated successfully; otherwise, false.</returns>
        public bool UpdateTask(TaskItem updatedTask)
        {
            if (string.IsNullOrWhiteSpace(updatedTask.Title))
            {
                throw new ArgumentException("Task title cannot be empty.");
            }
            var existingTask = _data.Tasks.Find(t => t.Id == updatedTask.Id);

            if (existingTask == null)
                return false;

            if (updatedTask.RecurrenceRule != null)
                ValidateRecurringTask(updatedTask);
            if (updatedTask.Dependencies.Any(dependencyId =>
                    _data.Tasks.Any(task => task.Id == dependencyId && task.RecurrenceRule != null)) ||
                (updatedTask.RecurrenceRule != null &&
                    _data.Tasks.Any(task => task.Dependencies.Contains(updatedTask.Id))))
            {
                throw new InvalidOperationException("Recurring task dependencies are not supported.");
            }
            if (_dependencyGraphHelper.WouldCreateCycle(_data.Tasks, updatedTask.Id, updatedTask.Dependencies))
                throw new InvalidOperationException("Circular dependency detected. Cannot update task with dependencies that create a cycle.");

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Link = updatedTask.Link;
            existingTask.Importance = updatedTask.Importance;
            existingTask.DueDate = updatedTask.DueDate;
            existingTask.NotBefore = updatedTask.NotBefore;
            existingTask.IsCompleted = updatedTask.IsCompleted;
            existingTask.Dependencies = new List<Guid>(updatedTask.Dependencies);

            // Update newly supported fields
            existingTask.EstimatedDuration = updatedTask.EstimatedDuration;
            existingTask.Complexity = updatedTask.Complexity;
            existingTask.IsPinned = updatedTask.IsPinned;
            if (updatedTask.RecurrenceRule == null)
            {
                existingTask.RecurrenceRule = null;
                existingTask.SeriesId = null;
                existingTask.OccurrenceStates ??= new List<TaskOccurrenceState>();
                existingTask.OccurrenceStates.Clear();
                existingTask.CurrentStreak = 0;
                existingTask.BestStreak = 0;
            }
            else
            {
                existingTask.RecurrenceRule = updatedTask.RecurrenceRule.Clone();
                existingTask.SeriesId = existingTask.Id;
                existingTask.ProgressionMode = updatedTask.ProgressionMode;
                existingTask.RequiredCompletions = updatedTask.RequiredCompletions;
                existingTask.ShowMissedIndicator = updatedTask.ShowMissedIndicator;
                existingTask.TrackStreak = updatedTask.TrackStreak;
                existingTask.IsCompleted = false;
            }

            // If critical scheduling parameters changed, we might want to clear the scheduled parts
            // so they don't persist in an invalid state until the next schedule run.
            // However, the system relies on the user running 'schedule', so we'll leave them as is
            // but ensure the task itself has the new values.
            
            SaveData();

            return true;
        }

        /// <summary>
        /// Deletes a task by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to delete.</param>
        /// <returns>True if the task was deleted successfully; otherwise, false.</returns>
        public bool DeleteTask(Guid id)
        {
            var task = _data.Tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            _data.Tasks.Remove(task);
            SaveData();
            return true;
        }

        /// <summary>
        /// Deletes tasks in bulk.
        /// </summary>
        /// <param name="tasksToDelete">The collection of TaskItem objects to delete.</param>
        public void DeleteTasks(IEnumerable<TaskItem> tasksToDelete)
        {
            var taskIdsToDelete = new HashSet<Guid>(tasksToDelete.Select(task => task.Id));
            _data.Tasks.RemoveAll(task => taskIdsToDelete.Contains(task.Id));
            SaveData();
        }

        /// <summary>
        /// Archives a single task by its unique ID: appends it to the persisted archive record, then
        /// removes it from the active task list.
        /// </summary>
        /// <param name="id">The unique ID of the task to archive.</param>
        /// <returns>True if the task was found and archived; otherwise, false.</returns>
        public bool ArchiveTask(Guid id)
        {
            var task = _data.Tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            task.ArchiveGroupId = null;
            _persistenceService.ArchiveTasks(new[] { task });
            _data.Tasks.Remove(task);
            SaveData();
            return true;
        }

        /// <summary>Archives a set of tasks as one restorable group.</summary>
        /// <param name="taskIds">The IDs of all tasks in the group.</param>
        /// <returns>True when every requested task was found and archived.</returns>
        public bool ArchiveTaskGroup(IEnumerable<Guid> taskIds)
        {
            var requestedIds = taskIds.Distinct().ToHashSet();
            if (requestedIds.Count == 0)
                return false;

            var tasks = _data.Tasks.Where(task => requestedIds.Contains(task.Id)).ToList();
            if (tasks.Count != requestedIds.Count)
                return false;

            var groupId = Guid.NewGuid();
            foreach (var task in tasks)
                task.ArchiveGroupId = groupId;

            _persistenceService.ArchiveTasks(tasks);
            _data.Tasks.RemoveAll(task => requestedIds.Contains(task.Id));
            SaveData();
            return true;
        }

        /// <summary>
        /// Retrieves the total count of tasks.
        /// </summary>
        /// <returns>The total number of tasks.</returns>
        public int GetTaskCount()
        {
            return _data.Tasks.Count;
        }

        /// <summary>
        /// Marks a task as complete by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to mark as complete.</param>
        /// <returns>True if the task was marked as complete; otherwise, false.</returns>
        public bool MarkTaskAsComplete(Guid id)
        {
            var task = _data.Tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            task.IsCompleted = true;
            SaveData();
            return true;
        }

        /// <summary>
        /// Marks a task as incomplete by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to mark as incomplete.</param>
        /// <returns>True if the task was marked as incomplete; otherwise, false.</returns>
        public bool MarkTaskAsIncomplete(Guid id)
        {
            var task = _data.Tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            task.IsCompleted = false;
            SaveData();
            return true;
        }

        /// <summary>
        /// Adds a new task list to the collection and saves the changes.
        /// </summary>
        /// <param name="list">The TaskList object to add.</param>
        public void AddList(TaskList list)
        {
            if (_data.Lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A list with the name '{list.Name}' already exists.");
            }
            ValidateWorkHours(
                list.WorkStartTime ?? _data.UserProfile.WorkStartTime,
                list.WorkEndTime ?? _data.UserProfile.WorkEndTime);
            list.ApplyDefaultsFrom(_data.UserProfile);
            list.Id = Guid.NewGuid();
            _data.Lists.Add(list);
            SaveData();
        }

        /// <summary>
        /// Retrieves a task list by its name.
        /// </summary>
        /// <param name="listName">The name of the task list.</param>
        /// <returns>The task list if found; otherwise, null.</returns>
        public TaskList? GetListByName(string listName)
        {
            return _data.Lists.FirstOrDefault(l => l.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retrieves a task list by its unique ID.
        /// </summary>
        /// <param name="listId">The identifier of the task list.</param>
        /// <returns>The task list if found; otherwise, null.</returns>
        public TaskList? GetListById(Guid listId)
        {
            return _data.Lists.FirstOrDefault(l => l.Id == listId);
        }

        /// <summary>
        /// Retrieves all task lists.
        /// </summary>
        /// <returns>An enumerable collection of task lists.</returns>
        public IEnumerable<TaskList> GetAllLists()
        {
            return new List<TaskList>(_data.Lists);
        }

        /// <summary>
        /// Deletes a task list by its name and removes associated tasks.
        /// If the last list is deleted, automatically recreates a 'General' list to maintain the invariant.
        /// </summary>
        /// <param name="listName">The name of the task list to delete.</param>
        /// <returns>True if the list was the last one and a new 'General' list was auto-created; otherwise, false.</returns>
        public bool DeleteList(string listName)
        {
            var listToDelete = _data.Lists.FirstOrDefault(list => list.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));
            if (listToDelete == null)
            {
                return false;
            }

            bool wasLastList = _data.Lists.Count == 1;

            _data.Lists.Remove(listToDelete);
            _data.Tasks.RemoveAll(task => task.ListId == listToDelete.Id);

            // If we just deleted the last list, recreate a new 'General' list to maintain the invariant
            if (wasLastList)
            {
                var newGeneralList = new TaskList { Id = Guid.NewGuid(), Name = "General" };
                newGeneralList.ApplyDefaultsFrom(_data.UserProfile);
                _data.Lists.Add(newGeneralList);
                _data.ActiveListId = newGeneralList.Id;
            }
            else if (listToDelete.Id == _data.ActiveListId)
            {
                // If we deleted the active list (but it wasn't the last), switch to the first remaining list
                _data.ActiveListId = _data.Lists.First().Id;
            }

            SaveData();
            return wasLastList;
        }

        /// <summary>
        /// Updates an existing task list with new details.
        /// </summary>
        /// <param name="updatedList">The updated task list object.</param>
        public void UpdateList(TaskList updatedList)
        {
            var existingList = updatedList.Id != Guid.Empty
                ? _data.Lists.FirstOrDefault(list => list.Id == updatedList.Id)
                : _data.Lists.FirstOrDefault(list => list.Name.Equals(updatedList.Name, StringComparison.OrdinalIgnoreCase));
            if (existingList != null)
            {
                ValidateWorkHours(
                    updatedList.WorkStartTime ?? _data.UserProfile.WorkStartTime,
                    updatedList.WorkEndTime ?? _data.UserProfile.WorkEndTime);

                if (_data.Lists.Any(list => list.Id != existingList.Id && list.Name.Equals(updatedList.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"A list with the name '{updatedList.Name}' already exists.");
                }

                existingList.Name = updatedList.Name;
                existingList.Description = updatedList.Description;
                existingList.SortOption = updatedList.SortOption;
                existingList.SchedulingMode = updatedList.SchedulingMode;
                existingList.WorkStartTime = updatedList.WorkStartTime;
                existingList.WorkEndTime = updatedList.WorkEndTime;
                existingList.WorkDays = updatedList.WorkDays == null ? null : new List<DayOfWeek>(updatedList.WorkDays);
                existingList.SlackThresholdDire = updatedList.SlackThresholdDire;
                existingList.SlackThresholdPressing = updatedList.SlackThresholdPressing;
                existingList.SlackThresholdFocus = updatedList.SlackThresholdFocus;
                existingList.SlackThresholdSafe = updatedList.SlackThresholdSafe;
                existingList.SimulatedTime = updatedList.SimulatedTime;
                if (updatedList.SimulatedTime.HasValue)
                {
                    existingList.LastSimulatedTime = updatedList.SimulatedTime;
                }
                SaveData();
            }
        }

        private static void ValidateWorkHours(TimeOnly startTime, TimeOnly endTime)
        {
            if (endTime <= startTime)
            {
                throw new ArgumentException("Work end time must be after its start time.", nameof(endTime));
            }
        }

        /// <summary>
        /// Builds an effective user profile for the specified list.
        /// </summary>
        /// <param name="list">The list to resolve.</param>
        /// <returns>A profile containing the active list's resolved settings.</returns>
        public UserProfile BuildEffectiveUserProfile(TaskList? list)
        {
            var effectiveProfile = new UserProfile
            {
                DefaultListSortOption = _data.UserProfile.DefaultListSortOption,
                DesiredBreatherDuration = _data.UserProfile.DesiredBreatherDuration,
                WorkStartTime = _data.UserProfile.WorkStartTime,
                WorkEndTime = _data.UserProfile.WorkEndTime,
                WorkDays = new List<DayOfWeek>(_data.UserProfile.WorkDays),
                SchedulingMode = _data.UserProfile.SchedulingMode,
                SlackThresholdDire = _data.UserProfile.SlackThresholdDire,
                SlackThresholdPressing = _data.UserProfile.SlackThresholdPressing,
                SlackThresholdFocus = _data.UserProfile.SlackThresholdFocus,
                SlackThresholdSafe = _data.UserProfile.SlackThresholdSafe
            };

            if (list == null)
            {
                return effectiveProfile;
            }

            effectiveProfile.WorkStartTime = list.WorkStartTime ?? effectiveProfile.WorkStartTime;
            effectiveProfile.WorkEndTime = list.WorkEndTime ?? effectiveProfile.WorkEndTime;
            effectiveProfile.WorkDays = list.WorkDays != null ? new List<DayOfWeek>(list.WorkDays) : new List<DayOfWeek>(effectiveProfile.WorkDays);
            effectiveProfile.SchedulingMode = list.SchedulingMode ?? effectiveProfile.SchedulingMode;
            effectiveProfile.SlackThresholdDire = list.SlackThresholdDire ?? effectiveProfile.SlackThresholdDire;
            effectiveProfile.SlackThresholdPressing = list.SlackThresholdPressing ?? effectiveProfile.SlackThresholdPressing;
            effectiveProfile.SlackThresholdFocus = list.SlackThresholdFocus ?? effectiveProfile.SlackThresholdFocus;
            effectiveProfile.SlackThresholdSafe = list.SlackThresholdSafe ?? effectiveProfile.SlackThresholdSafe;

            return effectiveProfile;
        }

        /// <summary>
        /// Applies the active list's saved time preference to the provided time service.
        /// </summary>
        /// <param name="listId">The list whose saved time should be applied.</param>
        /// <param name="timeService">The runtime time service.</param>
        public void ApplyListTimePreference(Guid listId, ITimeService timeService)
        {
            var list = GetListById(listId);
            if (list?.SimulatedTime.HasValue == true)
            {
                timeService.SetSimulatedTime(list.SimulatedTime.Value);
            }
            else
            {
                timeService.ClearSimulatedTime();
            }
        }

        private bool ApplyDefaultsIfNeeded(TaskList list)
        {
            var before = (
                list.Description,
                list.SortOption,
                list.SchedulingMode,
                list.WorkStartTime,
                list.WorkEndTime,
                list.WorkDays == null ? 0 : list.WorkDays.Count,
                list.SlackThresholdDire,
                list.SlackThresholdPressing,
                list.SlackThresholdFocus,
                list.SlackThresholdSafe);

            list.ApplyDefaultsFrom(_data.UserProfile);

            var after = (
                list.Description,
                list.SortOption,
                list.SchedulingMode,
                list.WorkStartTime,
                list.WorkEndTime,
                list.WorkDays == null ? 0 : list.WorkDays.Count,
                list.SlackThresholdDire,
                list.SlackThresholdPressing,
                list.SlackThresholdFocus,
                list.SlackThresholdSafe);

            return !before.Equals(after);
        }

        /// <summary>
        /// Archives the specified tasks to the archive file.
        /// </summary>
        /// <param name="tasksToArchive">The tasks to archive.</param>
        public void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive)
        {
            var tasks = tasksToArchive.ToList();
            var groupId = Guid.NewGuid();
            foreach (var task in tasks)
                task.ArchiveGroupId = groupId;
            _persistenceService.ArchiveTasks(tasks);
        }

        /// <summary>
        /// Retrieves all tasks currently held in the archive.
        /// </summary>
        public List<TaskItem> GetArchivedTasks()
        {
            return _persistenceService.GetArchivedTasks();
        }

        /// <summary>Builds grouped archive contents, including singleton groups for legacy items.</summary>
        public List<ArchiveGroup> GetArchiveGroups()
        {
            var taskGroups = _persistenceService.GetArchivedTasks()
                .GroupBy(task => task.ArchiveGroupId ?? task.Id)
                .Select(group => new ArchiveGroup(group.Key, "tasks", group.ToList(), Array.Empty<Event>()));
            var eventGroups = _persistenceService.GetArchivedEvents()
                .GroupBy(evt => evt.ArchiveGroupId ?? evt.SeriesId ?? evt.Id)
                .Select(group => new ArchiveGroup(group.Key, "events", Array.Empty<TaskItem>(), group.ToList()));
            return taskGroups.Concat(eventGroups).ToList();
        }

        /// <summary>
        /// Restores an archived task back to the active task list, defaulting to the task's original
        /// list if it still exists, or an explicit <paramref name="targetListId"/> otherwise.
        /// </summary>
        /// <param name="taskId">The ID of the archived task to restore.</param>
        /// <param name="targetListId">
        /// The list to restore the task into. Required when the task's original list no longer exists;
        /// otherwise optional (overrides the original list when supplied).
        /// </param>
        public RestoreArchivedTaskResult RestoreArchivedTask(Guid taskId, Guid? targetListId = null)
        {
            var archivedTask = _persistenceService.GetArchivedTasks().FirstOrDefault(t => t.Id == taskId);
            if (archivedTask == null)
            {
                return RestoreArchivedTaskResult.NotFound;
            }

            TaskList? targetList;
            if (targetListId.HasValue)
            {
                targetList = GetListById(targetListId.Value);
                if (targetList == null)
                {
                    return RestoreArchivedTaskResult.TargetListNotFound;
                }
            }
            else
            {
                targetList = GetListById(archivedTask.ListId);
                if (targetList == null)
                {
                    return RestoreArchivedTaskResult.ListRequired;
                }
            }

            _persistenceService.RemoveArchivedTask(taskId);
            archivedTask.ArchiveGroupId = null;
            archivedTask.ListId = targetList.Id;
            archivedTask.ListName = targetList.Name;
            archivedTask.DisplayId = _data.NextDisplayId++;
            _data.Tasks.Add(archivedTask);
            SaveData();
            return RestoreArchivedTaskResult.Restored;
        }

        /// <summary>Restores every archived task in a group, validating destinations before changing state.</summary>
        public RestoreArchivedTaskResult RestoreArchivedTaskGroup(Guid groupId, Guid? targetListId = null)
        {
            var archivedTasks = _persistenceService.GetArchivedTasks()
                .Where(task => (task.ArchiveGroupId ?? task.Id) == groupId)
                .ToList();
            if (archivedTasks.Count == 0)
                return RestoreArchivedTaskResult.NotFound;

            var targets = new List<(TaskItem Task, TaskList List)>();
            foreach (var task in archivedTasks)
            {
                var targetList = targetListId.HasValue
                    ? GetListById(targetListId.Value)
                    : GetListById(task.ListId);
                if (targetList == null)
                    return targetListId.HasValue
                        ? RestoreArchivedTaskResult.TargetListNotFound
                        : RestoreArchivedTaskResult.ListRequired;
                targets.Add((task, targetList));
            }

            _persistenceService.RemoveArchivedTasks(archivedTasks.Select(task => task.Id));
            foreach (var (task, targetList) in targets)
            {
                task.ArchiveGroupId = null;
                task.ListId = targetList.Id;
                task.ListName = targetList.Name;
                task.DisplayId = _data.NextDisplayId++;
                _data.Tasks.Add(task);
            }
            SaveData();
            return RestoreArchivedTaskResult.Restored;
        }

        /// <summary>
        /// Permanently deletes an archived task from the persisted archive record.
        /// </summary>
        /// <param name="taskId">The ID of the archived task to delete.</param>
        /// <returns>True if a matching archived task was found and deleted; otherwise false.</returns>
        public bool DeleteArchivedTask(Guid taskId)
        {
            return _persistenceService.RemoveArchivedTask(taskId);
        }

        /// <summary>
        /// Retrieves the ID of the currently active task list.
        /// </summary>
        /// <returns>The ID of the active list.</returns>
        public Guid GetActiveListId()
        {
            return _data.ActiveListId;
        }

        /// <summary>
        /// Sets the ID of the currently active task list.
        /// </summary>
        /// <param name="listId">The ID of the list to set as active.</param>
        public void SetActiveListId(Guid listId)
        {
            if (!_data.Lists.Any(list => list.Id == listId))
            {
                throw new ArgumentException($"List with ID {listId} does not exist.");
            }
            _data.ActiveListId = listId;
            SaveData();
        }

        // Event Management: delegates to IEventService (see docs/ARCHITECTURE_CORE.md "Key Services").
        public void AddEvent(Event newEvent) => _eventService.AddEvent(newEvent);

        public IEnumerable<Event> GetAllEvents() => _eventService.GetAllEvents();

        /// <summary>Retrieves concrete event occurrences within an inclusive date window.</summary>
        public IEnumerable<EventOccurrence> GetEventOccurrences(DateTime rangeStart, DateTime rangeEnd)
            => _eventService.GetEventOccurrences(rangeStart, rangeEnd);

        public Event? GetEvent(Guid id) => _eventService.GetEvent(id);

        public bool UpdateEvent(Event updatedEvent) => _eventService.UpdateEvent(updatedEvent);

        public bool DeleteEvent(Guid id) => _eventService.DeleteEvent(id);

        /// <summary>Archives an active event or recurring series.</summary>
        public bool ArchiveEvent(Guid id) => _eventService.ArchiveEvent(id);

        /// <summary>Archives the selected scope of a recurring event.</summary>
        public bool ArchiveEventOccurrence(Guid seriesId, DateTime occurrenceDate, RecurrenceEditTarget target)
            => _eventService.ArchiveOccurrence(seriesId, occurrenceDate, target);

        /// <summary>Retrieves all archived events.</summary>
        public List<Event> GetArchivedEvents() => _eventService.GetArchivedEvents();

        /// <summary>Restores an archived event or recurring series.</summary>
        public bool RestoreArchivedEvent(Guid eventId) => _eventService.RestoreArchivedEvent(eventId);

        /// <summary>Restores all archived events in the specified group.</summary>
        public bool RestoreArchivedEventGroup(Guid groupId) => _eventService.RestoreArchivedEventGroup(groupId);

        /// <summary>Permanently deletes an archived event or recurring series.</summary>
        public bool DeleteArchivedEvent(Guid eventId) => _persistenceService.RemoveArchivedEvent(eventId);

        /// <summary>Permanently removes all archived tasks and events.</summary>
        public void ClearArchive() => _persistenceService.ClearArchive();

        public void ClearEvents() => _eventService.ClearEvents();

        public bool EditEventOccurrence(Guid seriesId, DateTime occurrenceDate, string name, DateTime startTime, DateTime endTime, RecurrenceEditTarget target, RecurrenceRule? recurrenceRule = null, string description = "", string? link = null)
            => _eventService.EditOccurrence(seriesId, occurrenceDate, name, startTime, endTime, target, recurrenceRule, description, link);

        public bool DeleteEventOccurrence(Guid seriesId, DateTime occurrenceDate, RecurrenceEditTarget target)
            => _eventService.DeleteOccurrence(seriesId, occurrenceDate, target);
    }
}
