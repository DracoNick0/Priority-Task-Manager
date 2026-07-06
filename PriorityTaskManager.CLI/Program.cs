using PriorityTaskManager.CLI.Handlers;
using PriorityTaskManager.CLI.Interfaces;
using System.Collections.Generic;
using System;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Scheduling.GoldPanning;

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
			var timeService = new TimeService();

			var urgencyStrategy = new GoldPanningStrategy(dataContainer.UserProfile, dataContainer.Events, timeService);
			var service = new TaskManagerService(urgencyStrategy, persistenceService, dataContainer);
			var taskMetricsService = new TaskMetricsService();
			var scheduleSnapshotProvider = new ScheduleSnapshotProvider(service, taskMetricsService, timeService);
			scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);

			var backgroundRefreshScheduler = new BackgroundRefreshScheduler(scheduleSnapshotProvider);
			backgroundRefreshScheduler.Start();

			Console.WriteLine("Priority Task Manager CLI (type 'help' for commands)");

			var handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
			{
				{ "add", new AddHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "list", new ListHandler(taskMetricsService, timeService, scheduleSnapshotProvider) },
				{ "edit", new EditHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "delete", new DeleteHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "complete", new CompleteHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "uncomplete", new UncompleteHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "depend", new DependHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "view", new ViewHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "cleanup", new CleanupHandler(service, scheduleSnapshotProvider, taskMetricsService) },
				{ "help", new HelpHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "settings", new SettingsHandler(timeService, scheduleSnapshotProvider, taskMetricsService) },
				{ "event", new EventCommandHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "e", new EventCommandHandler(scheduleSnapshotProvider, taskMetricsService) },
				{ "time", new TimeHandler(timeService, scheduleSnapshotProvider, taskMetricsService) },
				{ "mode", new ModeHandler(scheduleSnapshotProvider, taskMetricsService) }
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
					backgroundRefreshScheduler.Stop();
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
