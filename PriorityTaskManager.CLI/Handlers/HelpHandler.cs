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
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            RunInteractiveHelp();
        }

        private void RunInteractiveHelp()
        {
            var categories = new List<string> { "Task Commands", "List Commands", "Dependency Commands", "Event Commands", "General Commands", "Exit" };
            int selectedIndex = 0;

            Console.CursorVisible = false;
            while (true)
            {
                Console.Clear();
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
                            Console.Clear();
                            Console.CursorVisible = true;
                            return;
                        }
                        ShowCategoryHelp(categories[selectedIndex]);
                        Console.WriteLine("\nPress any key to return to the help menu...");
                        Console.ReadKey(true);
                        break;
                    case ConsoleKey.Escape:
                        Console.Clear();
                        Console.CursorVisible = true;
                        return;
                }
            }
        }

        private void ShowCategoryHelp(string category)
        {
            Console.Clear();
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
                    Console.WriteLine("list sort <Option>  - Change the sort order for the active list (options: Default, Alphabetical, DueDate, Id)");
                    Console.WriteLine("list delete <Name>  - Delete a list and all its tasks");
                    break;
                case "Dependency Commands":
                    Console.WriteLine("depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
                    Console.WriteLine("depend remove <childId> <parentId> - Remove a dependency");
                    break;
                case "Event Commands":
                    Console.WriteLine("event add           - Interactively add a new event");
                    Console.WriteLine("event add \"<Name>\" --from \"<DateTime>\" --to \"<DateTime>\" - Add a new event with details");
                    Console.WriteLine("event list          - List all upcoming events");
                    Console.WriteLine("event remove <Id>   - Remove an event by its Id");
                    break;
                case "General Commands":
                    Console.WriteLine("help                - Display this help text");
                    Console.WriteLine("mode [Mode]         - View or set the urgency strategy mode (SingleAgent, MultiAgent)");
                    Console.WriteLine("settings            - Open interactive user settings");
                    Console.WriteLine("exit                - Exit the application");
                    break;
            }
        }
    }
}
