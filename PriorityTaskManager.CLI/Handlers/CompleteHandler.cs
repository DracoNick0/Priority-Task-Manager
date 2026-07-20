using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using System.Text;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'complete' command.
    /// Marks one or more tasks as complete using their display IDs.
    /// </summary>
    public class CompleteHandler : ICommandResultHandler
    {
        public CompleteHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            // Dependencies intentionally retained in the constructor to avoid breaking current wiring
            // while this handler migrates toward Program-driven dashboard rendering.
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
                    Message = NonInteractiveCommandResultHelper.BuildNoValidIdsMessage(parseResult, "Usage: complete <Id1>,<Id2>,..."),
                    ShouldRefreshDashboard = false
                };
            }

            var completedDisplayIds = new List<int>();
            var failedDisplayIds = new List<int>();

            foreach (var taskRef in parseResult.ValidTasks)
            {
                if (service.MarkTaskAsComplete(taskRef.RealId))
                {
                    completedDisplayIds.Add(taskRef.DisplayId);
                }
                else
                {
                    failedDisplayIds.Add(taskRef.DisplayId);
                }
            }

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Completed {completedDisplayIds.Count} task(s): {string.Join(", ", completedDisplayIds)}.");

            if (failedDisplayIds.Count > 0)
            {
                messageBuilder.AppendLine($"Failed to complete {failedDisplayIds.Count} task(s): {string.Join(", ", failedDisplayIds)}.");
            }

            NonInteractiveCommandResultHelper.AppendParseWarnings(messageBuilder, parseResult);

            return new CommandResult
            {
                Status = failedDisplayIds.Count == 0 ? CommandResultStatus.Success : CommandResultStatus.Warning,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = completedDisplayIds.Count > 0
            };
        }
    }
}
