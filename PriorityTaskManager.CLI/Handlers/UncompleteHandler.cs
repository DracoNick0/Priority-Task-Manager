using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;

namespace PriorityTaskManager.CLI.Handlers
{
    public class UncompleteHandler : ICommandHandler
    {
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int id))
            {
                Console.WriteLine("Invalid arguments. Please provide a valid task ID.");
                return;
            }

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
