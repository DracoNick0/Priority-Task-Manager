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
                strategy = new PriorityTaskManager.Scheduling.GoldPanning.GoldPanningStrategy(effectiveProfile, _data.Events, timeService);
            }
            
            var rawTasks = GetAllTasks(listId).ToList();

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
            task.Id = Guid.NewGuid();
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

            if (_dependencyGraphHelper.WouldCreateCycle(_data.Tasks, updatedTask.Id, updatedTask.Dependencies))
                throw new InvalidOperationException("Circular dependency detected. Cannot update task with dependencies that create a cycle.");

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Importance = updatedTask.Importance;
            existingTask.DueDate = updatedTask.DueDate;
            existingTask.NotBefore = updatedTask.NotBefore;
            existingTask.IsCompleted = updatedTask.IsCompleted;
            existingTask.Dependencies = new List<Guid>(updatedTask.Dependencies);

            // Update newly supported fields
            existingTask.EstimatedDuration = updatedTask.EstimatedDuration;
            existingTask.Complexity = updatedTask.Complexity;
            existingTask.IsPinned = updatedTask.IsPinned;

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
                SaveData();
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
            _persistenceService.ArchiveTasks(tasksToArchive);
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

        public Event? GetEvent(Guid id) => _eventService.GetEvent(id);

        public bool UpdateEvent(Event updatedEvent) => _eventService.UpdateEvent(updatedEvent);

        public bool DeleteEvent(Guid id) => _eventService.DeleteEvent(id);

        public void ClearEvents() => _eventService.ClearEvents();
    }
}
