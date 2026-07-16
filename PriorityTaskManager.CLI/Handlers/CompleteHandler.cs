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
    public class CompleteHandler : ICommandHandler, ICommandResultHandler
    {
        public CompleteHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
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
            var parseResult = ParseDisplayIds(service, args);
            if (parseResult.ValidTasks.Count == 0)
            {
                return new CommandResult
                {
                    Status = parseResult.HasInput ? CommandResultStatus.Warning : CommandResultStatus.Usage,
                    Message = BuildNoValidIdsMessage(parseResult),
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

            if (parseResult.InvalidTokens.Count > 0)
            {
                messageBuilder.AppendLine($"Ignored invalid IDs: {string.Join(", ", parseResult.InvalidTokens)}.");
            }

            if (parseResult.NotFoundDisplayIds.Count > 0)
            {
                messageBuilder.AppendLine($"Not found in active list: {string.Join(", ", parseResult.NotFoundDisplayIds)}.");
            }

            return new CommandResult
            {
                Status = failedDisplayIds.Count == 0 ? CommandResultStatus.Success : CommandResultStatus.Warning,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = completedDisplayIds.Count > 0
            };
        }

        private static string BuildNoValidIdsMessage(CompleteParseResult parseResult)
        {
            var messageBuilder = new StringBuilder();

            if (!parseResult.HasInput)
            {
                messageBuilder.AppendLine("No task IDs provided.");
            }
            else
            {
                messageBuilder.AppendLine("No valid task IDs provided.");
            }

            if (parseResult.InvalidTokens.Count > 0)
            {
                messageBuilder.AppendLine($"Invalid IDs: {string.Join(", ", parseResult.InvalidTokens)}.");
            }

            if (parseResult.NotFoundDisplayIds.Count > 0)
            {
                messageBuilder.AppendLine($"Not found in active list: {string.Join(", ", parseResult.NotFoundDisplayIds)}.");
            }

            messageBuilder.Append("Usage: complete <Id1>,<Id2>,...");
            return messageBuilder.ToString();
        }

        private static CompleteParseResult ParseDisplayIds(TaskManagerService service, string[] args)
        {
            var result = new CompleteParseResult();
            if (args == null || args.Length == 0)
            {
                return result;
            }

            result.HasInput = true;
            string input = string.Join(string.Empty, args);
            string[] potentialDisplayIds = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            int activeListId = service.GetActiveListId();

            foreach (var idString in potentialDisplayIds)
            {
                string trimmedId = idString.Trim();

                if (!int.TryParse(trimmedId, out int displayId))
                {
                    result.InvalidTokens.Add(trimmedId);
                    continue;
                }

                var task = service.GetTaskByDisplayId(displayId, activeListId);
                if (task == null)
                {
                    result.NotFoundDisplayIds.Add(displayId);
                    continue;
                }

                result.ValidTasks.Add((displayId, task.Id));
            }

            return result;
        }

        private sealed class CompleteParseResult
        {
            public bool HasInput { get; set; }

            public List<(int DisplayId, int RealId)> ValidTasks { get; } = new();

            public List<string> InvalidTokens { get; } = new();

            public List<int> NotFoundDisplayIds { get; } = new();
        }
}
}
