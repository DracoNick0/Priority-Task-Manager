using System;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Services;
using PriorityTaskManager.Scheduling.GoldPanning;
using PriorityTaskManager.CLI.MCP.Agents.Cleanup;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class CleanupHandler : ICommandResultHandler
    {
        private readonly TaskManagerService _taskManagerService;

        public CleanupHandler(TaskManagerService taskManagerService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _taskManagerService = taskManagerService;
            // Snapshot/metrics dependencies intentionally retained in the constructor to avoid breaking
            // current wiring while this handler migrates toward Program-driven dashboard rendering.
        }

        /// <inheritdoc/>
        public CommandResult ExecuteWithResult(TaskManagerService taskManagerService, string[] args)
        {
            Console.WriteLine("WARNING: This will permanently delete all completed tasks and re-index all remaining task IDs. This action cannot be undone.");
            Console.Write("Type 'confirm' to proceed: ");
            var userInput = Console.ReadLine();

            if (!string.Equals(userInput, "confirm", StringComparison.OrdinalIgnoreCase))
            {
                return new CommandResult
                {
                    Status = CommandResultStatus.Warning,
                    Message = "Operation cancelled.",
                    ShouldRefreshDashboard = false
                };
            }

            var agentChain = new List<ISchedulingStage>
            {
                new FindCompletedTasksAgent(_taskManagerService),
                new ArchiveTasksAgent(_taskManagerService),
                new DeleteTasksAgent(_taskManagerService),
                new ReIndexTasksAgent(_taskManagerService),
                new UpdateDependenciesAgent(_taskManagerService)
            };

            var initialContext = new SchedulingContext();
            initialContext.History.Add("Cleanup command initiated by user.");

            var finalContext = PipelineCoordinator.Coordinate(agentChain, initialContext);

            var messageBuilder = new System.Text.StringBuilder();

            if (finalContext.LastError != null)
            {
                messageBuilder.AppendLine("An error occurred during the cleanup operation:");
                messageBuilder.AppendLine(finalContext.LastError.Message);
            }

            messageBuilder.AppendLine("Cleanup Operation Log:");
            foreach (var logEntry in finalContext.History)
            {
                messageBuilder.AppendLine(logEntry);
            }

            if (finalContext.LastError == null)
            {
                messageBuilder.Append("Cleanup complete.");
            }

            return new CommandResult
            {
                Status = finalContext.LastError == null ? CommandResultStatus.Success : CommandResultStatus.Error,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = finalContext.LastError == null
            };
        }
    }
}
