using System;
using System.Globalization;
using System.Linq;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
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
                    Console.WriteLine("Unknown command. Usage: event <add|list|edit|delete|clear>");
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
                Console.WriteLine("Adding a new event (press Esc to cancel).");
                Console.Write("Event Name: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Event creation cancelled.");
                    return;
                }
                name = input!;
            }

            DateTime? startTime = ConsoleInputHelper.GetDateTimeFromUser("Set event start time (Enter to confirm, Esc to cancel):", DateTime.Now);
            if (startTime == null)
            {
                Console.WriteLine("Event creation cancelled.");
                return;
            }

            DateTime? endTime = ConsoleInputHelper.GetDateTimeFromUser("Set event end time (Enter to confirm, Esc to cancel):", startTime.Value.AddHours(1));
            if (endTime == null)
            {
                Console.WriteLine("Event creation cancelled.");
                return;
            }

            if (endTime < startTime)
            {
                Console.WriteLine("Warning: End time is before start time. Swapping times.");
                (startTime, endTime) = (endTime, startTime);
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

        private void HandleListEvents(TaskManagerService service)
        {
            var events = service.GetAllEvents().OrderBy(e => e.StartTime).ToList();
            if (!events.Any())
            {
                Console.WriteLine("No scheduled events found.");
                return;
            }

            Console.WriteLine($"{"ID",-5} {"Time",-35} {"Event",-40}");
            Console.WriteLine(new string('-', 80));

            foreach (var ev in events)
            {
                string timeString = $"{ev.StartTime:g} - {ev.EndTime:t}";
                Console.WriteLine($"{ev.Id,-5} {timeString,-35} {ev.Name,-40}");
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

            // Interactive Menu Loop
            int selectedIndex = 0;
            Console.CursorVisible = false;

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"Editing event: {existingEvent.Name}");
                
                var displayItems = new List<string>
                {
                    $"Name: {existingEvent.Name}",
                    $"Start Time: {existingEvent.StartTime:yyyy-MM-dd HH:mm}",
                    $"End Time: {existingEvent.EndTime:yyyy-MM-dd HH:mm}",
                    "[Save & Exit]",
                    "[Cancel]"
                };

                ConsoleHelper.DrawMenu(displayItems, selectedIndex);

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter && (key.Modifiers & ConsoleModifiers.Shift) != 0)
                {
                    service.UpdateEvent(existingEvent);
                    Console.CursorVisible = true;
                    Console.WriteLine("\nEvent updated successfully.");
                    return;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + displayItems.Count) % displayItems.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % displayItems.Count;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == displayItems.Count - 2) // Save & Exit
                        {
                            service.UpdateEvent(existingEvent);
                            Console.CursorVisible = true;
                            Console.WriteLine("\nEvent updated successfully.");
                            return;
                        }
                        if (selectedIndex == displayItems.Count - 1) // Cancel
                        {
                            Console.CursorVisible = true;
                            Console.WriteLine("\nEdit cancelled.");
                            return;
                        }

                        // Handle property edits
                        Console.CursorVisible = true;
                        if (selectedIndex == 0) // Name
                        {
                             Console.Write($"\nNew Name ({existingEvent.Name}): ");
                             string? input = Console.ReadLine();
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
                                    Console.WriteLine("Warning: End time cannot be before start time.");
                                    Console.WriteLine("Press any key to continue...");
                                    Console.ReadKey(true);
                                }
                                else
                                {
                                    existingEvent.EndTime = newEnd.Value;
                                }
                            }
                        }
                        Console.CursorVisible = false;
                        break;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        Console.WriteLine("\nEdit cancelled.");
                        return;
                }
            }
        }

        private void HandleRemoveEvent(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: event delete <ID> or <ID1,ID2,...>");
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
                        Console.WriteLine($"Event {id} deleted.");
                        successCount++;
                    }
                    else
                    {
                        Console.WriteLine($"Event {id} not found.");
                        failCount++;
                    }
                }
                else
                {
                    Console.WriteLine($"Invalid ID format: '{idStr}'");
                    failCount++;
                }
            }

            if (idStrings.Length > 1) 
            {
                Console.WriteLine($"Finished: {successCount} deleted, {failCount} failed.");
            }
        }

        private void HandleClearEvents(TaskManagerService service)
        {
            Console.Write("Are you sure you want to delete ALL events? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                service.ClearEvents();
                Console.WriteLine("All events cleared.");
            }
            else
            {
                Console.WriteLine("Operation cancelled.");
            }
        }
    }
}
