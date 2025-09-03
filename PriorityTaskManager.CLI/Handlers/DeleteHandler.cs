using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

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
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("Usage: delete <Id>,<Id2>,...");
                return;
            }

            foreach (var id in validTaskIds)
            {
                if (service.DeleteTask(id))
                {
                    Console.WriteLine($"Task {id} deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"Task {id} not found.");
                }
            }
        }
    }
}
