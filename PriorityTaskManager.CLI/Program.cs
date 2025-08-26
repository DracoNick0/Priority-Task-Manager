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
			bool running = true;
			while (running)
			{
				Console.WriteLine("\nPriority Task Manager");
				Console.WriteLine("1. Add a New Task");
				Console.WriteLine("2. View All Tasks");
				Console.WriteLine("3. Update an Existing Task");
				Console.WriteLine("4. Delete a Task");
				Console.WriteLine("5. Mark Task as Complete");
				Console.WriteLine("6. Exit");
				Console.Write("Select an option: ");
				var input = Console.ReadLine();
				switch (input)
				{
					case "1":
						HandleAddTask(service);
						break;
					case "2":
						HandleViewAllTasks(service);
						break;
					case "3":
						HandleUpdateTask(service);
						break;
					case "4":
						HandleDeleteTask(service);
						break;
					case "5":
						HandleMarkTaskAsComplete(service);
						break;
					case "6":
						running = false;
						Console.WriteLine("Goodbye!");
						break;
					default:
						Console.WriteLine("Invalid option. Please try again.");
						break;
				}
			}
		}

		private static void HandleAddTask(TaskManagerService service)
		{
			Console.Write("Enter Title: ");
			var title = Console.ReadLine();
			Console.Write("Enter Description: ");
			var description = Console.ReadLine();
			int importance = 1;
			while (true)
			{
				Console.Write("Enter Importance (1-10): ");
				var importanceInput = Console.ReadLine();
				if (int.TryParse(importanceInput, out importance) && importance >= 1 && importance <= 10)
					break;
				Console.WriteLine("Invalid importance. Please enter a number between 1 and 10.");
			}
			double durationHours = 1.0;
			while (true)
			{
				Console.Write("Enter Estimated Duration (hours): ");
				var durationInput = Console.ReadLine();
				if (double.TryParse(durationInput, out durationHours) && durationHours > 0)
					break;
				Console.WriteLine("Invalid duration. Please enter a positive number.");
			}
			double progress = 0.0;
			while (true)
			{
				Console.Write("Enter Progress (0-100): ");
				var progressInput = Console.ReadLine();
				if (double.TryParse(progressInput, out progress) && progress >= 0 && progress <= 100)
					break;
				Console.WriteLine("Invalid progress. Please enter a number between 0 and 100.");
			}
			Console.Write("Enter Dependencies (comma-separated task IDs, or leave blank): ");
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
			var task = new TaskItem
			{
				Title = title,
				Description = description,
				Importance = importance,
				DueDate = DateTime.Now.AddDays(1),
				IsCompleted = false,
				EstimatedDuration = TimeSpan.FromHours(durationHours),
				Progress = progress / 100.0,
				Dependencies = deps
			};
			service.AddTask(task);
			Console.WriteLine($"Task added with Id {task.Id}.");
		}

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

		private static void HandleUpdateTask(TaskManagerService service)
		{
			Console.Write("Enter Id of task to update: ");
			if (!int.TryParse(Console.ReadLine(), out int id))
			{
				Console.WriteLine("Invalid Id.");
				return;
			}
			var existing = service.GetTaskById(id);
			if (existing == null)
			{
				Console.WriteLine("Task not found.");
				return;
			}
			Console.Write("Enter new Title: ");
			var title = Console.ReadLine();
			Console.Write("Enter new Description: ");
			var description = Console.ReadLine();
			int importance = existing.Importance;
			while (true)
			{
				Console.Write("Enter new Importance (1-10): ");
				var importanceInput = Console.ReadLine();
				if (int.TryParse(importanceInput, out importance) && importance >= 1 && importance <= 10)
					break;
				Console.WriteLine("Invalid importance. Please enter a number between 1 and 10.");
			}
			double durationHours = existing.EstimatedDuration.TotalHours;
			while (true)
			{
				Console.Write("Enter new Estimated Duration (hours): ");
				var durationInput = Console.ReadLine();
				if (double.TryParse(durationInput, out durationHours) && durationHours > 0)
					break;
				Console.WriteLine("Invalid duration. Please enter a positive number.");
			}
			double progress = existing.Progress * 100.0;
			while (true)
			{
				Console.Write("Enter new Progress (0-100): ");
				var progressInput = Console.ReadLine();
				if (double.TryParse(progressInput, out progress) && progress >= 0 && progress <= 100)
					break;
				Console.WriteLine("Invalid progress. Please enter a number between 0 and 100.");
			}
			Console.Write("Enter new Dependencies (comma-separated task IDs, or leave blank): ");
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
			var updated = new TaskItem
			{
				Id = id,
				Title = title,
				Description = description,
				Importance = importance,
				DueDate = existing.DueDate,
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

		private static void HandleDeleteTask(TaskManagerService service)
		{
			Console.Write("Enter Id of task to delete: ");
			if (!int.TryParse(Console.ReadLine(), out int id))
			{
				Console.WriteLine("Invalid Id.");
				return;
			}
			if (service.DeleteTask(id))
				Console.WriteLine("Task deleted successfully.");
			else
				Console.WriteLine("Task not found.");
		}

		private static void HandleMarkTaskAsComplete(TaskManagerService service)
		{
			Console.Write("Enter Id of task to mark as complete: ");
			if (!int.TryParse(Console.ReadLine(), out int id))
			{
				Console.WriteLine("Invalid Id.");
				return;
			}
			if (service.MarkTaskAsComplete(id))
				Console.WriteLine("Task marked as complete.");
			else
				Console.WriteLine("Task not found.");
		}
	}
}
