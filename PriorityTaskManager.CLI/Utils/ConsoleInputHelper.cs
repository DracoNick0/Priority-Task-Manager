using System;
using System.Collections.Generic;

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
        /// <param name="initialDate">The initial date to start the adjustment from.</param>
        /// <returns>The adjusted and confirmed date.</returns>
        public static DateTime HandleInteractiveDateInput(DateTime initialDate)
        {
            DateTime date = initialDate;
            int left = Console.CursorLeft;
            int top = Console.CursorTop;
            IncrementMode mode = IncrementMode.Day;

            while (true)
            {
                Console.SetCursorPosition(left, top);
                Console.Write($"[Mode: {mode}] {date:yyyy-MM-dd dddd}      ");

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.RightArrow:
                        switch (mode)
                        {
                            case IncrementMode.Day: date = date.AddDays(1); break;
                            case IncrementMode.Week: date = date.AddDays(7); break;
                            case IncrementMode.Month: date = date.AddMonths(1); break;
                            case IncrementMode.Year: date = date.AddYears(1); break;
                        }
                        break;
                    case ConsoleKey.LeftArrow:
                        switch (mode)
                        {
                            case IncrementMode.Day: date = date.AddDays(-1); break;
                            case IncrementMode.Week: date = date.AddDays(-7); break;
                            case IncrementMode.Month: date = date.AddMonths(-1); break;
                            case IncrementMode.Year: date = date.AddYears(-1); break;
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        mode = (IncrementMode)(((int)mode + 1) % 4);
                        break;
                    case ConsoleKey.DownArrow:
                        mode = (IncrementMode)(((int)mode + 3) % 4);
                        break;
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return date;
                }
            }
        }

        /// <summary>
        /// Parses and validates task IDs from user input.
        /// </summary>
        /// <param name="service">The TaskManagerService instance to validate task existence.</param>
        /// <param name="args">The command-line arguments containing task IDs.</param>
        /// <returns>A list of valid task IDs.</returns>
        public static List<int> ParseAndValidateTaskIds(TaskManagerService service, string[] args)
        {
            var validTaskIds = new List<int>();

            if (args == null || args.Length == 0)
            {
                return validTaskIds;
            }

            string input = string.Join("", args);
            string[] potentialIds = input.Split(',');

            foreach (var idString in potentialIds)
            {
                string trimmedId = idString.Trim();

                if (int.TryParse(trimmedId, out int taskId))
                {
                    if (service.GetTaskById(taskId) != null)
                    {
                        validTaskIds.Add(taskId);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Task ID {taskId} does not exist and will be skipped.");
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: '{trimmedId}' is not a valid task ID and will be skipped.");
                }
            }

            return validTaskIds;
        }

        /// <summary>
        /// Represents the modes for incrementing the date during interactive input.
        /// </summary>
        private enum IncrementMode { Day, Week, Month, Year }
    }
}
