using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class DeleteTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public DeleteTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            var tasksToDelete = context.SharedState["CompletedTasks"] as List<TaskItem>;

            if (tasksToDelete == null || !tasksToDelete.Any())
            {
                context.History.Add("No tasks to delete.");
                return context;
            }

            context.History.Add($"Deleting {tasksToDelete.Count} tasks from the active list...");
            _taskManagerService.DeleteTasks(tasksToDelete);
            context.History.Add("Tasks successfully deleted from the active list.");

            return context;
        }
    }
}