using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class TaskManagerService
    {
        private readonly List<TaskItem> _tasks = new List<TaskItem>();
        private int _nextId = 1;

        public void AddTask(TaskItem task)
        {
            task.Id = _nextId++;
            _tasks.Add(task);
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
            return true;
        }

        public bool DeleteTask(int id)
        {
            var task = _tasks.Find(t => t.Id == id);
            if (task == null)
                return false;
            _tasks.Remove(task);
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
            return true;
        }

        public void CalculateUrgencyForAllTasks()
        {
            // Ensure all tasks have a reset default value before calculation.
            // DateTime.MinValue is a good indicator that it hasn't been calculated yet.
            foreach (var task in _tasks)
            {
                task.LatestPossibleStartDate = DateTime.MinValue;
            }

            var today = DateTime.Today;

            // Step 1: Build a map of successors (who depends on me?)
            // Key: Task ID, Value: List of tasks that depend on the key task.
            var successorMap = new Dictionary<int, List<TaskItem>>();
            foreach (var task in _tasks)
            {
                // Initialize an empty list for every task in the map
                if (!successorMap.ContainsKey(task.Id))
                {
                    successorMap[task.Id] = new List<TaskItem>();
                }

                // For each dependency this task has, add this task to the dependency's successor list.
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
            // If LPSD has already been calculated (memoization), or if we are in a loop, exit.
            if (task.LatestPossibleStartDate != DateTime.MinValue || visited.Contains(task.Id))
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
                // --- BASE CASE ---
                // This is an "end task". No other tasks depend on it.
                // Its LPSD is based purely on its own due date.
                lpsd = task.DueDate.AddDays(-remainingWork);
            }
            else
            {
                // --- RECURSIVE STEP ---
                // First, ensure the LPSD is calculated for all successor tasks.
                foreach (var successor in successors)
                {
                    CalculateLpsdRecursive(successor, today, successorMap, visited);
                }

                // This task must be finished before the EARLIEST of its successors' start dates.
                // We find the minimum LPSD among all tasks that depend on this one.
                DateTime minSuccessorLpsd = successors.Min(s => s.LatestPossibleStartDate);

                // This task's LPSD is the earliest successor LPSD minus its own work.
                lpsd = minSuccessorLpsd.AddDays(-remainingWork);
            }

            // Store the calculated values back in the task object.
            task.LatestPossibleStartDate = lpsd;
            double slackTime = (lpsd - today).TotalDays;

            // Set a floor for slackTime to prevent division by a negative number if a task is already late.
            if (slackTime < 0) slackTime = 0;

            task.UrgencyScore = 1.0 / (slackTime + 1.0);
            
            // We are done with this path, so we can remove it from the visited set for the current recursive stack.
            visited.Remove(task.Id);
        }
    }
}
