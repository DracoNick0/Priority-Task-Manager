using System;

namespace PriorityTaskManager.CLI.Utils
{
    public static class ConsoleInputHelper
    {
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

        public enum IncrementMode { Day, Week, Month, Year }
    }
}
