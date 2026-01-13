using System;
using System.Globalization;
using System.Linq;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class EventCommandHandler : ICommandHandler
    {
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: event <add|list|edit|remove>");
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
                case "edit":
                    HandleEditEvent(service, args.Skip(1).ToArray());
                    break;
                case "remove":
                    HandleRemoveEvent(service, args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine("Usage: event <add|list|edit|remove>");
                    break;
            }
        }

        private void HandleAddEvent(TaskManagerService service, string[] args)
        {
            if (args.Length > 0)
            {
                ParseAddEventArgs(service, args);
            }
            else
            {
                RunInteractiveAdd(service);
            }
        }

        private void ParseAddEventArgs(TaskManagerService service, string[] args)
        {
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

        private void RunInteractiveAdd(TaskManagerService service)
        {
            Console.WriteLine("Adding a new event (press Esc to cancel).");

            Console.Write("Event Name: ");
            string? name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Event creation cancelled.");
                return;
            }

            DateTime? startTime = Utils.ConsoleInputHelper.GetDateTimeFromUser("Set event start time:", DateTime.Now);
            if (startTime == null)
            {
                Console.WriteLine("Event creation cancelled. A valid start time is required.");
                return;
            }

            DateTime? endTime = Utils.ConsoleInputHelper.GetDateTimeFromUser("Set event end time:", startTime.Value.AddHours(1));
            if (endTime == null)
            {
                Console.WriteLine("Event creation cancelled. A valid end time is required.");
                return;
            }

            var newEvent = new Event
            {
                Name = name,
                StartTime = startTime.Value,
                EndTime = endTime.Value
            };

            service.AddEvent(newEvent);
            Console.WriteLine($"Event '{name}' added successfully.");
        }

        private DateTime GetDateTimeFromUser(string prompt, DateTime? defaultTime = null)
        {
            DateTime result;
            while (true)
            {
                Console.Write(prompt);
                string? input = Console.ReadLine();
                if (DateTime.TryParse(input, out result))
                {
                    return result;
                }
                else if (defaultTime.HasValue && string.IsNullOrWhiteSpace(input))
                {
                    return defaultTime.Value;
                }
                else
                {
                    Console.WriteLine("Invalid date/time format. Please try again.");
                }
            }
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

        private void HandleEditEvent(TaskManagerService service, string[] args)
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int id))
            {
                Console.WriteLine("Usage: event edit <ID>");
                return;
            }

            var existingEvent = service.GetEvent(id);
            if (existingEvent == null)
            {
                Console.WriteLine($"Error: Event with ID {id} not found.");
                return;
            }

            Console.WriteLine($"Editing event: {existingEvent.Name} (ID: {existingEvent.Id})");

            Console.Write($"Event Name ({existingEvent.Name}): ");
            string? newName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                existingEvent.Name = newName;
            }

            DateTime? newStartTime = Utils.ConsoleInputHelper.GetDateTimeFromUser("Set new event start time:", existingEvent.StartTime);
            if (newStartTime.HasValue)
            {
                existingEvent.StartTime = newStartTime.Value;
            }


            DateTime? newEndTime = Utils.ConsoleInputHelper.GetDateTimeFromUser("Set new event end time:", existingEvent.EndTime);

            if (newEndTime.HasValue)
            {
                if (newEndTime.Value < existingEvent.StartTime)
                {
                    Console.WriteLine("End time cannot be before start time. Adjusting end time.");
                    existingEvent.EndTime = existingEvent.StartTime.AddHours(1);
                }
                else
                {
                    existingEvent.EndTime = newEndTime.Value;
                }
            }

            service.UpdateEvent(existingEvent);
            Console.WriteLine($"Event '{existingEvent.Name}' updated successfully.");
        }

        private void HandleRemoveEvent(TaskManagerService service, string[] args)
        {
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
