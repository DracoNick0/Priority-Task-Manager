using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'delete' command, allowing users to remove tasks by their ID.
    /// </summary>
    public class DeleteHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int id))
            {
                Console.WriteLine("Invalid arguments. Please provide a valid task ID.");
                return;
            }

            if (service.DeleteTask(id))
            {
                Console.WriteLine("Task deleted successfully.");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }
    }
}
