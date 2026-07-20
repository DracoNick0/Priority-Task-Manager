using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Services;
using System.Text;

/// <summary>
/// Handles the 'view' command, displaying all details of a specific task by Id.
/// </summary>
public class ViewHandler : ICommandHandler, ICommandResultHandler
{
    public ViewHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
    {
        // Dependencies intentionally retained in the constructor to avoid breaking current wiring while this handler migrates toward Program-driven dashboard rendering.
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
        if (args.Length != 1 || !int.TryParse(args[0], out int id))
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Usage,
                Message = "Usage: view <Id>",
                ShouldRefreshDashboard = true
            };
        }

        var task = service.GetTaskByDisplayId(id, service.GetActiveListId());
        if (task == null)
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = "Task not found.",
                ShouldRefreshDashboard = true
            };
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine();
        messageBuilder.AppendLine($"Task Details (Id: {task.DisplayId})");
        messageBuilder.AppendLine($"List: {task.ListName}");
        messageBuilder.AppendLine($"Title: {task.Title}");
        messageBuilder.AppendLine($"Description: {task.Description}");
        messageBuilder.AppendLine($"Importance: {task.Importance}");
        messageBuilder.AppendLine($"Due Date: {task.DueDate:yyyy-MM-dd HH:mm}");
        messageBuilder.AppendLine($"Completed: {(task.IsCompleted ? "Yes" : "No")}");
        messageBuilder.AppendLine($"Estimated Duration: {task.EstimatedDuration.TotalHours} hours");
        messageBuilder.AppendLine($"Progress: {task.Progress * 100:F1}%");
        messageBuilder.AppendLine($"Dependencies: {(task.Dependencies != null && task.Dependencies.Any() ? string.Join(", ", task.Dependencies) : "None")}");

        if (!task.IsCompleted)
        {
            messageBuilder.AppendLine($"Urgency Score: {task.UrgencyScore:F3}");
            messageBuilder.AppendLine($"Latest Possible Start Date: {task.LatestPossibleStartDate:yyyy-MM-dd}");
        }

        return new CommandResult
        {
            Status = CommandResultStatus.Success,
            Message = messageBuilder.ToString().TrimEnd(),
            ShouldRefreshDashboard = true
        };
    }
}
