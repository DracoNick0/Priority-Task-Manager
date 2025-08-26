using System;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI
{
	class Program
	{
		static void Main(string[] args)
		{
			var service = new TaskManagerService();
			Console.WriteLine("Priority Task Manager CLI (type 'help' for commands)");
			while (true)
			{
				Console.Write("\n> ");
				var line = Console.ReadLine();
				if (string.IsNullOrWhiteSpace(line)) continue;
				var parts = line.Trim().Split(' ', 2);
				var command = parts[0].ToLower(); // Case-insensitive command handling
				var argString = parts.Length > 1 ? parts[1] : string.Empty;
				var argsArr = argString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				switch (command)
				{
					case "add":
						HandleAddTask(service, argsArr);
						break;
					case "list":
						HandleViewAllTasks(service);
						break;
					case "edit":
						if (argsArr.Length > 0 && int.TryParse(argsArr[0], out int upId))
							HandleUpdateTask(service, upId);
						else Console.WriteLine("Usage: edit <Id>");
						break;
					case "delete":
						if (argsArr.Length > 0 && int.TryParse(argsArr[0], out int delId))
							HandleDeleteTask(service, delId);
						else Console.WriteLine("Usage: delete <Id>");
						break;
					case "complete":
						if (argsArr.Length > 0 && int.TryParse(argsArr[0], out int compId))
							HandleMarkTaskAsComplete(service, compId);
						else Console.WriteLine("Usage: complete <Id>");
						break;
					case "help":
						PrintHelp();
						break;
					case "exit":
						Console.WriteLine("Goodbye!");
						return;
					default:
						Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
						break;
				}
			}
		}

		private static void HandleAddTask(TaskManagerService service, string[] args)
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
			Console.Write($"Progress (0-100, default: 0): ");
			var progressInput = Console.ReadLine();
			double progress = 0.0;
			if (!string.IsNullOrWhiteSpace(progressInput) && double.TryParse(progressInput, out double prog) && prog >= 0 && prog <= 100) progress = prog;
			Console.Write($"Dependencies (comma-separated task IDs, default: none): ");
			var depsInput = Console.ReadLine();
			var deps = new List<int>();
			if (!string.IsNullOrWhiteSpace(depsInput))
			{
				foreach (var part in depsInput.Split(','))
				{
					if (int.TryParse(part.Trim(), out int depId))
						deps.Add(depId);
				}
			}
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
				Progress = progress / 100.0,
				Dependencies = deps
			};
			service.AddTask(task);
			Console.WriteLine($"Task added with Id {task.Id}.");
		}

		private static DateTime HandleInteractiveDateInput(DateTime initialDate)
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
					case ConsoleKey.Enter:
						Console.WriteLine();
						return date;
					case ConsoleKey.D:
						mode = IncrementMode.Day;
						break;
					case ConsoleKey.W:
						mode = IncrementMode.Week;
						break;
					case ConsoleKey.M:
						mode = IncrementMode.Month;
						break;
					case ConsoleKey.Y:
						mode = IncrementMode.Year;
						break;
				}
			}
		}
		private enum IncrementMode { Day, Week, Month, Year }

	private static void HandleViewAllTasks(TaskManagerService service)
		{
			service.CalculateUrgencyForAllTasks();
			var tasks = service.GetAllTasks().OrderByDescending(t => t.UrgencyScore).ToList();
			if (tasks.Count == 0)
			{
				Console.WriteLine("No tasks found.");
				return;
			}
			Console.WriteLine("\nAll Tasks (sorted by urgency):");
			foreach (var task in tasks)
			{
				Console.WriteLine($"Id: {task.Id}, Title: {task.Title}, Description: {task.Description}, Completed: {task.IsCompleted}, Urgency: {task.UrgencyScore:F3}, LPSD: {task.LatestPossibleStartDate:yyyy-MM-dd}");
			}
		}

		private static void HandleUpdateTask(TaskManagerService service, int id)
		{
			var existing = service.GetTaskById(id);
			if (existing == null)
			{
				Console.WriteLine("Task not found.");
				return;
			}
			Console.Write($"New Title (default: {existing.Title}): ");
			var title = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(title)) title = existing.Title;
			Console.Write($"New Description (default: {existing.Description}): ");
			var description = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(description)) description = existing.Description;
			Console.Write($"New Importance (1-10, default: {existing.Importance}): ");
			var importanceInput = Console.ReadLine();
			int importance = existing.Importance;
			if (!string.IsNullOrWhiteSpace(importanceInput) && int.TryParse(importanceInput, out int imp) && imp >= 1 && imp <= 10) importance = imp;
			Console.Write($"New Estimated Duration (hours, default: {existing.EstimatedDuration.TotalHours}): ");
			var durationInput = Console.ReadLine();
			double durationHours = existing.EstimatedDuration.TotalHours;
			if (!string.IsNullOrWhiteSpace(durationInput) && double.TryParse(durationInput, out double dur) && dur > 0) durationHours = dur;
			Console.Write($"New Progress (0-100, default: {existing.Progress * 100.0}): ");
			var progressInput = Console.ReadLine();
			double progress = existing.Progress * 100.0;
			if (!string.IsNullOrWhiteSpace(progressInput) && double.TryParse(progressInput, out double prog) && prog >= 0 && prog <= 100) progress = prog;
			Console.Write($"New Dependencies (comma-separated task IDs, default: {string.Join(",", existing.Dependencies)}): ");
			var depsInput = Console.ReadLine();
			var deps = new List<int>(existing.Dependencies);
			if (!string.IsNullOrWhiteSpace(depsInput))
			{
				deps.Clear();
				foreach (var part in depsInput.Split(','))
				{
					if (int.TryParse(part.Trim(), out int depId))
						deps.Add(depId);
				}
			}
			var dueDate = existing.DueDate;
			Console.Write("Press 'd' to edit the due date, or any other key to skip: ");
			var key = Console.ReadKey(true);
			if (key.Key == ConsoleKey.D)
			{
				Console.WriteLine();
				dueDate = HandleInteractiveDateInput(existing.DueDate);
			}
			else
			{
				Console.WriteLine();
			}
			var updated = new TaskItem
			{
				Id = id,
				Title = title,
				Description = description,
				Importance = importance,
				DueDate = dueDate,
				IsCompleted = existing.IsCompleted,
				EstimatedDuration = TimeSpan.FromHours(durationHours),
				Progress = progress / 100.0,
				Dependencies = deps
			};
			if (service.UpdateTask(updated))
				Console.WriteLine("Task updated successfully.");
			else
				Console.WriteLine("Update failed.");
		}

		private static void HandleDeleteTask(TaskManagerService service, int id)
		{
			if (service.DeleteTask(id))
				Console.WriteLine("Task deleted successfully.");
			else
				Console.WriteLine("Task not found.");
		}

		private static void HandleMarkTaskAsComplete(TaskManagerService service, int id)
		{
			if (service.MarkTaskAsComplete(id))
				Console.WriteLine("Task marked as complete.");
			else
				Console.WriteLine("Task not found.");
		}

		private static void PrintHelp()
		{
			Console.WriteLine("\nAvailable commands:");
			Console.WriteLine("add <Title>         - Add a new task (prompts for details)");
			Console.WriteLine("list                - List all tasks sorted by urgency");
			Console.WriteLine("edit <Id>           - Edit a task by Id");
			Console.WriteLine("delete <Id>         - Delete a task by Id");
			Console.WriteLine("complete <Id>       - Mark a task as complete");
			Console.WriteLine("help                - Show this help message");
			Console.WriteLine("exit                - Exit the application");
		}
	}
}
