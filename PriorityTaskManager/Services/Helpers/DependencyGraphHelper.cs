using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services.Helpers
{
    public class DependencyGraphHelper
    {
        public List<TaskItem> GetFullChain(List<TaskItem> allTasks, int startTaskId)
        {
            var visitedTaskIds = new HashSet<int>();
            var tasksToProcess = new Queue<int>();
            var chainTasks = new HashSet<TaskItem>();
            tasksToProcess.Enqueue(startTaskId);

            while (tasksToProcess.Count > 0)
            {
                var currentTaskId = tasksToProcess.Dequeue();
                if (!visitedTaskIds.Add(currentTaskId))
                    continue;
                var currentTask = allTasks.FirstOrDefault(t => t.Id == currentTaskId);
                if (currentTask == null)
                    continue;
                chainTasks.Add(currentTask);
                // Go up: prerequisites
                foreach (var depId in currentTask.Dependencies)
                {
                    tasksToProcess.Enqueue(depId);
                }
                // Go down: dependents
                foreach (var dependent in allTasks)
                {
                    if (dependent.Dependencies.Contains(currentTaskId))
                        tasksToProcess.Enqueue(dependent.Id);
                }
            }
            return chainTasks.ToList();
        }
    }
}
