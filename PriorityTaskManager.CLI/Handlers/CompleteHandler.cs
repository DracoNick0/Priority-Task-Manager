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
        private readonly TaskManagerService _service;

        public CompleteHandler(TaskManagerService service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public void Execute(string[] args)
        {
            int activeListId = _service.GetActiveListId();
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(_service, args, activeListId);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("Usage: complete <Id>,<Id2>,...");
                return;
            }

            foreach (var id in validTaskIds)
            {
                if (_service.MarkTaskAsComplete(id))
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
