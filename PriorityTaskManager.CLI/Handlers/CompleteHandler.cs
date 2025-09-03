using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

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
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("Usage: complete <Id>,<Id2>,...");
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
                    Console.WriteLine($"Task {id} not found.");
                }
            }
        }
    }
}
