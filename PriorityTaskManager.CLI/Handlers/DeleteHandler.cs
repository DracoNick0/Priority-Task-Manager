using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using System.Text;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'delete' command.
    /// Permanently removes one or more tasks from the active list using their display IDs.
    /// </summary>
    public class DeleteHandler : ICommandHandler, ICommandResultHandler
    {
        public DeleteHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            // Dependencies intentionally retained in the constructor to avoid breaking current wiring
            // while this handler migrates toward Program-driven dashboard rendering.
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            var result = ExecuteWithResult(service, args);
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                Console.WriteLine(result.Message);
            }
        }

        /// <inheritdoc/>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            var parseResult = NonInteractiveCommandResultHelper.ParseDisplayIds(service, args);
            if (parseResult.ValidTasks.Count == 0)
            {
                return new CommandResult
                {
                    Status = parseResult.HasInput ? CommandResultStatus.Warning : CommandResultStatus.Usage,
                    Message = NonInteractiveCommandResultHelper.BuildNoValidIdsMessage(parseResult, "Usage: delete <Id1>,<Id2>,..."),
                    ShouldRefreshDashboard = false
                };
            }

            var deletedDisplayIds = new List<int>();
            var failedDisplayIds = new List<int>();

            foreach (var taskRef in parseResult.ValidTasks)
            {
                if (service.DeleteTask(taskRef.RealId))
                {
                    deletedDisplayIds.Add(taskRef.DisplayId);
                }
                else
                {
                    failedDisplayIds.Add(taskRef.DisplayId);
                }
            }

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Deleted {deletedDisplayIds.Count} task(s): {string.Join(", ", deletedDisplayIds)}.");

            if (failedDisplayIds.Count > 0)
            {
                messageBuilder.AppendLine($"Failed to delete {failedDisplayIds.Count} task(s): {string.Join(", ", failedDisplayIds)}.");
            }

            NonInteractiveCommandResultHelper.AppendParseWarnings(messageBuilder, parseResult);

            return new CommandResult
            {
                Status = failedDisplayIds.Count == 0 ? CommandResultStatus.Success : CommandResultStatus.Warning,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = deletedDisplayIds.Count > 0
            };
        }
    }
}

