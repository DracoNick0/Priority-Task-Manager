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

            Console.WriteLine("add <Title>         - Add a new task (prompts for details)");
            Console.WriteLine("list view           - Display tasks in the current active list");
            Console.WriteLine("list all            - Show all available lists");
            Console.WriteLine("list create <Name>  - Create a new task list");
            Console.WriteLine("list switch <Name>  - Set the active task list");
            Console.WriteLine("list sort <option>  - Change the sort order for the active list (options: Default, Alphabetical, DueDate, Id)");
            Console.WriteLine("list delete <Name>  - Delete a list and all its tasks");
            Console.WriteLine("edit <Id>           - Edit a task by Id");
            Console.WriteLine("edit <Id> <attribute> [new value] - Edit a specific attribute, optionally providing the new value directly.");
            Console.WriteLine("delete <Id>         - Delete a task by Id");
            Console.WriteLine("complete <Id>       - Mark a task as complete");
            Console.WriteLine("uncomplete <Id>     - Mark a task as incomplete");
            Console.WriteLine("depend add <childId> <parentId>    - Add a dependency (child depends on parent)");
            Console.WriteLine("depend remove <childId> <parentId> - Remove a dependency");
            Console.WriteLine("view <Id>           - View all details of a specific task");
        }
    }
}
