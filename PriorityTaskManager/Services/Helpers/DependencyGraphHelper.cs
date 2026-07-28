using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services.Helpers
{
    public class DependencyGraphHelper
    {
        public List<TaskItem> GetFullChain(List<TaskItem> allTasks, Guid startTaskId)
        {
            var visitedTaskIds = new HashSet<Guid>();
            var tasksToProcess = new Queue<Guid>();
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

        /// <summary>
        /// Checks if adding the given dependencies to the specified task would create a circular dependency.
        /// </summary>
        /// <param name="allTasks">All tasks to search for dependency chains within.</param>
        /// <param name="taskId">The ID of the task being updated.</param>
        /// <param name="newDependencies">The list of proposed new dependencies.</param>
        /// <returns>True if a cycle would be created; otherwise, false.</returns>
        public bool WouldCreateCycle(List<TaskItem> allTasks, Guid taskId, List<Guid> newDependencies)
        {
            var visited = new HashSet<Guid>();
            foreach (var depId in newDependencies)
            {
                if (DetectCycleRecursive(allTasks, taskId, depId, visited))
                    return true;
            }
            return false;
        }

        private bool DetectCycleRecursive(List<TaskItem> allTasks, Guid originalTaskId, Guid currentId, HashSet<Guid> visited)
        {
            if (currentId == originalTaskId)
                return true;
            if (visited.Contains(currentId))
                return false;
            visited.Add(currentId);
            var currentTask = allTasks.FirstOrDefault(t => t.Id == currentId);
            if (currentTask == null)
                return false;
            foreach (var depId in currentTask.Dependencies)
            {
                if (DetectCycleRecursive(allTasks, originalTaskId, depId, visited))
                    return true;
            }
            return false;
        }
    }
}
