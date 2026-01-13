using System;
using System.Collections.Generic;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Utils
{
    /// <summary>
    /// Provides shared console input functionality for the CLI application.
    /// </summary>
    public static class ConsoleInputHelper
    {
        /// <summary>
        /// Handles interactive date input, allowing users to adjust and confirm a date.
        /// </summary>
        /// <param name="initialDate">The initial date to start the adjustment from. Can be null.</param>
        /// <returns>The adjusted and confirmed date, or null if no due date is selected.</returns>
        public static DateTime? HandleInteractiveDateInput(DateTime? initialDate)
        {
            DateTime date = initialDate ?? DateTime.Today;
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            IncrementMode mode = IncrementMode.Day;

            while (true)
            {
                Console.SetCursorPosition(left, top);
                if (mode == IncrementMode.NoDueDate)
                {
                    Console.Write($"[Mode: No Due Date] (Press Enter to confirm)      ");
                }
                else
                {
                    Console.Write($"[Mode: {mode}] {date:yyyy-MM-dd dddd}              ");
                }

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.RightArrow:
                        if (mode != IncrementMode.NoDueDate)
                        {
                            switch (mode)
                            {
                                case IncrementMode.Day: date = date.AddDays(1); break;
                                case IncrementMode.Week: date = date.AddDays(7); break;
                                case IncrementMode.Month: date = date.AddMonths(1); break;
                                case IncrementMode.Year: date = date.AddYears(1); break;
                            }
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (mode != IncrementMode.NoDueDate)
                        {
                            switch (mode)
                            {
                                case IncrementMode.Day: date = date.AddDays(-1); break;
                                case IncrementMode.Week: date = date.AddDays(-7); break;
                                case IncrementMode.Month: date = date.AddMonths(-1); break;
                                case IncrementMode.Year: date = date.AddYears(-1); break;
                            }
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        mode = (IncrementMode)(((int)mode + 1) % 5);
                        break;
                    case ConsoleKey.DownArrow:
                        mode = (IncrementMode)(((int)mode + 4) % 5);
                        break;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        if (mode == IncrementMode.NoDueDate)
                        {
                            return null;
                        }
                        return date;
                }
            }
        }

        /// <summary>
        /// Parses and validates task IDs from user input.
        /// </summary>
        /// <param name="service">The TaskManagerService instance to validate task existence.</param>
        /// <param name="args">The command-line arguments containing task IDs.</param>
        /// <param name="activeListId">The ID of the active task list.</param>
        /// <returns>A list of valid task IDs.</returns>
        public static List<int> ParseAndValidateTaskIds(TaskManagerService service, string[] args, int activeListId)
        {
            var realIds = new List<int>();

            if (args == null || args.Length == 0)
            {
                return realIds;
            }

            string input = string.Join("", args);
            string[] potentialDisplayIds = input.Split(',');

            foreach (var idString in potentialDisplayIds)
            {
                string trimmedId = idString.Trim();

                if (int.TryParse(trimmedId, out int displayId))
                {
                    var task = service.GetTaskByDisplayId(displayId, activeListId);

                    if (task != null)
                    {
                        realIds.Add(task.Id);
                    }
                    else
                    {
                        Console.WriteLine($"Error: Task with Display ID {displayId} not found in the current list.");
                        return new List<int>();
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: '{trimmedId}' is not a valid Display ID and will be skipped.");
                }
            }

            return realIds;
        }

        /// <summary>
        /// Represents the modes for incrementing the date during interactive input.
        /// </summary>
        private enum IncrementMode { Day, Week, Month, Year, NoDueDate }

        private enum TimeIncrementMode { Hour, Minute }

        public static DateTime? GetDateTimeFromUser(string prompt, DateTime? defaultTime = null)
        {
            Console.WriteLine(prompt);
            DateTime? datePart = HandleInteractiveDateInput(defaultTime ?? DateTime.Now);
            if (datePart == null)
            {
                return null;
            }
            DateTime timePart = HandleInteractiveTimeInput(defaultTime ?? datePart.Value);
            return datePart.Value.Date + timePart.TimeOfDay;
        }

        public static DateTime HandleInteractiveTimeInput(DateTime initialTime)
        {
            DateTime time = initialTime;
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            TimeIncrementMode mode = TimeIncrementMode.Hour;

            // Round initial time to nearest 15 minutes and clear seconds/milliseconds
            time = time.AddMinutes(-(time.Minute % 15));
            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Kind);

            while (true)
            {
                Console.SetCursorPosition(left, top);
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Set time: ");
                
                Console.BackgroundColor = (mode == TimeIncrementMode.Hour) ? ConsoleColor.DarkCyan : ConsoleColor.Black;
                Console.ForegroundColor = (mode == TimeIncrementMode.Hour) ? ConsoleColor.White : ConsoleColor.Gray;
                Console.Write($"{time:HH}");

                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(":");

                Console.BackgroundColor = (mode == TimeIncrementMode.Minute) ? ConsoleColor.DarkCyan : ConsoleColor.Black;
                Console.ForegroundColor = (mode == TimeIncrementMode.Minute) ? ConsoleColor.White : ConsoleColor.Gray;
                Console.Write($"{time:mm}");

                Console.ResetColor();
                Console.Write(" (Use Up/Down to change value, Left/Right to switch, Enter to confirm)      ");

                var key = Console.ReadKey(true);

                // Clear the line for redrawing
                Console.SetCursorPosition(left, top);
                Console.Write(new string(' ', Console.WindowWidth - 1)); 

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        switch (mode)
                        {
                            case TimeIncrementMode.Hour: time = time.AddHours(1); break;
                            case TimeIncrementMode.Minute: time = time.AddMinutes(15); break;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        switch (mode)
                        {
                            case TimeIncrementMode.Hour: time = time.AddHours(-1); break;
                            case TimeIncrementMode.Minute: time = time.AddMinutes(-15); break;
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        mode = mode == TimeIncrementMode.Hour ? TimeIncrementMode.Minute : TimeIncrementMode.Hour;
                        break;
                    case ConsoleKey.Enter:
                        Console.SetCursorPosition(left, top);
                        Console.Write(new string(' ', Console.WindowWidth - 1)); 
                        Console.SetCursorPosition(left, top);
                        Console.WriteLine($"Selected time: {time:HH:mm}");
                        return time;
                }
            }
        }
    }
}
