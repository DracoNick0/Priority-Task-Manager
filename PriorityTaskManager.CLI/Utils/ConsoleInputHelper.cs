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
        /// Prompts the user for an integer within a specified range.
        /// </summary>
        public static int PromptForInt(string prompt, int min, int max, int defaultValue)
        {
            while (true)
            {
                Console.Write($"{prompt} (default: {defaultValue}): ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return defaultValue;
                }

                if (int.TryParse(input, out int result) && result >= min && result <= max)
                {
                    return result;
                }

                Console.WriteLine($"Invalid input. Please enter a number between {min} and {max}.");
            }
        }

        /// <summary>
        /// Prompts the user for a yes/no answer.
        /// </summary>
        public static bool PromptForBool(string prompt, bool defaultValue)
        {
            while (true)
            {
                Console.Write($"{prompt} (y/n, default: {(defaultValue ? "y" : "n")}): ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return defaultValue;
                }

                if (input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (input.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
            }
        }

        /// <summary>
        /// Prompts the user for a duration string (e.g., "30m", "1.5h").
        /// </summary>
        public static TimeSpan PromptForDuration(string prompt, TimeSpan defaultValue)
        {
            while (true)
            {
                Console.Write($"{prompt} (default: {defaultValue.TotalHours}h): ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return defaultValue;
                }

                if (TryParseDuration(input, out TimeSpan duration))
                {
                    return duration;
                }

                Console.WriteLine("Invalid format. Use '30m' for minutes or '1.5h' for hours.");
            }
        }

        private static bool TryParseDuration(string input, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
            input = input.Trim().ToLower();

            if (input.EndsWith("m"))
            {
                if (double.TryParse(input.TrimEnd('m'), out double minutes))
                {
                    duration = TimeSpan.FromMinutes(minutes);
                    return true;
                }
            }
            else if (input.EndsWith("h"))
            {
                if (double.TryParse(input.TrimEnd('h'), out double hours))
                {
                    duration = TimeSpan.FromHours(hours);
                    return true;
                }
            }
            else
            {
                if (double.TryParse(input, out double hours))
                {
                    duration = TimeSpan.FromHours(hours);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Handles interactive date input, allowing users to adjust and confirm a date.
        /// </summary>
        /// <param name="initialDate">The initial date to start the adjustment from. Can be null.</param>
        /// <returns>The adjusted and confirmed date, or original if cancelled.</returns>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public static DateTime? HandleInteractiveDateInput(DateTime? initialDate)
        {
            DateTime date = initialDate ?? DateTime.Today;
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            IncrementMode mode = IncrementMode.Day; // Default to adjusting Day

            if (!initialDate.HasValue)
            {
                // If starting from nothing, default to today
                date = DateTime.Today;
            }

            // Hide cursor for cleaner UI
            bool originalCursorVisible = Console.CursorVisible;
            Console.CursorVisible = false;

            try
            {
                while (true)
                {
                    Console.SetCursorPosition(left, top);
                    
                    // Clear line
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.SetCursorPosition(left, top);

                    string modeStr = mode.ToString();
                    if (mode == IncrementMode.NoDueDate) { modeStr = "No Due Date"; }

                    if (mode == IncrementMode.NoDueDate)
                    {
                        Console.Write($"[Mode: {modeStr}] (Press Enter to confirm, Esc to cancel)      ");
                    }
                    else
                    {
                        Console.Write($"[Mode: {modeStr}] {date:yyyy-MM-dd dddd} (Arrows to adjust, Enter to confirm, Esc to cancel)");
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
                            // Cycle through modes: Day -> Week -> Month -> Year -> No Due Date -> Day
                            int nextMode = ((int)mode + 1);
                            if (nextMode > 4) nextMode = 0; // Assuming NoDueDate is 4 or similar, need to check enum
                            // Re-implementing simplified toggle
                            if (mode == IncrementMode.NoDueDate) mode = IncrementMode.Day;
                            else if (mode == IncrementMode.Year) mode = IncrementMode.NoDueDate;
                            else mode = (IncrementMode)((int)mode + 1);
                            break;
                        case ConsoleKey.DownArrow:
                             // Cycle backwards
                            if (mode == IncrementMode.Day) mode = IncrementMode.NoDueDate;
                            else if (mode == IncrementMode.NoDueDate) mode = IncrementMode.Year;
                            else mode = (IncrementMode)((int)mode - 1);
                            break;
                        case ConsoleKey.Enter:
                            Console.WriteLine();
                            if (mode == IncrementMode.NoDueDate)
                            {
                                return null;
                            }
                            return date;
                        case ConsoleKey.Escape:
                            Console.WriteLine();
                            return initialDate; // Return original unchanged
                    }
                }
            }
            finally
            {
                Console.CursorVisible = originalCursorVisible;
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

        private enum TimeIncrementMode { Hour, MinuteTens, MinuteOnes, EndOfDay }

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

            // Round initial time to nearest minute and clear seconds/milliseconds
            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Kind);

            while (true)
            {
                // Prepare line for drawing
                Console.SetCursorPosition(left, top);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(left, top);

                Console.ResetColor();
                Console.Write("Set time: ");

                // Draw Hour
                if (mode == TimeIncrementMode.Hour) { Console.BackgroundColor = ConsoleColor.White; Console.ForegroundColor = ConsoleColor.Black; }
                else { Console.ResetColor(); }
                Console.Write($"{time:HH}");
                Console.ResetColor();

                Console.Write(":");

                // Draw Minute Tens
                if (mode == TimeIncrementMode.MinuteTens) { Console.BackgroundColor = ConsoleColor.White; Console.ForegroundColor = ConsoleColor.Black; }
                else { Console.ResetColor(); }
                Console.Write($"{time.Minute / 10}");
                Console.ResetColor();

                // Draw Minute Ones
                if (mode == TimeIncrementMode.MinuteOnes) { Console.BackgroundColor = ConsoleColor.White; Console.ForegroundColor = ConsoleColor.Black; }
                else { Console.ResetColor(); }
                Console.Write($"{time.Minute % 10}");
                Console.ResetColor();

                Console.Write(" ");

                // Draw End of Day Button
                if (mode == TimeIncrementMode.EndOfDay) { Console.BackgroundColor = ConsoleColor.White; Console.ForegroundColor = ConsoleColor.Black; }
                else { Console.ResetColor(); }
                Console.Write("[End of Day]");
                Console.ResetColor();

                Console.Write(" (Arrows to adjust/move, Enter to confirm, Esc to cancel)");

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (mode == TimeIncrementMode.Hour) time = time.AddHours(1);
                        else if (mode == TimeIncrementMode.MinuteTens)
                        {
                            int newTens = (time.Minute / 10 + 1) % 6;
                            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, newTens * 10 + (time.Minute % 10), 0);
                        }
                        else if (mode == TimeIncrementMode.MinuteOnes)
                        {
                            int newOnes = (time.Minute % 10 + 1) % 10;
                            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, (time.Minute / 10) * 10 + newOnes, 0);
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (mode == TimeIncrementMode.Hour) time = time.AddHours(-1);
                        else if (mode == TimeIncrementMode.MinuteTens)
                        {
                            int newTens = (time.Minute / 10 - 1 + 6) % 6;
                            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, newTens * 10 + (time.Minute % 10), 0);
                        }
                        else if (mode == TimeIncrementMode.MinuteOnes)
                        {
                            int newOnes = (time.Minute % 10 - 1 + 10) % 10;
                            time = new DateTime(time.Year, time.Month, time.Day, time.Hour, (time.Minute / 10) * 10 + newOnes, 0);
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        if (mode == TimeIncrementMode.Hour) mode = TimeIncrementMode.EndOfDay;
                        else mode = (TimeIncrementMode)((int)mode - 1);
                        break;
                    case ConsoleKey.RightArrow:
                         if (mode == TimeIncrementMode.EndOfDay) mode = TimeIncrementMode.Hour;
                        else mode = (TimeIncrementMode)((int)mode + 1);
                        break;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        if (mode == TimeIncrementMode.EndOfDay)
                        {
                            return time.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                        }
                        return time;
                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        return initialTime; // Return original unchanged
                }
            }
        }
    }
}
