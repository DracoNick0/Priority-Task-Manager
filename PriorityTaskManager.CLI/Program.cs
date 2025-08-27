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
						if (argsArr.Length == 1 && int.TryParse(argsArr[0], out int editId))
						{
							HandleUpdateTask(service, editId);
						}
						else if (argsArr.Length == 2 && int.TryParse(argsArr[1], out int targetEditId))
						{
							HandleUpdateTask(service, targetEditId, argsArr[0]);
						}
						else
						{
							Console.WriteLine("Usage: edit <Id> or edit <attribute> <Id>");
						}
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
					case "uncomplete":
						if (argsArr.Length > 0 && int.TryParse(argsArr[0], out int incomId))
							HandleMarkTaskAsIncomplete(service, incomId);
						else Console.WriteLine("Usage: uncomplete <Id>");
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
				var checkbox = task.IsCompleted ? "[x]" : "[ ]";
				if (task.IsCompleted)
				{
					Console.WriteLine($"{checkbox} Id: {task.Id}, Title: {task.Title}");
				}
				else
				{
					Console.WriteLine($"{checkbox} Id: {task.Id}, Title: {task.Title}, Urgency: {task.UrgencyScore:F3}, LPSD: {task.LatestPossibleStartDate:yyyy-MM-dd}");
				}
			}
		}

		private static void HandleUpdateTask(TaskManagerService service, int id, string? attribute = null)
		{
			var existing = service.GetTaskById(id);
			if (existing == null)
			{
				Console.WriteLine("Task not found.");
				return;
			}

			if (!string.IsNullOrEmpty(attribute))
			{
				HandleTargetedUpdate(service, id, attribute);
				return;
			}

			// Full edit process
			Console.Write($"New Title (default: {existing.Title}): ");
			existing.Title = Console.ReadLine() ?? existing.Title;
			Console.Write($"New Description (default: {existing.Description}): ");
			existing.Description = Console.ReadLine() ?? existing.Description;
			Console.Write($"New Importance (1-10, default: {existing.Importance}): ");
			if (int.TryParse(Console.ReadLine(), out int importance) && importance >= 1 && importance <= 10) existing.Importance = importance;
			existing.DueDate = HandleInteractiveDateInput(existing.DueDate);
			Console.Write($"New Estimated Duration (hours, default: {existing.EstimatedDuration.TotalHours}): ");
			if (double.TryParse(Console.ReadLine(), out double duration) && duration > 0) existing.EstimatedDuration = TimeSpan.FromHours(duration);
			Console.Write($"New Progress (0-100, default: {existing.Progress * 100.0}): ");
			if (double.TryParse(Console.ReadLine(), out double progress) && progress >= 0 && progress <= 100) existing.Progress = progress / 100.0;
			service.UpdateTask(existing);
			Console.WriteLine("Task updated.");
		}

		private static void HandleTargetedUpdate(TaskManagerService service, int id, string attribute)
		{
			var existing = service.GetTaskById(id);
			if (existing == null)
			{
				Console.WriteLine("Task not found.");
				return;
			}

			switch (attribute.ToLower())
			{
				case "title":
					Console.Write($"New Title (default: {existing.Title}): ");
					existing.Title = Console.ReadLine() ?? existing.Title;
					break;
				case "desc":
					Console.Write($"New Description (default: {existing.Description}): ");
					existing.Description = Console.ReadLine() ?? existing.Description;
					break;
				case "importance":
					Console.Write($"New Importance (1-10, default: {existing.Importance}): ");
					if (int.TryParse(Console.ReadLine(), out int imp) && imp >= 1 && imp <= 10) existing.Importance = imp;
					break;
				case "due":
					existing.DueDate = HandleInteractiveDateInput(existing.DueDate);
					break;
				case "progress":
					Console.Write($"New Progress (0-100, default: {existing.Progress * 100.0}): ");
					if (double.TryParse(Console.ReadLine(), out double prog) && prog >= 0 && prog <= 100) existing.Progress = prog / 100.0;
					break;
				case "duration":
					Console.Write($"New Estimated Duration (hours, default: {existing.EstimatedDuration.TotalHours}): ");
					if (double.TryParse(Console.ReadLine(), out double dur) && dur > 0) existing.EstimatedDuration = TimeSpan.FromHours(dur);
					break;
				default:
					Console.WriteLine("Unknown attribute.");
					return;
			}

			service.UpdateTask(existing);
			Console.WriteLine("Task updated.");
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

		private static void HandleMarkTaskAsIncomplete(TaskManagerService service, int id)
		{
			if (service.MarkTaskAsIncomplete(id))
			{
				Console.WriteLine($"Task {id} marked as incomplete.");
			}
			else
			{
				Console.WriteLine($"Task {id} not found.");
			}
		}

		private static void PrintHelp()
		{
			Console.WriteLine("\nAvailable commands:");
			Console.WriteLine("add <Title>         - Add a new task (prompts for details)");
			Console.WriteLine("list                - List all tasks sorted by urgency");
			Console.WriteLine("edit <Id>           - Edit a task by Id");
			Console.WriteLine("edit <attribute> <Id> - Edit a specific attribute of a task");
			Console.WriteLine("delete <Id>         - Delete a task by Id");
			Console.WriteLine("complete <Id>       - Mark a task as complete");
			Console.WriteLine("uncomplete <Id>     - Mark a task as incomplete");
			Console.WriteLine("help                - Show this help message");
			Console.WriteLine("exit                - Exit the application");
		}
	}
}
