using System;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Services;
using PriorityTaskManager.MCP;
using PriorityTaskManager.CLI.MCP.Agents.Cleanup;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class CleanupHandler : ICommandHandler
    {
        private readonly TaskManagerService _taskManagerService;
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public CleanupHandler(TaskManagerService taskManagerService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _taskManagerService = taskManagerService;
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        public void Handle(string[] args)
        {
            Console.WriteLine("WARNING: This will permanently delete all completed tasks and re-index all remaining task IDs. This action cannot be undone.");
            Console.Write("Type 'confirm' to proceed: ");
            var userInput = Console.ReadLine();

            if (!string.Equals(userInput, "confirm", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Operation cancelled.");
                return;
            }

            var agentChain = new List<IAgent>
            {
                new FindCompletedTasksAgent(_taskManagerService),
                new ArchiveTasksAgent(_taskManagerService),
                new DeleteTasksAgent(_taskManagerService),
                new ReIndexTasksAgent(_taskManagerService),
                new UpdateDependenciesAgent(_taskManagerService)
            };

            var initialContext = new MCPContext();
            initialContext.History.Add("Cleanup command initiated by user.");

            var finalContext = PriorityTaskManager.MCP.MCP.Coordinate(agentChain, initialContext);

            if (finalContext.LastError != null)
            {
                Console.WriteLine("An error occurred during the cleanup operation:");
                Console.WriteLine(finalContext.LastError.Message);
            }

            if (finalContext.LastError == null)
            {
                _snapshotProvider.RefreshActiveListSnapshot(out _);
                ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            }

            Console.WriteLine("Cleanup Operation Log:");
            foreach (var logEntry in finalContext.History)
            {
                Console.WriteLine(logEntry);
            }

            if (finalContext.LastError == null)
            {
                Console.WriteLine("Cleanup complete.");
            }
        }

        public void Execute(TaskManagerService taskManagerService, string[] args)
        {
            Handle(args);
        }
    }
}