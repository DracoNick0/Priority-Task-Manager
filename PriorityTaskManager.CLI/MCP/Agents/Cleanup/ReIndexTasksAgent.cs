using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;
using System.Linq;
using System.Collections.Generic;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class ReIndexTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public ReIndexTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Re-indexing remaining tasks...");

            var remainingTasks = _taskManagerService.GetAllTasks();

            _taskManagerService.CalculateUrgencyForAllTasks();
            remainingTasks = remainingTasks.OrderByDescending(task => task.UrgencyScore).ToList();

            var idMap = new Dictionary<int, int>();
            int newDisplayId = 1;

            foreach (var task in remainingTasks)
            {
                idMap[task.Id] = newDisplayId;
                task.DisplayId = newDisplayId;
                newDisplayId++;
            }

            _taskManagerService.SaveTasks();

            context.SharedState["IdMap"] = idMap;
            context.History.Add($"Successfully re-indexed {idMap.Count} tasks.");

            return context;
        }
    }
}