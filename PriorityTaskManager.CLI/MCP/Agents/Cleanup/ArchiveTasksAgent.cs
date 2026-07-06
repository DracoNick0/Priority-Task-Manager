using System;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.Services;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.CLI.MCP.Agents.Cleanup
{
    public class ArchiveTasksAgent : ISchedulingStage
    {
        private readonly TaskManagerService _taskManagerService;

        public ArchiveTasksAgent(TaskManagerService taskManagerService)
        {
            _taskManagerService = taskManagerService;
        }

        public SchedulingContext Act(SchedulingContext context)
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