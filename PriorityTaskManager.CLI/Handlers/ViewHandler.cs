using PriorityTaskManager.Services;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Interfaces;
using System;
using System.Linq;

/// <summary>
/// Handles the 'view' command, displaying all details of a specific task by Id.
/// </summary>
public class ViewHandler : ICommandHandler
{
    /// <inheritdoc/>
    public void Execute(TaskManagerService service, string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out int id))
        {
            Console.WriteLine("Usage: view <Id>");
            return;
        }

        var task = service.GetTaskById(id);
        if (task == null)
        {
            Console.WriteLine("Task not found.");
            return;
        }

        Console.WriteLine($"\nTask Details (Id: {task.Id})");
        Console.WriteLine($"List: {task.ListName}");
        Console.WriteLine($"Title: {task.Title}");
        Console.WriteLine($"Description: {task.Description}");
        Console.WriteLine($"Importance: {task.Importance}");
        Console.WriteLine($"Due Date: {task.DueDate:yyyy-MM-dd HH:mm}");
        Console.WriteLine($"Completed: {(task.IsCompleted ? "Yes" : "No")}");
        Console.WriteLine($"Estimated Duration: {task.EstimatedDuration.TotalHours} hours");
        Console.WriteLine($"Progress: {task.Progress * 100:F1}%");
        Console.WriteLine($"Dependencies: {(task.Dependencies != null && task.Dependencies.Any() ? string.Join(", ", task.Dependencies) : "None")}");

        if (!task.IsCompleted)
        {
            Console.WriteLine($"Urgency Score: {task.UrgencyScore:F3}");
            Console.WriteLine($"Latest Possible Start Date: {task.LatestPossibleStartDate:yyyy-MM-dd}");
        }
    }
}
