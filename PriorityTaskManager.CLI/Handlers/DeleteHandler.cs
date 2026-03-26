using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'delete' command.
    /// Permanently removes one or more tasks from the active list using their display IDs.
    /// </summary>
    public class DeleteHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            int activeListId = service.GetActiveListId();
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args, activeListId);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("No valid task IDs provided.");
                Console.WriteLine("Usage: delete <Id1>,<Id2>,...");
                return;
            }

            // Optional: Add a confirmation step for safety.
            // Console.Write($"You are about to delete {validTaskIds.Count} task(s). Are you sure? (y/n): ");
            // if (Console.ReadKey().Key != ConsoleKey.Y)
            // {
            //     Console.WriteLine("\nDeletion cancelled.");
            //     return;
            // }
            // Console.WriteLine();

            foreach (var id in validTaskIds)
            {
                if (service.DeleteTask(id))
                {
                    Console.WriteLine($"Task {id} deleted successfully.");
                }
                else
                {
                    // This case is unlikely if ParseAndValidateTaskIds is correct, but included for robustness.
                    Console.WriteLine($"Error: Task {id} could not be found or deleted.");
                }
            }
        }
    }
}

