using System;
using System.Collections.Generic;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.Services
{
    public class UrgencyService : IUrgencyService
    {
        public void CalculateUrgencyForAllTasks(List<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                task.LatestPossibleStartDate = DateTime.MinValue;
                if (task.IsCompleted)
                {
                    task.UrgencyScore = 0;
                }
            }

            var today = DateTime.Today;
            var successorMap = new Dictionary<int, List<TaskItem>>();
            foreach (var task in tasks)
            {
                if (!successorMap.ContainsKey(task.Id))
                {
                    successorMap[task.Id] = new List<TaskItem>();
                }

                foreach (var depId in task.Dependencies)
                {
                    var depTask = tasks.FirstOrDefault(t => t.Id == depId);
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

            foreach (var task in tasks)
            {
                // Reset EffectiveImportance before calculation
                task.EffectiveImportance = task.Importance;
            }

            var visited = new HashSet<int>();
            foreach (var task in tasks)
            {
                CalculateLpsdRecursive(task, tasks, today, successorMap, visited);
            }
        }

        private int CalculateLpsdRecursive(TaskItem task, List<TaskItem> allTasks, DateTime today, Dictionary<int, List<TaskItem>> successorMap, HashSet<int> visited)
        {
            if (task.LatestPossibleStartDate != DateTime.MinValue || visited.Contains(task.Id) || task.IsCompleted)
            {
                return task.EffectiveImportance;
            }

            visited.Add(task.Id);

            // Calculate the actual work remaining on this task.
            double remainingWork = task.EstimatedDuration.TotalDays * (1 - task.Progress);

            var successors = successorMap[task.Id];
            DateTime lpsd;
            int maxSuccessorImportance = 0;
            if (successors.Count == 0)
            {
                lpsd = task.DueDate.AddDays(-remainingWork);
            }
            else
            {
                var successorImportances = new List<int>();
                foreach (var successor in successors)
                {
                    successorImportances.Add(CalculateLpsdRecursive(successor, allTasks, today, successorMap, visited));
                }
                maxSuccessorImportance = successorImportances.Any() ? successorImportances.Max() : 0;
                DateTime minSuccessorLpsd = successors.Min(s => s.LatestPossibleStartDate);
                lpsd = minSuccessorLpsd.AddDays(-remainingWork);
            }

            // Store the calculated values back in the task object.
            task.LatestPossibleStartDate = lpsd;
            double slackTime = (lpsd - today).TotalDays;
            if (slackTime < 0) slackTime = 0;
            int effectiveImportance = Math.Max(task.Importance, maxSuccessorImportance);
            task.EffectiveImportance = effectiveImportance;
            task.UrgencyScore = effectiveImportance * Math.Pow(1.0 / 4.0, slackTime - task.EstimatedDuration.TotalDays / 2.0);
            
            // We are done with this path, so we can remove it from the visited set for the current recursive stack.
            visited.Remove(task.Id);
            return effectiveImportance;
        }
    }
}
