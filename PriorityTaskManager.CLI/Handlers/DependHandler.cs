using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

/// <summary>
/// Handles the 'depend' command, allowing users to add or remove dependencies between tasks.
/// </summary>
public class DependHandler : ICommandHandler
{
    private readonly ScheduleSnapshotProvider _snapshotProvider;
    private readonly ITaskMetricsService _taskMetricsService;

    public DependHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
    {
        _snapshotProvider = snapshotProvider;
        _taskMetricsService = taskMetricsService;
    }

    /// <summary>
    /// Executes the 'depend' command, routing to add or remove dependency logic.
    /// </summary>
    public void Execute(TaskManagerService service, string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return;
        }

        var subcommand = args[0].ToLower();
        if (subcommand == "add")
        {
            HandleAddDependency(service, args);
        }
        else if (subcommand == "remove")
        {
            HandleRemoveDependency(service, args);
        }
        else
        {
            PrintUsage();
        }
    }

    private void HandleAddDependency(TaskManagerService service, string[] args)
    {
        _snapshotProvider.RefreshActiveListSnapshot(out _);
        ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
        
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: depend add <childId> <parentId>");
            return;
        }

        if (!int.TryParse(args[1], out int childDisplayId) || !int.TryParse(args[2], out int parentDisplayId))
        {
            Console.WriteLine("Both childId and parentId must be valid integers.");
            return;
        }

        if (childDisplayId == parentDisplayId)
        {
            Console.WriteLine("A task cannot depend on itself.");
            return;
        }

        var childTask = service.GetTaskByDisplayId(childDisplayId, service.GetActiveListId());
        var parentTask = service.GetTaskByDisplayId(parentDisplayId, service.GetActiveListId());
        if (childTask == null || parentTask == null)
        {
            Console.WriteLine("One or both tasks do not exist.");
            return;
        }

        if (childTask.Dependencies.Contains(parentTask.Id))
        {
            Console.WriteLine($"Task {childDisplayId} already depends on task {parentDisplayId}.");
            return;
        }

        childTask.Dependencies.Add(parentTask.Id);
        try
        {
            service.UpdateTask(childTask);
            Console.WriteLine($"Added dependency: Task {childDisplayId} now depends on Task {parentDisplayId}.");
        }
        catch (InvalidOperationException)
        {
            childTask.Dependencies.Remove(parentTask.Id); // Rollback
            Console.WriteLine("Error: This action would create a circular dependency and was rejected.");
        }
    }

    private void HandleRemoveDependency(TaskManagerService service, string[] args)
    {
        _snapshotProvider.RefreshActiveListSnapshot(out _);
        ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);

        if (args.Length != 3)
        {
            Console.WriteLine("Usage: depend remove <childId> <parentId>");
            return;
        }

        if (!int.TryParse(args[1], out int childDisplayId) || !int.TryParse(args[2], out int parentDisplayId))
        {
            Console.WriteLine("Both childId and parentId must be valid integers.");
            return;
        }

        var childTask = service.GetTaskByDisplayId(childDisplayId, service.GetActiveListId());
        var parentTask = service.GetTaskByDisplayId(parentDisplayId, service.GetActiveListId());
        
        if (childTask == null)
        {
            Console.WriteLine($"{childDisplayId} does not exist.");
        }

        if (parentTask == null)
        {
            Console.WriteLine($"{parentDisplayId} does not exist.");
        }

        if (childTask == null || parentTask == null)
        {
            return;
        }

        if (!childTask.Dependencies.Contains(parentTask.Id))
        {
            Console.WriteLine($"Task {childDisplayId} does not depend on task {parentDisplayId}.");
            return;
        }

        childTask.Dependencies.Remove(parentTask.Id);
        service.UpdateTask(childTask);
        Console.WriteLine($"Removed dependency: Task {childDisplayId} no longer depends on Task {parentDisplayId}.");
    }

    private void PrintUsage()
    {
        _snapshotProvider.RefreshActiveListSnapshot(out _);
        ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);

        Console.WriteLine("Usage:");
        Console.WriteLine("  depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
        Console.WriteLine("  depend remove <childId> <parentId> - Remove a dependency");
    }
}
