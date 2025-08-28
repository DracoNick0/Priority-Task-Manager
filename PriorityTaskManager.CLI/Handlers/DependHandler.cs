using PriorityTaskManager.Services;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Interfaces;
using System;
using System.Linq;

/// <summary>
/// Handles the 'depend' command, allowing users to add or remove dependencies between tasks.
/// </summary>
public class DependHandler : ICommandHandler
{
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
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: depend add <childId> <parentId>");
            return;
        }

        if (!int.TryParse(args[1], out int childId) || !int.TryParse(args[2], out int parentId))
        {
            Console.WriteLine("Both childId and parentId must be valid integers.");
            return;
        }

        if (childId == parentId)
        {
            Console.WriteLine("A task cannot depend on itself.");
            return;
        }

        var childTask = service.GetTaskById(childId);
        var parentTask = service.GetTaskById(parentId);
        if (childTask == null || parentTask == null)
        {
            Console.WriteLine("One or both task IDs do not exist.");
            return;
        }

        if (childTask.Dependencies.Contains(parentId))
        {
            Console.WriteLine($"Task {childId} already depends on task {parentId}.");
            return;
        }

        childTask.Dependencies.Add(parentId);
        try
        {
            service.UpdateTask(childTask);
            Console.WriteLine($"Added dependency: Task {childId} now depends on Task {parentId}.");
        }
        catch (InvalidOperationException)
        {
            childTask.Dependencies.Remove(parentId); // Rollback
            Console.WriteLine("Error: This action would create a circular dependency and was rejected.");
        }
    }

    private void HandleRemoveDependency(TaskManagerService service, string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: depend remove <childId> <parentId>");
            return;
        }

        if (!int.TryParse(args[1], out int childId) || !int.TryParse(args[2], out int parentId))
        {
            Console.WriteLine("Both childId and parentId must be valid integers.");
            return;
        }

        var childTask = service.GetTaskById(childId);
        var parentTask = service.GetTaskById(parentId);
        if (childTask == null || parentTask == null)
        {
            Console.WriteLine("One or both task IDs do not exist.");
            return;
        }

        if (!childTask.Dependencies.Contains(parentId))
        {
            Console.WriteLine($"Task {childId} does not depend on task {parentId}.");
            return;
        }

        childTask.Dependencies.Remove(parentId);
        service.UpdateTask(childTask);
        Console.WriteLine($"Removed dependency: Task {childId} no longer depends on Task {parentId}.");
    }

    private void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
        Console.WriteLine("  depend remove <childId> <parentId> - Remove a dependency");
    }
}
