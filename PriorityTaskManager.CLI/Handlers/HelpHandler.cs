using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;

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
            Console.WriteLine("\nAvailable commands:");

            // Task Commands
            Console.WriteLine("\nTask Commands:");
            Console.WriteLine("add <Title>         - Add a new task (prompts for details)");
            Console.WriteLine("view <Id>           - View all details of a specific task");
            Console.WriteLine("edit <Id> ...       - Edit a task by Id or specific attributes");
            Console.WriteLine("delete <Id1,Id2,...> - Delete tasks by Id");
            Console.WriteLine("complete <Id1,Id2,...> - Mark tasks as complete");
            Console.WriteLine("uncomplete <Id1,Id2,...> - Mark tasks as incomplete");

            // List Commands
            Console.WriteLine("\nList Commands:");
            Console.WriteLine("list                - Display tasks in the current active list");
            Console.WriteLine("list all            - Show all available lists");
            Console.WriteLine("list create <Name>  - Create a new task list");
            Console.WriteLine("list switch <Name>  - Set the active task list");
            Console.WriteLine("list sort <Option>  - Change the sort order for the active list (options: Default, Alphabetical, DueDate, Id)");
            Console.WriteLine("list delete <Name>  - Delete a list and all its tasks");

            // Dependency Commands
            Console.WriteLine("\nDependency Commands:");
            Console.WriteLine("depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
            Console.WriteLine("depend remove <childId> <parentId> - Remove a dependency");

            // General Commands
            Console.WriteLine("\nGeneral Commands:");
            Console.WriteLine("help                - Display this help text");
            Console.WriteLine("exit                - Exit the application");
        }
    }
}
