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
		/// The main method that initializes the application and processes user commands.
		/// </summary>
		/// <param name="args">Command-line arguments passed to the application.</param>
		static void Main(string[] args)
		{
			var service = new TaskManagerService();

			Console.WriteLine("Priority Task Manager CLI (type 'help' for commands)");

			var handlers = new Dictionary<string, ICommandHandler>(StringComparer.OrdinalIgnoreCase)
			{
				{ "add", new AddHandler() },
				{ "list", new ListHandler() },
				{ "edit", new EditHandler() },
				{ "delete", new DeleteHandler() },
				{ "complete", new CompleteHandler() },
				{ "uncomplete", new UncompleteHandler() },
				{ "help", new HelpHandler() }
			};
			handlers.Add("depend", new DependHandler());

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
