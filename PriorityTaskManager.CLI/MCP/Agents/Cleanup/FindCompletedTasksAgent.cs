using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class FindCompletedTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public FindCompletedTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            context.History.Add("Finding all completed tasks...");

            var allTasks = _taskManagerService.GetAllTasks();
            var completedTasks = allTasks.Where(task => task.IsCompleted).ToList();

            context.SharedState["CompletedTasks"] = completedTasks;
            context.History.Add($"Found {completedTasks.Count} completed tasks to process.");

            return context;
        }
    }
}