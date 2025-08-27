using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class AddHandler : ICommandHandler
    {
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
            if (string.IsNullOrWhiteSpace(description)) description = string.Empty;
            Console.Write($"Importance (1-10, default: 5): ");
            var importanceInput = Console.ReadLine();
            int importance = 5;
            if (!string.IsNullOrWhiteSpace(importanceInput) && int.TryParse(importanceInput, out int imp) && imp >= 1 && imp <= 10) importance = imp;
            Console.Write($"Estimated Duration (hours, default: 1): ");
            var durationInput = Console.ReadLine();
            double durationHours = 1.0;
            if (!string.IsNullOrWhiteSpace(durationInput) && double.TryParse(durationInput, out double dur) && dur > 0) durationHours = dur;
            Console.WriteLine($"Due Date (use arrow keys to adjust, Enter to confirm):");
            var dueDate = ConsoleInputHelper.HandleInteractiveDateInput(DateTime.Today.AddDays(1));
            var task = new TaskItem
            {
                Title = title,
                Description = description,
                Importance = importance,
                DueDate = dueDate,
                IsCompleted = false,
                EstimatedDuration = TimeSpan.FromHours(durationHours),
                Progress = 0.0,
                Dependencies = new List<int>()
            };
            service.AddTask(task);
            Console.WriteLine($"Task added with Id {task.Id}.");
        }
    }
}
