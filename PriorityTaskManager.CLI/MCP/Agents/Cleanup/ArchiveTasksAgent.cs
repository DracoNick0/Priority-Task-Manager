using System;
using PriorityTaskManager.MCP;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class ArchiveTasksAgent : IAgent
    {
        private readonly TaskManagerService _taskManagerService;

        public ArchiveTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public MCPContext Act(MCPContext context)
        {
            var tasksToArchive = context.SharedState["CompletedTasks"] as List<TaskItem>;

            if (tasksToArchive == null || !tasksToArchive.Any())
            {
                context.History.Add("No tasks to archive.");
                return context;
            }

            context.History.Add($"Archiving {tasksToArchive.Count} tasks...");
            _taskManagerService.ArchiveTasks(tasksToArchive);
            context.History.Add("Tasks successfully archived.");

            return context;
        }
    }
}