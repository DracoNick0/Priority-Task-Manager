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
        private readonly IInteractiveConsoleFacade _console;

        public HelpHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
            : this(snapshotProvider, taskMetricsService, null)
        {
        }

        public HelpHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService, IInteractiveConsoleFacade? console)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
            _console = console ?? new InteractiveConsoleFacade();
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

            _console.CursorVisible = false;
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine("Select a category to see available commands:");
            int selectorTop = _console.CursorTop;
            _console.DrawMenuItems(categories, selectedIndex, selectorTop);

            while (true)
            {
                var key = _console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + categories.Count) % categories.Count;
                        _console.UpdateMenuSelection(categories, previousUp, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % categories.Count;
                        _console.UpdateMenuSelection(categories, previousDown, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == categories.Count - 1) // Exit
                        {
                            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            _console.CursorVisible = true;
                            return;
                        }
                        ShowCategoryHelp(categories[selectedIndex]);
                        _console.WriteLine(string.Empty);
                        _console.WriteLine("Press any key to return to the help menu...");
                        _console.ReadKey(true);
                        _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                        _console.WriteLine("Select a category to see available commands:");
                        selectorTop = _console.CursorTop;
                        _console.DrawMenuItems(categories, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Escape:
                        _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                        _console.CursorVisible = true;
                        return;
                }
            }
        }

        private void ShowCategoryHelp(string category)
        {
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine($"{category}:\n");

            switch (category)
            {
                case "Task Commands":
                    _console.WriteLine("add <Title>         - Add a new task (prompts for details)");
                    _console.WriteLine("view <Id>           - View all details of a specific task");
                    _console.WriteLine("edit <Id> ...       - Edit a task by Id or specific attributes");
                    _console.WriteLine("edit <Id> <attr> [val] - Edit a task attribute (title, description, importance, duedate, duration, complexity, pin)");
                    _console.WriteLine("delete <Id1,Id2,...> - Delete tasks by Id");
                    _console.WriteLine("complete <Id1,Id2,...> - Mark tasks as complete");
                    _console.WriteLine("uncomplete <Id1,Id2,...> - Mark tasks as incomplete");
                    break;
                case "List Commands":
                    _console.WriteLine("list                - Display tasks in the current active list");
                    _console.WriteLine("list all            - Show all available lists");
                    _console.WriteLine("list create <Name>  - Create a new task list");
                    _console.WriteLine("list switch <Name>  - Set the active task list");
                    _console.WriteLine("list settings       - Open interactive settings for the active list");
                    _console.WriteLine("list settings <subcommand> - Edit a specific list setting directly");
                    _console.WriteLine("list delete <Name>  - Delete a list and all its tasks");
                    break;
                case "Dependency Commands":
                    _console.WriteLine("depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
                    _console.WriteLine("depend remove <childId> <parentId> - Remove a dependency");
                    break;
                case "Time Commands":
                    _console.WriteLine("time                - Show current time (real or simulated)");
                    _console.WriteLine("time now            - Switch to using real-time");
                    _console.WriteLine("time custom         - Interactively set a custom simulated time");
                    break;
                case "Event Commands":
                    _console.WriteLine("event (alias 'e')       - Base command for event operations");
                    _console.WriteLine("e add [Name]            - Add a new event (interactive time selection)");
                    _console.WriteLine("e list                  - List all events sorted by time");
                    _console.WriteLine("e edit <Id>             - Edit an event (supports smart time shifting)");
                    _console.WriteLine("e delete <Id1,Id2...>   - Remove one or more events");
                    _console.WriteLine("e clear                 - delete ALL events");
                    break;
                case "General Commands":
                    _console.WriteLine("help                - Display this help text");
                    _console.WriteLine("mode [gold|constraint] - View or set scheduling strategy (GoldPanning or ConstraintOptimization)");
                    _console.WriteLine("defaults            - Open interactive menu for global defaults");
                    _console.WriteLine("exit                - Exit the application");
                    break;
            }
        }
    }
}
