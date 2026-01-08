using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class TimeHandler : ICommandHandler
    {
        private readonly ITimeService _timeService;

        public TimeHandler(ITimeService timeService)
        {
            _timeService = timeService;
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
                    Console.WriteLine("Time simulation cleared. Using current real-time.");
                    break;

                case "custom":
                case "cus":
                    var simulatedTime = ConsoleInputHelper.GetDateTimeFromUser("Enter the simulated date and time");
                    _timeService.SetSimulatedTime(simulatedTime);
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
