using System.Text.Json;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        private readonly string _filePath;
        private readonly string _listFilePath;
        private List<TaskItem> _tasks = new List<TaskItem>();
        private List<TaskList> _lists = new List<TaskList>();
        private int _nextId = 1;
        private int _nextDisplayId = 1;
        private int _nextListId = 1;
        private readonly IUrgencyService _urgencyService;

        /// <summary>
        /// Initializes a new instance of the TaskManagerService class with specified file paths.
        /// </summary>
        /// <param name="urgencyService">The urgency service used to calculate task urgency.</param>
        /// <param name="tasksFilePath">The file path for storing tasks.</param>
        /// <param name="listsFilePath">The file path for storing lists.</param>
        public TaskManagerService(IUrgencyService urgencyService, string tasksFilePath, string listsFilePath)
        {
            _urgencyService = urgencyService;
            _filePath = Path.GetFullPath(tasksFilePath);
            _listFilePath = Path.GetFullPath(listsFilePath);
            LoadTasks();
            LoadLists();
        }

        /// <summary>
        /// Initializes a new instance of the TaskManagerService class with default file paths.
        /// </summary>
        /// <param name="urgencyService">The urgency service used to calculate task urgency.</param>
        public TaskManagerService(IUrgencyService urgencyService)
            : this(urgencyService,
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tasks.json"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "lists.json"))
        {
        }

        /// <summary>
        /// Calculates urgency for all tasks using the urgency service.
        /// </summary>
        public void CalculateUrgencyForAllTasks()
        {
            _urgencyService.CalculateUrgencyForAllTasks(_tasks);
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
            task.Id = _nextId++;
            task.EffectiveImportance = task.Importance;
            task.DisplayId = _nextDisplayId++;

            // ListId should already be set by CLI layer. Do not set ListName here.
            _tasks.Add(task);
            SaveTasks();
        }

        /// <summary>
        /// Retrieves all tasks associated with a specific list ID.
        /// </summary>
        /// <param name="listId">The ID of the list.</param>
        /// <returns>An enumerable collection of tasks.</returns>
        public IEnumerable<TaskItem> GetAllTasks(int listId)
        {
            return _tasks.Where(task => task.ListId == listId);
        }

        public List<TaskItem> GetAllTasks()
        {
            // Placeholder implementation
            return new List<TaskItem>();
        }

        /// <summary>
        /// Retrieves a task by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task.</param>
        /// <returns>The task if found; otherwise, null.</returns>
        public TaskItem? GetTaskById(int id)
        {
            return _tasks.Find(t => t.Id == id);
        }

        /// <summary>
        /// Retrieves a task by its display ID and list ID.
        /// </summary>
        /// <param name="displayId">The display ID of the task.</param>
        /// <param name="listId">The ID of the list the task belongs to.</param>
        /// <returns>The task if found; otherwise, null.</returns>
        public TaskItem? GetTaskByDisplayId(int displayId, int listId)
        {
            return _tasks.FirstOrDefault(t => t.DisplayId == displayId && t.ListId == listId);
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
            var existingTask = _tasks.Find(t => t.Id == updatedTask.Id);

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

            SaveTasks();

            return true;
        }

        /// <summary>
        /// Deletes a task by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to delete.</param>
        /// <returns>True if the task was deleted successfully; otherwise, false.</returns>
        public bool DeleteTask(int id)
        {
            var task = _tasks.Find(t => t.Id == id);

            if (task == null)
                return false;

            _tasks.Remove(task);

            SaveTasks();

            return true;
        }

        /// <summary>
        /// Deletes tasks in bulk.
        /// </summary>
        /// <param name="tasksToDelete">The collection of TaskItem objects to delete.</param>
        public void DeleteTasks(IEnumerable<TaskItem> tasksToDelete)
        {
            var taskIdsToDelete = new HashSet<int>(tasksToDelete.Select(task => task.Id));

            _tasks.RemoveAll(task => taskIdsToDelete.Contains(task.Id));

            SaveTasks();
        }

        /// <summary>
        /// Retrieves the total count of tasks.
        /// </summary>
        /// <returns>The total number of tasks.</returns>
        public int GetTaskCount()
        {
            return _tasks.Count;
        }

        /// <summary>
        /// Marks a task as complete by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to mark as complete.</param>
        /// <returns>True if the task was marked as complete; otherwise, false.</returns>
        public bool MarkTaskAsComplete(int id)
        {
            var task = _tasks.Find(t => t.Id == id);

            if (task == null)
                return false;

            task.IsCompleted = true;

            SaveTasks();

            return true;
        }

        /// <summary>
        /// Marks a task as incomplete by its unique ID.
        /// </summary>
        /// <param name="id">The unique ID of the task to mark as incomplete.</param>
        /// <returns>True if the task was marked as incomplete; otherwise, false.</returns>
        public bool MarkTaskAsIncomplete(int id)
        {
            var task = _tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            task.IsCompleted = false;
            SaveTasks();
            return true;
        }

        /// <summary>
        /// Adds a new task list to the collection and saves the changes.
        /// </summary>
        /// <param name="list">The TaskList object to add.</param>
        public void AddList(TaskList list)
        {
            if (_lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A list with the name '{list.Name}' already exists.");
            }
            list.Id = _nextListId++;
            _lists.Add(list);
            SaveLists();
        }

        /// <summary>
        /// Retrieves a task list by its name.
        /// </summary>
        /// <param name="listName">The name of the task list.</param>
        /// <returns>The task list if found; otherwise, null.</returns>
        public TaskList? GetListByName(string listName)
        {
            return _lists.FirstOrDefault(l => l.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Retrieves all task lists.
        /// </summary>
        /// <returns>An enumerable collection of task lists.</returns>
        public IEnumerable<TaskList> GetAllLists()
        {
            return new List<TaskList>(_lists);
        }

        /// <summary>
        /// Deletes a task list by its name and removes associated tasks.
        /// </summary>
        /// <param name="listName">The name of the task list to delete.</param>
        public void DeleteList(string listName)
        {
            var listToDelete = _lists.FirstOrDefault(list => list.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));

            if (listToDelete == null)
            {
                return;
            }

            _lists.Remove(listToDelete);
            _tasks.RemoveAll(task => task.ListId == listToDelete.Id);

            SaveLists();
            SaveTasks();
        }

        /// <summary>
        /// Updates an existing task list with new details.
        /// </summary>
        /// <param name="updatedList">The updated task list object.</param>
        public void UpdateList(TaskList updatedList)
        {
            var existingList = _lists.FirstOrDefault(list => list.Name.Equals(updatedList.Name, StringComparison.OrdinalIgnoreCase));
            if (existingList != null)
            {
                existingList.SortOption = updatedList.SortOption;
                SaveLists();
            }
        }

        private void LoadTasks()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                    if (data != null && data.ContainsKey("Tasks") && data.ContainsKey("NextId"))
                    {
                        var rawTasks = data["Tasks"].GetRawText();
                        var loadedTasks = new List<TaskItem>();
                        try
                        {
                            loadedTasks = JsonSerializer.Deserialize<List<TaskItem>>(rawTasks) ?? new List<TaskItem>();
                        }
                        catch (ArgumentException)
                        {
                            // Skip all tasks if deserialization fails due to invalid title
                            loadedTasks = new List<TaskItem>();
                        }
                        // Initialize EffectiveImportance for loaded tasks
                        foreach (var t in loadedTasks)
                        {
                            t.EffectiveImportance = t.Importance;
                        }
                        _tasks = loadedTasks;
                        _nextId = data["NextId"].GetInt32();
                        _nextDisplayId = data["NextDisplayId"].GetInt32();
                    }
                }
                catch
                {
                    // If any error occurs, skip loading tasks
                    _tasks = new List<TaskItem>();
                    _nextId = 1;
                    _nextDisplayId = 1;
                }
            }
        }

        public void SaveTasks()
        {
            var data = new
            {
                Tasks = _tasks,
                NextId = _nextId,
                NextDisplayId = _nextDisplayId
            };

            File.WriteAllText(_filePath, JsonSerializer.Serialize(data));
        }

        private void LoadLists()
        {
            if (File.Exists(_listFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_listFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (data != null && data.ContainsKey("Lists"))
                    {
                        var rawLists = data["Lists"].GetRawText();
                        _lists = JsonSerializer.Deserialize<List<TaskList>>(rawLists) ?? new List<TaskList>();
                        if (_lists.Count > 0)
                        {
                            _nextListId = _lists.Max(l => l.Id) + 1;
                        }
                        else
                        {
                            _nextListId = 1;
                        }
                        if (data.ContainsKey("NextListId"))
                        {
                            _nextListId = Math.Max(_nextListId, data["NextListId"].GetInt32());
                        }
                    }
                    else
                    {
                        _lists = new List<TaskList>();
                        _nextListId = 1;
                    }
                }
                catch
                {
                    _lists = new List<TaskList>();
                    _nextListId = 1;
                }
            }
            else
            {
                var defaultList = new TaskList { Id = _nextListId++, Name = "General", SortOption = SortOption.Default };
                _lists.Add(defaultList);
                SaveLists();
            }
        }

        private void SaveLists()
        {
            var data = new
            {
                Lists = _lists,
                NextListId = _nextListId
            };
            File.WriteAllText(_listFilePath, JsonSerializer.Serialize(data));
        }

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
            var currentTask = _tasks.Find(t => t.Id == currentId);
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
            var currentTask = _tasks.Find(t => t.Id == currentId);
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
            return _lists.FirstOrDefault()?.Id ?? 0;
        }
    }
}
