using System;
using System.Globalization;
using System.Linq;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class EventHandler : ICommandResultHandler
    {
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public EventHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        /// <remarks>
        /// This handler is not currently wired up in <c>Program.cs</c> (the "event"/"e" commands
        /// dispatch to <see cref="EventCommandHandler"/> instead). It returns an inert
        /// <see cref="CommandResult"/> for contract consistency; no dashboard refresh or message is
        /// deferred to <c>Program.cs</c>.
        /// </remarks>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            ExecuteInteractive(service, args);
            return new CommandResult();
        }

        private void ExecuteInteractive(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: event <add|list|remove>");
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                    HandleAddEvent(service, args.Skip(1).ToArray());
                    break;
                case "list":
                    HandleListEvents(service);
                    break;
                case "remove":
                    HandleRemoveEvent(service, args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine("Usage: event <add|list|remove>");
                    break;
            }
        }

        private void HandleAddEvent(TaskManagerService service, string[] args)
        {
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            
            // ptm event add "My Event" --from "2025-12-01 10:00" --to "2025-12-01 11:00"
            if (args.Length < 5 || !args[1].Equals("--from") || !args[3].Equals("--to"))
            {
                Console.WriteLine("Usage: event add \"<Event Name>\" --from \"<yyyy-MM-dd HH:mm>\" --to \"<yyyy-MM-dd HH:mm>\"");
                return;
            }

            string name = args[0];
            if (!DateTime.TryParse(args[2], out DateTime startTime) || !DateTime.TryParse(args[4], out DateTime endTime))
            {
                Console.WriteLine("Invalid date format. Please use 'yyyy-MM-dd HH:mm'.");
                return;
            }

            var newEvent = new Event
            {
                Name = name,
                StartTime = startTime,
                EndTime = endTime
            };

            service.AddEvent(newEvent);
            Console.WriteLine($"Event '{name}' added successfully.");
        }

        private void HandleListEvents(TaskManagerService service)
        {
            var events = service.GetAllEvents().OrderBy(e => e.StartTime);
            if (!events.Any())
            {
                Console.WriteLine("No scheduled events.");
                return;
            }

            Console.WriteLine("Scheduled Events:");
            foreach (var ev in events)
            {
                Console.WriteLine($"  [ID: {ev.Id}] {ev.Name} ({ev.StartTime:g} - {ev.EndTime:g})");
            }
        }

        private void HandleRemoveEvent(TaskManagerService service, string[] args)
        {
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);

            if (args.Length == 0 || !int.TryParse(args[0], out int id))
            {
                Console.WriteLine("Usage: event remove <ID>");
                return;
            }

            if (service.DeleteEvent(id))
            {
                Console.WriteLine($"Event with ID {id} removed successfully.");
            }
            else
            {
                Console.WriteLine($"Error: Event with ID {id} not found.");
            }
        }
    }
}
