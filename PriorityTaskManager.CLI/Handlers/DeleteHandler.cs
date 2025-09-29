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
        public DeleteHandler() { }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            int activeListId = service.GetActiveListId();
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args, activeListId);

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
