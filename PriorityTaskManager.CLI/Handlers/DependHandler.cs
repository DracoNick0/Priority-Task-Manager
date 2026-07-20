using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

/// <summary>
/// Handles the 'depend' command, allowing users to add or remove dependencies between tasks.
/// </summary>
public class DependHandler : ICommandHandler, ICommandResultHandler
{
    public DependHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
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
        if (args.Length < 1)
        {
            return UsageResult();
        }

        var subcommand = args[0].ToLower();
        if (subcommand == "add")
        {
            return HandleAddDependency(service, args);
        }
        else if (subcommand == "remove")
        {
            return HandleRemoveDependency(service, args);
        }
        else
        {
            return UsageResult();
        }
    }

    private static CommandResult HandleAddDependency(TaskManagerService service, string[] args)
    {
        if (args.Length != 3)
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Usage,
                Message = "Usage: depend add <childId> <parentId>",
                ShouldRefreshDashboard = false
            };
        }

        if (!int.TryParse(args[1], out int childDisplayId) || !int.TryParse(args[2], out int parentDisplayId))
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = "Both childId and parentId must be valid integers.",
                ShouldRefreshDashboard = false
            };
        }

        if (childDisplayId == parentDisplayId)
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = "A task cannot depend on itself.",
                ShouldRefreshDashboard = false
            };
        }

        var childTask = service.GetTaskByDisplayId(childDisplayId, service.GetActiveListId());
        var parentTask = service.GetTaskByDisplayId(parentDisplayId, service.GetActiveListId());
        if (childTask == null || parentTask == null)
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = "One or both tasks do not exist.",
                ShouldRefreshDashboard = false
            };
        }

        if (childTask.Dependencies.Contains(parentTask.Id))
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = $"Task {childDisplayId} already depends on task {parentDisplayId}.",
                ShouldRefreshDashboard = false
            };
        }

        childTask.Dependencies.Add(parentTask.Id);
        try
        {
            service.UpdateTask(childTask);
            return new CommandResult
            {
                Status = CommandResultStatus.Success,
                Message = $"Added dependency: Task {childDisplayId} now depends on Task {parentDisplayId}.",
                ShouldRefreshDashboard = true
            };
        }
        catch (InvalidOperationException)
        {
            childTask.Dependencies.Remove(parentTask.Id); // Rollback
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = "Error: This action would create a circular dependency and was rejected.",
                ShouldRefreshDashboard = false
            };
        }
    }

    private static CommandResult HandleRemoveDependency(TaskManagerService service, string[] args)
    {
        if (args.Length != 3)
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Usage,
                Message = "Usage: depend remove <childId> <parentId>",
                ShouldRefreshDashboard = false
            };
        }

        if (!int.TryParse(args[1], out int childDisplayId) || !int.TryParse(args[2], out int parentDisplayId))
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = "Both childId and parentId must be valid integers.",
                ShouldRefreshDashboard = false
            };
        }

        var childTask = service.GetTaskByDisplayId(childDisplayId, service.GetActiveListId());
        var parentTask = service.GetTaskByDisplayId(parentDisplayId, service.GetActiveListId());

        if (childTask == null || parentTask == null)
        {
            var messageBuilder = new System.Text.StringBuilder();
            if (childTask == null)
            {
                messageBuilder.AppendLine($"{childDisplayId} does not exist.");
            }
            if (parentTask == null)
            {
                messageBuilder.AppendLine($"{parentDisplayId} does not exist.");
            }

            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = false
            };
        }

        if (!childTask.Dependencies.Contains(parentTask.Id))
        {
            return new CommandResult
            {
                Status = CommandResultStatus.Warning,
                Message = $"Task {childDisplayId} does not depend on task {parentDisplayId}.",
                ShouldRefreshDashboard = false
            };
        }

        childTask.Dependencies.Remove(parentTask.Id);
        service.UpdateTask(childTask);
        return new CommandResult
        {
            Status = CommandResultStatus.Success,
            Message = $"Removed dependency: Task {childDisplayId} no longer depends on Task {parentDisplayId}.",
            ShouldRefreshDashboard = true
        };
    }

    private static CommandResult UsageResult()
    {
        return new CommandResult
        {
            Status = CommandResultStatus.Usage,
            Message = "Usage:\n  depend add <childId> <parentId>    - Add a dependency (child depends on parent)\n  depend remove <childId> <parentId> - Remove a dependency",
            ShouldRefreshDashboard = false
        };
    }
}
