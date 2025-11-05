using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'edit' command, allowing users to modify tasks or specific attributes of tasks.
    /// </summary>
    public class EditHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int id))
            {
                Console.WriteLine("Invalid arguments. Please provide a valid task ID.");
                return;
            }

            var existing = service.GetTaskById(id);

            if (existing == null)
            {
                Console.WriteLine("Task not found.");
                return;
            }

            if (args.Length > 1)
            {
                string attribute = args[1];
                string? directValue = null;
                if (args.Length > 2)
                {
                    directValue = string.Join(" ", args.Skip(2));
                }
                HandleTargetedUpdate(service, id, attribute, directValue);
                return;
            }

            // Full edit process
            Console.Write($"New Title (default: {existing.Title}): ");
            existing.Title = Console.ReadLine() ?? existing.Title;

            Console.Write($"New Description (default: {existing.Description}): ");
            existing.Description = Console.ReadLine() ?? existing.Description;

            Console.Write($"New Importance (1-10, default: {existing.Importance}): ");
            if (int.TryParse(Console.ReadLine(), out int importance) && importance >= 1 && importance <= 10)
                existing.Importance = importance;

            existing.DueDate = ConsoleInputHelper.HandleInteractiveDateInput(existing.DueDate);

            Console.Write($"New Estimated Duration (hours, default: {existing.EstimatedDuration.TotalHours}): ");
            if (double.TryParse(Console.ReadLine(), out double duration) && duration > 0)
                existing.EstimatedDuration = TimeSpan.FromHours(duration);

            service.UpdateTask(existing);

            Console.WriteLine("Task updated successfully.");
        }

    private void HandleTargetedUpdate(TaskManagerService service, int id, string attribute, string? directValue = null)
        {
            var task = service.GetTaskById(id);

            if (task == null)
            {
                Console.WriteLine("Task not found.");
                return;
            }

            switch (attribute.ToLower())
            {
                case "title":
                    if (directValue != null)
                        task.Title = directValue;
                    else
                    {
                        Console.Write($"New Title (default: {task.Title}): ");
                        task.Title = Console.ReadLine() ?? task.Title;
                    }
                    break;
                case "description":
                    if (directValue != null)
                        task.Description = directValue;
                    else
                    {
                        Console.Write($"New Description (default: {task.Description}): ");
                        task.Description = Console.ReadLine() ?? task.Description;
                    }
                    break;
                case "importance":
                    if (directValue != null)
                    {
                        if (int.TryParse(directValue, out int importance) && importance >= 1 && importance <= 10)
                            task.Importance = importance;
                        else
                            Console.WriteLine("Invalid importance value. Must be an integer between 1 and 10.");
                    }
                    else
                    {
                        Console.Write($"New Importance (1-10, default: {task.Importance}): ");
                        if (int.TryParse(Console.ReadLine(), out int importance) && importance >= 1 && importance <= 10)
                            task.Importance = importance;
                    }
                    break;
                case "complexity":
                    if (directValue != null)
                    {
                        if (double.TryParse(directValue, out double complexity) && complexity >= 1 && complexity <= 10)
                            task.Complexity = complexity;
                        else
                            Console.WriteLine("Invalid complexity value. Must be a number between 1 and 10.");
                    }
                    else
                    {
                        Console.Write($"New Complexity (1-10, default: {task.Complexity}): ");
                        var input = Console.ReadLine();
                        if (double.TryParse(input, out double complexity) && complexity >= 1 && complexity <= 10)
                            task.Complexity = complexity;
                        else
                            Console.WriteLine("Invalid complexity value. Must be a number between 1 and 10.");
                    }
                    break;
                case "pin":
                    task.IsPinned = !task.IsPinned;
                    Console.WriteLine($"Task is now {(task.IsPinned ? "pinned" : "unpinned") }.");
                    break;
                case "duedate":
                    if (directValue != null)
                        Console.WriteLine("Direct value editing for due date is not supported. Please use interactive editor.");
                    else
                        task.DueDate = ConsoleInputHelper.HandleInteractiveDateInput(task.DueDate);
                    break;
                case "duration":
                    if (directValue != null)
                    {
                        if (double.TryParse(directValue, out double duration) && duration > 0)
                            task.EstimatedDuration = TimeSpan.FromHours(duration);
                        else
                            Console.WriteLine("Invalid duration value. Must be a positive number.");
                    }
                    else
                    {
                        Console.Write($"New Estimated Duration (hours, default: {task.EstimatedDuration.TotalHours}): ");
                        if (double.TryParse(Console.ReadLine(), out double duration) && duration > 0)
                            task.EstimatedDuration = TimeSpan.FromHours(duration);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown attribute.");
                    return;
            }

            service.UpdateTask(task);

            Console.WriteLine("Task updated successfully.");
        }
    }
}
