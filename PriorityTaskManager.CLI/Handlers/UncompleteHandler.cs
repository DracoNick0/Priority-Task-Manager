using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using System.Text;

namespace PriorityTaskManager.CLI.Handlers
{
    public class UncompleteHandler : ICommandHandler, ICommandResultHandler
    {
        public UncompleteHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
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
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args, service.GetActiveListId());

            if (validTaskIds.Count == 0)
            {
                return new CommandResult
                {
                    Status = CommandResultStatus.Usage,
                    Message = "Usage: uncomplete <Id>,<Id2>,...",
                    ShouldRefreshDashboard = false
                };
            }

            var messageBuilder = new StringBuilder();
            var markedCount = 0;

            foreach (var id in validTaskIds)
            {
                if (service.MarkTaskAsIncomplete(id))
                {
                    messageBuilder.AppendLine($"Task {id} marked as incomplete.");
                    markedCount++;
                }
                else
                {
                    messageBuilder.AppendLine($"Task {id} not found.");
                }
            }

            return new CommandResult
            {
                Status = markedCount == validTaskIds.Count ? CommandResultStatus.Success : CommandResultStatus.Warning,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = markedCount > 0
            };
        }
    }
}
