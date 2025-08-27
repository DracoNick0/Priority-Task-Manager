using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class EditHandler : ICommandHandler
    {
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
                HandleTargetedUpdate(service, id, args[1]);
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

        private void HandleTargetedUpdate(TaskManagerService service, int id, string attribute)
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
                    Console.Write($"New Title (default: {task.Title}): ");
                    task.Title = Console.ReadLine() ?? task.Title;
                    break;
                case "description":
                    Console.Write($"New Description (default: {task.Description}): ");
                    task.Description = Console.ReadLine() ?? task.Description;
                    break;
                case "importance":
                    Console.Write($"New Importance (1-10, default: {task.Importance}): ");
                    if (int.TryParse(Console.ReadLine(), out int importance) && importance >= 1 && importance <= 10)
                        task.Importance = importance;
                    break;
                case "duedate":
                    task.DueDate = ConsoleInputHelper.HandleInteractiveDateInput(task.DueDate);
                    break;
                case "duration":
                    Console.Write($"New Estimated Duration (hours, default: {task.EstimatedDuration.TotalHours}): ");
                    if (double.TryParse(Console.ReadLine(), out double duration) && duration > 0)
                        task.EstimatedDuration = TimeSpan.FromHours(duration);
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
