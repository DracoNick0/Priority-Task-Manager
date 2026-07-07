using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using System.Collections.Generic;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'help' command, displaying a list of available commands and their descriptions.
    /// </summary>
    public class HelpHandler : ICommandHandler
    {
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public HelpHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            InteractiveHelp();
        }

        private void InteractiveHelp()
        {
            var categories = new List<string> { "Task Commands", "List Commands", "Dependency Commands", "Event Commands", "Time Commands", "General Commands", "Exit" };
            int selectedIndex = 0;

            Console.CursorVisible = false;
            while (true)
            {
                ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                Console.WriteLine("Select a category to see available commands:");
                ConsoleHelper.DrawMenu(categories, selectedIndex);

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + categories.Count) % categories.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % categories.Count;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == categories.Count - 1) // Exit
                        {
                            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            Console.CursorVisible = true;
                            return;
                        }
                        ShowCategoryHelp(categories[selectedIndex]);
                        Console.WriteLine("\nPress any key to return to the help menu...");
                        Console.ReadKey(true);
                        break;
                    case ConsoleKey.Escape:
                        ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                        Console.CursorVisible = true;
                        return;
                }
            }
        }

        private void ShowCategoryHelp(string category)
        {
            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            Console.WriteLine($"{category}:\n");

            switch (category)
            {
                case "Task Commands":
                    Console.WriteLine("add <Title>         - Add a new task (prompts for details)");
                    Console.WriteLine("view <Id>           - View all details of a specific task");
                    Console.WriteLine("edit <Id> ...       - Edit a task by Id or specific attributes");
                    Console.WriteLine("edit <Id> <attr> [val] - Edit a task attribute (title, description, importance, duedate, duration, complexity, pin)");
                    Console.WriteLine("delete <Id1,Id2,...> - Delete tasks by Id");
                    Console.WriteLine("complete <Id1,Id2,...> - Mark tasks as complete");
                    Console.WriteLine("uncomplete <Id1,Id2,...> - Mark tasks as incomplete");
                    break;
                case "List Commands":
                    Console.WriteLine("list                - Display tasks in the current active list");
                    Console.WriteLine("list all            - Show all available lists");
                    Console.WriteLine("list create <Name>  - Create a new task list");
                    Console.WriteLine("list switch <Name>  - Set the active task list");
                    Console.WriteLine("list settings       - Open interactive settings for the active list");
                    Console.WriteLine("list settings <subcommand> - Edit a specific list setting directly");
                    Console.WriteLine("list delete <Name>  - Delete a list and all its tasks");
                    break;
                case "Dependency Commands":
                    Console.WriteLine("depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
                    Console.WriteLine("depend remove <childId> <parentId> - Remove a dependency");
                    break;
                case "Time Commands":
                    Console.WriteLine("time                - Show current time (real or simulated)");
                    Console.WriteLine("time now            - Switch to using real-time");
                    Console.WriteLine("time custom         - Interactively set a custom simulated time");
                    break;
                case "Event Commands":
                    Console.WriteLine("event (alias 'e')       - Base command for event operations");
                    Console.WriteLine("e add [Name]            - Add a new event (interactive time selection)");
                    Console.WriteLine("e list                  - List all events sorted by time");
                    Console.WriteLine("e edit <Id>             - Edit an event (supports smart time shifting)");
                    Console.WriteLine("e delete <Id1,Id2...>   - Remove one or more events");
                    Console.WriteLine("e clear                 - delete ALL events");
                    break;
                case "General Commands":
                    Console.WriteLine("help                - Display this help text");
                    Console.WriteLine("mode [gold|constraint] - View or set scheduling strategy (GoldPanning or ConstraintOptimization)");
                    Console.WriteLine("user defaults       - Open interactive menu for global defaults");
                    Console.WriteLine("exit                - Exit the application");
                    break;
            }
        }
    }
}
