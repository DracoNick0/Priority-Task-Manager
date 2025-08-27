using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'list' command, displaying all tasks sorted by urgency.
    /// </summary>
    public class ListHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            service.CalculateUrgencyForAllTasks();

            var tasks = service.GetAllTasks().OrderByDescending(t => t.UrgencyScore).ToList();

            if (tasks.Count == 0)
            {
                Console.WriteLine("No tasks found.");
                return;
            }

            Console.WriteLine("\nAll Tasks (sorted by urgency):");

            foreach (var task in tasks)
            {
                var checkbox = task.IsCompleted ? "[x]" : "[ ]";

                if (task.IsCompleted)
                {
                    Console.WriteLine($"{checkbox} Id: {task.Id}, Title: {task.Title}");
                }
                else
                {
                    Console.WriteLine($"{checkbox} Id: {task.Id}, Title: {task.Title}, Urgency: {task.UrgencyScore:F3}, LPSD: {task.LatestPossibleStartDate:yyyy-MM-dd}");
                }
            }
        }
    }
}
