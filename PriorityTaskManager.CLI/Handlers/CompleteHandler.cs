using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'complete' command.
    /// Marks one or more tasks as complete using their display IDs.
    /// </summary>
    public class CompleteHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            int activeListId = service.GetActiveListId();
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args, activeListId);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("No valid task IDs provided.");
                Console.WriteLine("Usage: complete <Id1>,<Id2>,...");
                return;
            }

            foreach (var id in validTaskIds)
            {
                if (service.MarkTaskAsComplete(id))
                {
                    Console.WriteLine($"Task {id} marked as complete.");
                }
                else
                {
                    // This case is unlikely if ParseAndValidateTaskIds is correct, but included for robustness.
                    Console.WriteLine($"Error: Task {id} could not be found or updated.");
                }
            }
        }
    }
}
