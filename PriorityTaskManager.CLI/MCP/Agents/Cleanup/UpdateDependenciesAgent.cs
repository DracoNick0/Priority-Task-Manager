using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class UpdateDependenciesAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public UpdateDependenciesAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Updating dependency references...");

            var idMap = context.SharedState["IdMap"] as Dictionary<int, int>;

            if (idMap == null || idMap.Count == 0)
            {
                context.History.Add("No ID mapping found; skipping dependency updates.");
                return context;
            }

            var allTasks = _taskManagerService.GetAllTasks(_taskManagerService.GetActiveListId());
            int updatedCount = 0;

            foreach (var task in allTasks)
            {
                var newDependencies = new List<int>();

                foreach (var dependencyId in task.Dependencies)
                {
                    if (idMap.TryGetValue(dependencyId, out var newId))
                    {
                        newDependencies.Add(newId);
                    }
                }

                if (!newDependencies.SequenceEqual(task.Dependencies))
                {
                    task.Dependencies = newDependencies;
                    updatedCount++;
                }
            }

            _taskManagerService.SaveData();

            context.History.Add($"Scanned all tasks and updated {updatedCount} dependency references.");

            return context;
        }
    }
}