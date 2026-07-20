using System;
using System.Globalization;
using System.Linq;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class EventCommandHandler : ICommandHandler, ICommandResultHandler
    {
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;
        private readonly IInteractiveConsoleFacade _console;

        public EventCommandHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
            : this(snapshotProvider, taskMetricsService, null)
        {
        }

        public EventCommandHandler(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService, IInteractiveConsoleFacade? console)
        {
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
            _console = console ?? new InteractiveConsoleFacade();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// This handler mixes non-interactive sub-commands with interactive prompts (event add/edit
        /// use cursor-based date pickers), all of which already own their console rendering via
        /// <see cref="IInteractiveConsoleFacade"/>. This wrapper exists only so <c>event</c> can
        /// participate in the canonical <see cref="ICommandResultHandler"/> dispatch contract;
        /// no dashboard refresh or message is deferred to <c>Program.cs</c>.
        /// </remarks>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            Execute(service, args);
            return new CommandResult();
        }

        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                HandleListEvents(service);
                return;
            }

            string command = args[0].ToLower();
            string[] subArgs = args.Skip(1).ToArray();

            switch (command)
            {
                case "add":
                    HandleAddEvent(service, subArgs);
                    break;
                case "list":
                    HandleListEvents(service);
                    break;
                case "edit":
                    HandleEditEvent(service, subArgs);
                    break;
                case "remove":
                case "delete":
                    HandleRemoveEvent(service, subArgs);
                    break;
                case "clear":
                    HandleClearEvents(service);
                    break;
                default:
                    _console.WriteLine("Unknown command. Usage: event <add|list|edit|delete|clear>");
                    break;
            }
        }

        private void HandleAddEvent(TaskManagerService service, string[] args)
        {
            string name;
            
            // Allow "event add My Event Name"
            if (args.Length > 0)
            {
                name = string.Join(" ", args);
            }
            else
            {
                _console.WriteLine("Adding a new event (press Esc to cancel).");
                _console.Write("Event Name: ");
                string? input = _console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    _console.WriteLine("Event creation cancelled.");
                    return;
                }
                name = input!;
            }

            DateTime? startTime = ConsoleInputHelper.GetDateTimeFromUser("Set event start time (Enter to confirm, Esc to cancel):", DateTime.Now);
            if (startTime == null)
            {
                _console.WriteLine("Event creation cancelled.");
                return;
            }

            DateTime? endTime = ConsoleInputHelper.GetDateTimeFromUser("Set event end time (Enter to confirm, Esc to cancel):", startTime.Value.AddHours(1));
            if (endTime == null)
            {
                _console.WriteLine("Event creation cancelled.");
                return;
            }

            if (endTime < startTime)
            {
                _console.WriteLine("Warning: End time is before start time. Swapping times.");
                (startTime, endTime) = (endTime, startTime);
            }

            var newEvent = new Event
            {
                Name = name,
                StartTime = startTime.Value,
                EndTime = endTime.Value
            };

            service.AddEvent(newEvent);
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine($"Event '{name}' added successfully.");
        }

        private void HandleListEvents(TaskManagerService service)
        {
            var events = service.GetAllEvents().OrderBy(e => e.StartTime).ToList();
            if (!events.Any())
            {
                _console.WriteLine("No scheduled events found.");
                return;
            }

            _console.WriteLine($"{"ID",-5} {"Time",-35} {"Event",-40}");
            _console.WriteLine(new string('-', 80));

            foreach (var ev in events)
            {
                string timeString = $"{ev.StartTime:g} - {ev.EndTime:t}";
                _console.WriteLine($"{ev.Id,-5} {timeString,-35} {ev.Name,-40}");
            }
        }

        private void HandleEditEvent(TaskManagerService service, string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int id))
            {
                _console.WriteLine("Usage: event edit <ID>");
                return;
            }

            var existingEvent = service.GetEvent(id);
            if (existingEvent == null)
            {
                _console.WriteLine($"Error: Event with ID {id} not found.");
                return;
            }

            // Interactive Menu Loop
            int selectedIndex = 0;
            _console.CursorVisible = false;

            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            _console.WriteLine($"Editing event: {existingEvent.Name}");
            int selectorTop = _console.CursorTop;

            var displayItems = BuildEventMenuItems(existingEvent);
            _console.DrawMenuItems(displayItems, selectedIndex, selectorTop);

            while (true)
            {
                var key = _console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter && (key.Modifiers & ConsoleModifiers.Shift) != 0)
                {
                    service.UpdateEvent(existingEvent);
                    _console.CursorVisible = true;
                    _snapshotProvider.RefreshActiveListSnapshot(out _);
                    _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    _console.WriteLine("\nEvent updated successfully.");
                    return;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + displayItems.Count) % displayItems.Count;
                        _console.UpdateMenuSelection(displayItems, previousUp, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % displayItems.Count;
                        _console.UpdateMenuSelection(displayItems, previousDown, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == displayItems.Count - 2) // Save & Exit
                        {
                            service.UpdateEvent(existingEvent);
                            _console.CursorVisible = true;
                            _snapshotProvider.RefreshActiveListSnapshot(out _);
                            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            _console.WriteLine("\nEvent updated successfully.");
                            return;
                        }
                        if (selectedIndex == displayItems.Count - 1) // Cancel
                        {
                            _console.CursorVisible = true;
                            _console.WriteLine("\nEdit cancelled.");
                            return;
                        }

                        // Handle property edits
                        _console.CursorVisible = true;
                        if (selectedIndex == 0) // Name
                        {
                             _console.Write($"\nNew Name ({existingEvent.Name}): ");
                             string? input = _console.ReadLine();
                             if (!string.IsNullOrWhiteSpace(input)) existingEvent.Name = input;
                        }
                        else if (selectedIndex == 1) // Start Time (Smart Shift)
                        {
                            TimeSpan originalDuration = existingEvent.EndTime - existingEvent.StartTime;
                            DateTime? newStart = ConsoleInputHelper.GetDateTimeFromUser("\nSet new start time:", existingEvent.StartTime);
                            if (newStart.HasValue)
                            {
                                existingEvent.StartTime = newStart.Value;
                                existingEvent.EndTime = newStart.Value.Add(originalDuration); // Smart Shift
                            }
                        }
                        else if (selectedIndex == 2) // End Time (Duration Change)
                        {
                            Console.CursorVisible = true;
                            // Prompt for end time starting from current end time, but ensure context is clear
                            DateTime? newEnd = ConsoleInputHelper.GetDateTimeFromUser("\nSet new end time:", existingEvent.EndTime);
                            if (newEnd.HasValue)
                            {
                                if (newEnd.Value < existingEvent.StartTime)
                                {
                                    _console.WriteLine("Warning: End time cannot be before start time.");
                                    _console.WriteLine("Press any key to continue...");
                                    _console.ReadKey(true);
                                }
                                else
                                {
                                    existingEvent.EndTime = newEnd.Value;
                                }
                            }
                        }
                        displayItems = BuildEventMenuItems(existingEvent);
                        _console.DrawMenuItems(displayItems, selectedIndex, selectorTop);
                        _console.CursorVisible = false;
                        break;
                    case ConsoleKey.Escape:
                        _console.CursorVisible = true;
                        _console.WriteLine("\nEdit cancelled.");
                        return;
                }
            }
        }

        private List<string> BuildEventMenuItems(Event existingEvent)
        {
            return new List<string>
            {
                $"Name: {existingEvent.Name}",
                $"Start Time: {existingEvent.StartTime:yyyy-MM-dd HH:mm}",
                $"End Time: {existingEvent.EndTime:yyyy-MM-dd HH:mm}",
                "[Save & Exit]",
                "[Cancel]"
            };
        }

        private void HandleRemoveEvent(TaskManagerService service, string[] args)
        {
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);

            if (args.Length == 0)
            {
                _console.WriteLine("Usage: event delete <ID> or <ID1,ID2,...>");
                return;
            }

            string joinedArgs = string.Join(",", args);
            var idStrings = joinedArgs.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            int successCount = 0;
            int failCount = 0;

            foreach (var idStr in idStrings)
            {
                if (int.TryParse(idStr.Trim(), out int id))
                {
                    if (service.DeleteEvent(id))
                    {
                        _console.WriteLine($"Event {id} deleted.");
                        successCount++;
                    }
                    else
                    {
                        _console.WriteLine($"Event {id} not found.");
                        failCount++;
                    }
                }
                else
                {
                    _console.WriteLine($"Invalid ID format: '{idStr}'");
                    failCount++;
                }
            }

            if (idStrings.Length > 1) 
            {
                _console.WriteLine($"Finished: {successCount} deleted, {failCount} failed.");
            }
        }

        private void HandleClearEvents(TaskManagerService service)
        {
            _snapshotProvider.RefreshActiveListSnapshot(out _);
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            
            _console.Write("Are you sure you want to delete ALL events? (y/n): ");
            if (_console.ReadLine()?.Trim().ToLower() == "y")
            {
                service.ClearEvents();
                _console.WriteLine("All events cleared.");
            }
            else
            {
                _console.WriteLine("Operation cancelled.");
            }
        }
    }
}
