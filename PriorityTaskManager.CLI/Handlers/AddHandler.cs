using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'add' command, allowing users to add new tasks to the task manager.
    /// </summary>
    public class AddHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: add <Title>");
                return;
            }

            var title = string.Join(" ", args);

            Console.Write($"Description (default: empty): ");
            var description = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(description))
                description = string.Empty;

            Console.Write($"Importance (1-10, default: 5): ");
            var importanceInput = Console.ReadLine();
            int importance = 5;
            if (!string.IsNullOrWhiteSpace(importanceInput) && int.TryParse(importanceInput, out int imp) && imp >= 1 && imp <= 10)
                importance = imp;

            Console.Write($"Estimated Duration (hours, default: 1): ");
            var durationInput = Console.ReadLine();
            double durationHours = 1.0;
            if (!string.IsNullOrWhiteSpace(durationInput) && double.TryParse(durationInput, out double dur) && dur > 0)
                durationHours = dur;

            Console.WriteLine($"Due Date (use arrow keys to adjust, Enter to confirm):");
            var dueDate = ConsoleInputHelper.HandleInteractiveDateInput(DateTime.Today.AddDays(1));

            // Prompt for dependencies
            Console.WriteLine("Dependencies (optional): Enter comma-separated IDs of tasks this depends on, or press Enter to skip.");
            var depInput = Console.ReadLine();
            var dependencies = new List<int>();
            if (!string.IsNullOrWhiteSpace(depInput))
            {
                var depStrings = depInput.Split(',');
                foreach (var depStr in depStrings)
                {
                    var trimmed = depStr.Trim();
                    if (int.TryParse(trimmed, out int depId))
                    {
                        var depTask = service.GetTaskById(depId);
                        if (depTask != null)
                        {
                            dependencies.Add(depId);
                        }
                        else
                        {
                            Console.WriteLine($"Warning: No task found with ID {depId}. Ignored.");
                        }
                    }
                    else if (!string.IsNullOrEmpty(trimmed))
                    {
                        Console.WriteLine($"Warning: '{trimmed}' is not a valid ID. Ignored.");
                    }
                }
            }

            var task = new TaskItem
            {
                Title = title,
                Description = description,
                Importance = importance,
                DueDate = dueDate,
                IsCompleted = false,
                EstimatedDuration = TimeSpan.FromHours(durationHours),
                Progress = 0.0,
                Dependencies = dependencies,
                ListName = Program.ActiveListName // Set the task's list to the active list
            };

            service.AddTask(task);

            Console.WriteLine($"Task added with Id {task.Id}.");
        }
    }
}
