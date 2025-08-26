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
			var task = new TaskItem
			{
				Title = "Sample Task",
				Description = "This is a sample task.",
				Importance = ImportanceLevel.Medium,
				DueDate = DateTime.Now.AddDays(2),
				IsCompleted = false
			};
			service.AddTask(task);
			Console.WriteLine($"Task added. Total tasks: {service.GetTaskCount()}");
		}
	}
}
