using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class UncompleteHandler : ICommandHandler
    {
        public void Execute(TaskManagerService service, string[] args)
        {
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args, Program.ActiveListId);

            if (validTaskIds.Count == 0)
            {
                Console.WriteLine("Usage: uncomplete <Id>,<Id2>,...");
                return;
            }

            foreach (var id in validTaskIds)
            {
                if (service.MarkTaskAsIncomplete(id))
                {
                    Console.WriteLine($"Task {id} marked as incomplete.");
                }
                else
                {
                    Console.WriteLine($"Task {id} not found.");
                }
            }
        }
    }
}
