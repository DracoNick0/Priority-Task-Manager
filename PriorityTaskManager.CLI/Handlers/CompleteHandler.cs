using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'complete' command, marking tasks as complete by their ID.
    /// </summary>
    public class CompleteHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int id))
            {
                Console.WriteLine("Invalid arguments. Please provide a valid task ID.");
                return;
            }

            if (service.MarkTaskAsComplete(id))
            {
                Console.WriteLine("Task marked as complete.");
            }
            else
            {
                Console.WriteLine("Task not found.");
            }
        }
    }
}
