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
        private readonly TaskManagerService _service;

        public DeleteHandler(TaskManagerService service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public void Execute(string[] args)
        {
            int activeListId = _service.GetActiveListId(Program.ActiveListId);
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(_service, args, activeListId);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("Usage: delete <Id>,<Id2>,...");
                return;
            }

            foreach (var id in validTaskIds)
            {
                if (_service.DeleteTask(id))
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
