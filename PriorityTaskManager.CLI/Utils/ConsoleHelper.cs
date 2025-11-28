using System;
using System.Collections.Generic;

namespace PriorityTaskManager.CLI.Utils
{
    public static class ConsoleHelper
    {
        public static void DrawMenu(List<string> items, int selectedIndex, int startLine = -1)
        {
            if (startLine != -1)
            {
                Console.SetCursorPosition(0, startLine);
            }
            for (int i = 0; i < items.Count; i++)
            {
                // Clear the line
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.CursorLeft = 0;

                if (i == selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.WriteLine($"> {items[i]}");
                }
                else
                {
                    Console.WriteLine($"  {items[i]}");
                }
                Console.ResetColor();
            }
        }
    }
}
