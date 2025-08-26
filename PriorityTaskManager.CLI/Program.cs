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
				Console.WriteLine("5. Exit");
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
			Console.Write("Enter Importance (Low, Medium, High): ");
			var importanceInput = Console.ReadLine();
			ImportanceLevel importance;
			if (!Enum.TryParse(importanceInput, true, out importance))
			{
				Console.WriteLine("Invalid importance. Defaulting to Low.");
				importance = ImportanceLevel.Low;
			}
			var task = new TaskItem
			{
				Title = title,
				Description = description,
				Importance = importance,
				DueDate = DateTime.Now.AddDays(1),
				IsCompleted = false
			};
			service.AddTask(task);
			Console.WriteLine($"Task added with Id {task.Id}.");
		}

		private static void HandleViewAllTasks(TaskManagerService service)
		{
			var tasks = service.GetAllTasks();
			if (tasks == null || !tasks.GetEnumerator().MoveNext())
			{
				Console.WriteLine("No tasks found.");
				return;
			}
			Console.WriteLine("\nAll Tasks:");
			foreach (var task in tasks)
			{
				Console.WriteLine($"Id: {task.Id}, Title: {task.Title}, Description: {task.Description}, Completed: {task.IsCompleted}");
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
			var updated = new TaskItem
			{
				Id = id,
				Title = title,
				Description = description,
				Importance = existing.Importance,
				DueDate = existing.DueDate,
				IsCompleted = existing.IsCompleted
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
	}
}
