using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;

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
            var dueDate = HandleInteractiveDateInput(DateTime.Today.AddDays(1));
            var task = new TaskItem
            {
                Title = title,
                Description = description,
                Importance = importance,
                DueDate = dueDate,
                IsCompleted = false,
                EstimatedDuration = TimeSpan.FromHours(durationHours),
                Progress = 0.0, // Default value
                Dependencies = new List<int>() // Default value
            };
            service.AddTask(task);
            Console.WriteLine($"Task added with Id {task.Id}.");
        }

        private DateTime HandleInteractiveDateInput(DateTime initialDate)
        {
            DateTime date = initialDate;
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            IncrementMode mode = IncrementMode.Day;
            while (true)
            {
                Console.SetCursorPosition(left, top);
                Console.Write($"[Mode: {mode}] {date:yyyy-MM-dd dddd}      ");
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.RightArrow:
                        switch (mode)
                        {
                            case IncrementMode.Day: date = date.AddDays(1); break;
                            case IncrementMode.Week: date = date.AddDays(7); break;
                            case IncrementMode.Month: date = date.AddMonths(1); break;
                            case IncrementMode.Year: date = date.AddYears(1); break;
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        switch (mode)
                        {
                            case IncrementMode.Day: date = date.AddDays(-1); break;
                            case IncrementMode.Week: date = date.AddDays(-7); break;
                            case IncrementMode.Month: date = date.AddMonths(-1); break;
                            case IncrementMode.Year: date = date.AddYears(-1); break;
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        mode = (IncrementMode)(((int)mode + 1) % 4);
                        break;
                    case ConsoleKey.DownArrow:
                        mode = (IncrementMode)(((int)mode + 3) % 4);
                        break;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return date;
                }
            }
        }

        private enum IncrementMode { Day, Week, Month, Year }
    }
}
