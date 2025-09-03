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

        public TaskManagerService()
        {
            // Always save tasks.json in the solution root (four levels above bin/Debug/netX.X)
            _filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tasks.json"));
            _listFilePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "lists.json"));
            LoadTasks();
            LoadLists();
        }


        private void SaveTasks()
        {
            var data = new
            {
                Tasks = _tasks,
                NextId = _nextId
            };

            File.WriteAllText(_filePath, JsonSerializer.Serialize(data));
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
                        _tasks = loadedTasks;
                        _nextId = data["NextId"].GetInt32();
                    }
                }
                catch
                {
                    // If any error occurs, skip loading tasks
                    _tasks = new List<TaskItem>();
                    _nextId = 1;
                }
            }
        }

        private void LoadLists()
        {
            if (File.Exists(_listFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_listFilePath);
                    _lists = JsonSerializer.Deserialize<List<TaskList>>(json) ?? new List<TaskList>();
                }
                catch
                {
                    _lists = new List<TaskList>();
                }
            }
            else
            {
                var defaultList = new TaskList { Name = "General", SortOption = SortOption.Default };
                _lists.Add(defaultList);
                SaveLists();
            }
        }

        private void SaveLists()
        {
            File.WriteAllText(_listFilePath, JsonSerializer.Serialize(_lists));
        }

        public void AddTask(TaskItem task)
        {
            task.Id = _nextId++;
            task.ListName = "General"; // Default list for now

            _tasks.Add(task);

            SaveTasks();
        }


        public IEnumerable<TaskItem> GetAllTasks(string listName)
        {
            return _tasks.Where(task => task.ListName.Equals(listName, StringComparison.OrdinalIgnoreCase));
        }


        public TaskItem? GetTaskById(int id)
        {
            return _tasks.Find(t => t.Id == id);
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


        public bool UpdateTask(TaskItem updatedTask)
        {
            var existingTask = _tasks.Find(t => t.Id == updatedTask.Id);

            if (existingTask == null)
                return false;

            // Check for circular dependencies before updating
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


        public bool DeleteTask(int id)
        {
            var task = _tasks.Find(t => t.Id == id);

            if (task == null)
                return false;

            _tasks.Remove(task);

            SaveTasks();

            return true;
        }


        public int GetTaskCount()
        {
            return _tasks.Count;
        }


        public bool MarkTaskAsComplete(int id)
        {
            var task = _tasks.Find(t => t.Id == id);

            if (task == null)
                return false;

            task.IsCompleted = true;

            SaveTasks();

            return true;
        }


        public bool MarkTaskAsIncomplete(int id)
        {
            var task = _tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            task.IsCompleted = false;
            SaveTasks();
            return true;
        }

        public void CalculateUrgencyForAllTasks()
        {
            foreach (var task in _tasks)
            {
                task.LatestPossibleStartDate = DateTime.MinValue;
                if (task.IsCompleted)
                {
                    task.UrgencyScore = 0;
                }
            }

            var today = DateTime.Today;
            var successorMap = new Dictionary<int, List<TaskItem>>();
            foreach (var task in _tasks)
            {
                if (!successorMap.ContainsKey(task.Id))
                {
                    successorMap[task.Id] = new List<TaskItem>();
                }

                foreach (var depId in task.Dependencies)
                {
                    var depTask = _tasks.FirstOrDefault(t => t.Id == depId);
                    if (depTask != null)
                    {
                        if (!successorMap.ContainsKey(depTask.Id))
                        {
                            successorMap[depTask.Id] = new List<TaskItem>();
                        }
                        successorMap[depTask.Id].Add(task);
                    }
                }
            }

            // Step 2: Recursively calculate LPSD for all tasks.
            // A visited set prevents infinite loops in case of circular dependencies.
            var visited = new HashSet<int>();
            foreach (var task in _tasks)
            {
                CalculateLpsdRecursive(task, today, successorMap, visited);
            }
        }

        private void CalculateLpsdRecursive(TaskItem task, DateTime today, Dictionary<int, List<TaskItem>> successorMap, HashSet<int> visited)
        {
            if (task.LatestPossibleStartDate != DateTime.MinValue || visited.Contains(task.Id) || task.IsCompleted)
            {
                return;
            }

            visited.Add(task.Id);

            // Calculate the actual work remaining on this task.
            double remainingWork = task.EstimatedDuration.TotalDays * (1 - task.Progress);

            var successors = successorMap[task.Id];
            DateTime lpsd;

            if (successors.Count == 0)
            {
                lpsd = task.DueDate.AddDays(-remainingWork);
            }
            else
            {
                foreach (var successor in successors)
                {
                    CalculateLpsdRecursive(successor, today, successorMap, visited);
                }

                DateTime minSuccessorLpsd = successors.Min(s => s.LatestPossibleStartDate);
                lpsd = minSuccessorLpsd.AddDays(-remainingWork);
            }

            // Store the calculated values back in the task object.
            task.LatestPossibleStartDate = lpsd;
            double slackTime = (lpsd - today).TotalDays;

            if (slackTime < 0) slackTime = 0;

            task.UrgencyScore = 1.0 / (slackTime + 1.0);
            
            // We are done with this path, so we can remove it from the visited set for the current recursive stack.
            visited.Remove(task.Id);
        }

        public void AddList(TaskList list)
        {
            if (_lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A list with the name '{list.Name}' already exists.");
            }

            _lists.Add(list);
            SaveLists();
        }

        public TaskList? GetListByName(string listName)
        {
            return _lists.FirstOrDefault(l => l.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<TaskList> GetAllLists()
        {
            return new List<TaskList>(_lists);
        }

        public void DeleteList(string listName)
        {
            var listToDelete = _lists.FirstOrDefault(list => list.Name.Equals(listName, StringComparison.OrdinalIgnoreCase));

            if (listToDelete == null)
            {
                return;
            }

            _lists.Remove(listToDelete);
            _tasks.RemoveAll(task => task.ListName.Equals(listName, StringComparison.OrdinalIgnoreCase));

            SaveLists();
            SaveTasks();
        }

        public void UpdateList(TaskList updatedList)
        {
            var existingList = _lists.FirstOrDefault(list => list.Name.Equals(updatedList.Name, StringComparison.OrdinalIgnoreCase));
            if (existingList != null)
            {
                existingList.SortOption = updatedList.SortOption;
                SaveLists();
            }
        }
    }
}
