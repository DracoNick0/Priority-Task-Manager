using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Interfaces;
using System.Collections.Generic;
using System;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI
{
	/// <summary>
	/// The entry point of the Priority Task Manager CLI application.
	/// </summary>
	class Program
	{
		/// <summary>
		/// Holds the ID of the currently active list.
		/// </summary>
		public static int ActiveListId { get; set; } = 1;

		/// <summary>
		/// The main method that initializes the application and processes user commands.
		/// </summary>
		/// <param name="args">Command-line arguments passed to the application.</param>
		static void Main(string[] args)
		{

			// Set up services
			var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
			var persistenceService = new PersistenceService(dataDirectory);
			var dataContainer = persistenceService.LoadData();

			var urgencyStrategy = new MultiAgentUrgencyStrategy(dataContainer.UserProfile, dataContainer.Events);
			var service = new TaskManagerService(urgencyStrategy, persistenceService, dataContainer);
			var taskMetricsService = new TaskMetricsService();

			Console.WriteLine("Priority Task Manager CLI (type 'help' for commands)");

			var handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
			{
				{ "add", new AddHandler() },
				{ "list", new ListHandler(taskMetricsService) },
				{ "edit", new EditHandler() },
				{ "delete", new DeleteHandler() },
				{ "complete", new CompleteHandler() },
				{ "uncomplete", new UncompleteHandler() },
				{ "help", new HelpHandler() },
				{ "depend", new DependHandler() },
				{ "view", new ViewHandler() },
				{ "cleanup", new CleanupHandler(service) },
				{ "mode", new ModeHandler() },
				{ "settings", new SettingsHandler() },
				{ "event", new EventCommandHandler() }
			};

			while (true)
			{
				Console.Write("\n> ");

				var line = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(line))
					continue;

				var parts = line.Trim().Split(' ', 2);
				var command = parts[0];
				var argString = parts.Length > 1 ? parts[1] : string.Empty;
				var argsArr = argString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
				{
					Console.WriteLine("Goodbye!");
					break;
				}

				if (handlers.TryGetValue(command, out var handler))
				{
					handler.Execute(service, argsArr);
				}
				else
				{
					Console.WriteLine("Unknown command. Type 'help' for a list of commands.");
				}
			}
		}
	}
}
