using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class TimeHandler : ICommandHandler
    {
        private readonly ITimeService _timeService;
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public TimeHandler(ITimeService timeService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _timeService = timeService;
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        public void Execute(TaskManagerService service, string[] args)
        {
            var subCommand = args.Length > 0 ? args[0].ToLower() : "status";

            switch (subCommand)
            {
                case "status":
                    var currentTime = _timeService.GetCurrentTime();
                    if (_timeService.IsSimulated())
                    {
                        Console.WriteLine($"Time is currently simulated.");
                        Console.WriteLine($"Current simulated time: {currentTime:yyyy-MM-dd HH:mm}");
                    }
                    else
                    {
                        Console.WriteLine($"Time is current (real-time).");
                        Console.WriteLine($"Current time: {currentTime:yyyy-MM-dd HH:mm}");
                    }
                    break;

                case "now":
                    _timeService.ClearSimulatedTime();
                    _snapshotProvider.RefreshActiveListSnapshot(out _);
                    ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    Console.WriteLine("Time simulation cleared. Using current real-time.");
                    break;

                case "custom":
                case "cus":
                    var simulatedTime = ConsoleInputHelper.GetDateTimeFromUser("Enter the simulated date and time");
                    _timeService.SetSimulatedTime(simulatedTime);
                    _snapshotProvider.RefreshActiveListSnapshot(out _);
                    ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    Console.WriteLine($"Time is now simulated. Current simulated time: {simulatedTime:yyyy-MM-dd HH:mm}");
                    break;

                default:
                    Console.WriteLine($"Unknown time command: {subCommand}");
                    Console.WriteLine("Available commands: time [status], time now, time custom|cus");
                    break;
            }
        }
    }
}
