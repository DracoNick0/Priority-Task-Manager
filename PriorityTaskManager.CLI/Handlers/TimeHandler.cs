using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class TimeHandler : ICommandHandler, ICommandResultHandler
    {
        private readonly ITimeService _timeService;

        public TimeHandler(ITimeService timeService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _timeService = timeService;
            // Snapshot/metrics dependencies intentionally retained in the constructor to avoid breaking
            // current wiring while this handler migrates toward Program-driven dashboard rendering.
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            var result = ExecuteWithResult(service, args);
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                Console.WriteLine(result.Message);
            }
        }

        /// <inheritdoc/>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            var subCommand = args.Length > 0 ? args[0].ToLower() : "status";

            switch (subCommand)
            {
                case "status":
                    var currentTime = _timeService.GetCurrentTime();
                    var statusMessage = _timeService.IsSimulated()
                        ? $"Time is currently simulated.\nCurrent simulated time: {currentTime:yyyy-MM-dd HH:mm}"
                        : $"Time is current (real-time).\nCurrent time: {currentTime:yyyy-MM-dd HH:mm}";

                    return new CommandResult
                    {
                        Status = CommandResultStatus.Info,
                        Message = statusMessage,
                        ShouldRefreshDashboard = false
                    };

                case "now":
                    _timeService.ClearSimulatedTime();
                    return new CommandResult
                    {
                        Status = CommandResultStatus.Success,
                        Message = "Time simulation cleared. Using current real-time.",
                        ShouldRefreshDashboard = true
                    };

                case "custom":
                case "cus":
                    var simulatedTime = ConsoleInputHelper.GetDateTimeFromUser("Enter the simulated date and time");
                    _timeService.SetSimulatedTime(simulatedTime);
                    return new CommandResult
                    {
                        Status = CommandResultStatus.Success,
                        Message = $"Time is now simulated. Current simulated time: {simulatedTime:yyyy-MM-dd HH:mm}",
                        ShouldRefreshDashboard = true
                    };

                default:
                    return new CommandResult
                    {
                        Status = CommandResultStatus.Usage,
                        Message = $"Unknown time command: {subCommand}\nAvailable commands: time [status], time now, time custom|cus",
                        ShouldRefreshDashboard = false
                    };
            }
        }
    }
}
