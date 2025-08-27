using System.Text.Json;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        private readonly string _filePath = "tasks.json";
        private List<TaskItem> _tasks = new List<TaskItem>();
        private int _nextId = 1;

        public TaskManagerService()
        {
            LoadTasks();
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
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (data != null && data.ContainsKey("Tasks") && data.ContainsKey("NextId"))
                {
                    _tasks = JsonSerializer.Deserialize<List<TaskItem>>(data["Tasks"].GetRawText()) ?? new List<TaskItem>();

                    _nextId = data["NextId"].GetInt32();
                }
            }
        }


        public void AddTask(TaskItem task)
        {
            task.Id = _nextId++;

            _tasks.Add(task);

            SaveTasks();
        }


        public IEnumerable<TaskItem> GetAllTasks()
        {
            return new List<TaskItem>(_tasks);
        }


        public TaskItem? GetTaskById(int id)
        {
            return _tasks.Find(t => t.Id == id);
        }


        public bool UpdateTask(TaskItem updatedTask)
        {
            var existingTask = _tasks.Find(t => t.Id == updatedTask.Id);

            if (existingTask == null)
                return false;

            existingTask.Title = updatedTask.Title;
            existingTask.Description = updatedTask.Description;
            existingTask.Importance = updatedTask.Importance;
            existingTask.DueDate = updatedTask.DueDate;
            existingTask.IsCompleted = updatedTask.IsCompleted;

            SaveTasks();

            return true;
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
    }
}
