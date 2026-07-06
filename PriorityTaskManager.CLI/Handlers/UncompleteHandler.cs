using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class UncompleteHandler : ICommandHandler
    {
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public UncompleteHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        public void Execute(TaskManagerService service, string[] args)
        {
            var validTaskIds = ConsoleInputHelper.ParseAndValidateTaskIds(service, args, service.GetActiveListId());

            _snapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            
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
