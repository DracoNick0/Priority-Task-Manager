using System.Text.Json;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        private readonly IUrgencyStrategy _urgencyStrategy;
        private readonly IPersistenceService _persistenceService;
        private DataContainer _data;
        public UserProfile UserProfile => _data.UserProfile;

        /// <summary>
        /// Saves all current data to persistent storage.
        /// </summary>
        public void SaveAll()
        {
            SaveData();
        }

        /// <summary>
        /// Initializes a new instance of the TaskManagerService class with the given persistence and urgency strategies.
        /// </summary>
        /// <param name="urgencyStrategy">The urgency strategy used to calculate task urgency.</param>
        /// <param name="persistenceService">The persistence service for loading and saving data.</param>
        public TaskManagerService(IUrgencyStrategy urgencyStrategy, IPersistenceService persistenceService)
        {
            _urgencyStrategy = urgencyStrategy;
            _persistenceService = persistenceService;
            _data = _persistenceService.LoadData();
            // Ensure at least one default list exists
            if (_data.Lists == null || _data.Lists.Count == 0)
            {
                _data.Lists = new List<TaskList>
                {
                    new TaskList { Id = 1, Name = "General", SortOption = SortOption.Default }
                };
                _data.NextListId = 2;
            }
        }

        private void SaveData() => _persistenceService.SaveData(_data);

        /// <summary>
        /// Calculates urgency for all tasks using the urgency strategy.
        /// </summary>
        public void CalculateUrgencyForAllTasks()
        {
            _data.Tasks = _urgencyStrategy.CalculateUrgency(_data.Tasks);
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
            task.Id = _data.NextTaskId++;
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
        public IEnumerable<TaskItem> GetAllTasks(int listId)
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
        public TaskItem? GetTaskById(int id)
        {
            return _data.Tasks.Find(t => t.Id == id);
        }

        /// <summary>
        /// Retrieves a task by its display ID and list ID.
        /// </summary>
        /// <param name="displayId">The display ID of the task.</param>
        /// <param name="listId">The ID of the list the task belongs to.</param>
        /// <returns>The task if found; otherwise, null.</returns>
        public TaskItem? GetTaskByDisplayId(int displayId, int listId)
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

            if (WouldCreateCycle(updatedTask.Id, updatedTask.Dependencies))
                throw new InvalidOperationException("Circular dependency detected. Cannot update task with dependencies that create a cycle.");

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Importance = updatedTask.Importance;
            existingTask.DueDate = updatedTask.DueDate;
            existingTask.IsCompleted = updatedTask.IsCompleted;
            existingTask.Dependencies = new List<int>(updatedTask.Dependencies);

            SaveData();

            return true;
        }

        /// <summary>
        /// Deletes a task by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to delete.</param>
        /// <returns>True if the task was deleted successfully; otherwise, false.</returns>
        public bool DeleteTask(int id)
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
            var taskIdsToDelete = new HashSet<int>(tasksToDelete.Select(task => task.Id));
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
        public bool MarkTaskAsComplete(int id)
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
        public bool MarkTaskAsIncomplete(int id)
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
            list.Id = _data.NextListId++;
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
        /// Retrieves all task lists.
        /// </summary>
        /// <returns>An enumerable collection of task lists.</returns>
        public IEnumerable<TaskList> GetAllLists()
        {
            return new List<TaskList>(_data.Lists);
        }

        /// <summary>
        /// Deletes a task list by its name and removes associated tasks.
        /// </summary>
        /// <param name="listName">The name of the task list to delete.</param>
        public void DeleteList(string listName)
        {
            var listToDelete = _data.Lists.FirstOrDefault(list => list.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));
            if (listToDelete == null)
            {
                return;
            }
            _data.Lists.Remove(listToDelete);
            _data.Tasks.RemoveAll(task => task.ListId == listToDelete.Id);
            SaveData();
        }

        /// <summary>
        /// Updates an existing task list with new details.
        /// </summary>
        /// <param name="updatedList">The updated task list object.</param>
        public void UpdateList(TaskList updatedList)
        {
            var existingList = _data.Lists.FirstOrDefault(list => list.Name.Equals(updatedList.Name, StringComparison.OrdinalIgnoreCase));
            if (existingList != null)
            {
                existingList.SortOption = updatedList.SortOption;
                SaveData();
            }
        }

        // ...existing code...

        /// <summary>
        /// Checks if adding the given dependencies to the specified task would create a circular dependency.
        /// </summary>
        /// <param name="taskId">The ID of the task being updated.</param>
        /// <param name="newDependencies">The list of proposed new dependencies.</param>
        /// <returns>True if a cycle would be created; otherwise, false.</returns>
        private bool WouldCreateCycle(int taskId, List<int> newDependencies)
        {
            var visited = new HashSet<int>();
            foreach (var depId in newDependencies)
            {
                if (DetectCycleRecursive(taskId, depId, visited))
                    return true;
            }
            return false;
        }

        private bool DetectCycleRecursive(int originalTaskId, int currentId, HashSet<int> visited)
        {
            if (currentId == originalTaskId)
                return true;
            if (visited.Contains(currentId))
                return false;
            visited.Add(currentId);
            var currentTask = _data.Tasks.Find(t => t.Id == currentId);
            if (currentTask == null)
                return false;
            foreach (var depId in currentTask.Dependencies)
            {
                if (DetectCycleRecursive(originalTaskId, depId, visited))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if updating a task's dependencies would create a circular dependency.
        /// </summary>
        private bool WouldCreateCircularDependency(int taskId, List<int> newDependencies)
        {
            var visited = new HashSet<int>();
            foreach (var depId in newDependencies)
            {
                if (HasCycle(taskId, depId, visited))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Recursively checks for cycles in the dependency graph.
        /// </summary>
        private bool HasCycle(int originalTaskId, int currentId, HashSet<int> visited)
        {
            if (currentId == originalTaskId)
                return true;
            if (visited.Contains(currentId))
                return false;
            visited.Add(currentId);
            var currentTask = _data.Tasks.Find(t => t.Id == currentId);
            if (currentTask == null)
                return false;
            foreach (var depId in currentTask.Dependencies)
            {
                if (HasCycle(originalTaskId, depId, visited))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Archives the specified tasks to the archive file.
        /// </summary>
        /// <param name="tasksToArchive">The tasks to archive.</param>
        public void ArchiveTasks(IEnumerable<TaskItem> tasksToArchive)
        {
            // This method should be moved to PersistenceService in a future refactor.
            const string archiveFilePath = "archive.json";
            List<TaskItem> archivedTasks = new List<TaskItem>();
            if (File.Exists(archiveFilePath))
            {
                var existingData = File.ReadAllText(archiveFilePath);
                archivedTasks = JsonSerializer.Deserialize<List<TaskItem>>(existingData) ?? new List<TaskItem>();
            }
            archivedTasks.AddRange(tasksToArchive);
            var updatedData = JsonSerializer.Serialize(archivedTasks);
            File.WriteAllText(archiveFilePath, updatedData);
        }

        /// <summary>
        /// Retrieves the ID of the currently active task list.
        /// </summary>
        /// <returns>The ID of the active list.</returns>
        public int GetActiveListId()
        {
            // Placeholder implementation: Return the first list's ID or 0 if no lists exist.
            return _data.Lists.FirstOrDefault()?.Id ?? 0;
        }
    }
}
