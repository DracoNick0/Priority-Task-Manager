using System;
using System.Collections.Generic;
using System.Linq;

namespace PriorityTaskManager.CLI.Utils
{
    public static class ConsoleMenuHelper
    {
        public sealed class AdjustableMenuOption
        {
            public AdjustableMenuOption(Func<string> labelFactory, Func<double> getValue, Action<double> setValue, double increment = 0.1, double minimum = 0.1)
            {
                LabelFactory = labelFactory;
                GetValue = getValue;
                SetValue = setValue;
                Increment = increment;
                Minimum = minimum;
            }

            public Func<string> LabelFactory { get; }

            public Func<double> GetValue { get; }

            public Action<double> SetValue { get; }

            public double Increment { get; }

            public double Minimum { get; }
        }

        public static void DrawMenu(List<string> items, int selectedIndex, int startLine = -1)
        {
            if (startLine != -1)
            {
                Console.SetCursorPosition(0, startLine);
            }

            for (int i = 0; i < items.Count; i++)
            {
                ClearCurrentLine();

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

            Console.WriteLine("\n[Use Arrows to Navigate, Enter to Select, Shift+Enter to Save/Confirm]");
        }

        public static void DrawMenuItems(IReadOnlyList<string> items, int selectedIndex, int startLine)
        {
            for (int i = 0; i < items.Count; i++)
            {
                DrawMenuItemLine(startLine + i, items[i], i == selectedIndex);
            }
        }

        public static void UpdateMenuSelection(IReadOnlyList<string> items, int previousIndex, int selectedIndex, int startLine)
        {
            if (previousIndex == selectedIndex)
            {
                DrawMenuItemLine(startLine + selectedIndex, items[selectedIndex], true);
                return;
            }

            DrawMenuItemLine(startLine + previousIndex, items[previousIndex], false);
            DrawMenuItemLine(startLine + selectedIndex, items[selectedIndex], true);
        }

        public static void DrawToggleLine(int row, string label, bool isChecked, bool isHighlighted)
        {
            var selectedForeground = isChecked ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;
            var normalForeground = isChecked ? ConsoleColor.Green : ConsoleColor.Gray;
            DrawLine(row, $"  [{(isChecked ? 'X' : ' ')}] {label}", isHighlighted, selectedForeground, normalForeground);
        }

        public static void DrawMenuItemLine(int row, string item, bool isSelected)
        {
            DrawLine(row, $"{(isSelected ? "> " : "  ")}{item}", isSelected, ConsoleColor.Black);
        }

        public static void ClearLine(int row)
        {
            Console.SetCursorPosition(0, row);
            ClearCurrentLine();
            Console.SetCursorPosition(0, row);
        }

        public static string? PromptInlineInput(int row, string prefix)
        {
            ClearLine(row);
            Console.Write(prefix);
            return Console.ReadLine();
        }

        public static bool TryPromptInlineInput(int row, string prefix, string initialValue, out string value)
        {
            value = initialValue ?? string.Empty;
            var buffer = new List<char>(initialValue ?? string.Empty);

            while (true)
            {
                ClearLine(row);
                Console.Write(prefix);
                Console.Write(new string(buffer.ToArray()));
                Console.SetCursorPosition(prefix.Length + buffer.Count, row);

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    value = new string(buffer.ToArray());
                    return true;
                }

                if (key.Key == ConsoleKey.Escape)
                {
                    value = initialValue ?? string.Empty;
                    return false;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Count > 0)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                    }

                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    buffer.Add(key.KeyChar);
                }
            }
        }

        public static bool RunToggleSelectionMenu<T>(string title, string instructions, IList<T> selectedItems, IReadOnlyList<T> allItems, Func<T, string> labelSelector)
        {
            var originalItems = selectedItems.ToList();
            int selectedIndex = 0;

            Console.WriteLine(title);
            Console.WriteLine(instructions);
            int selectorTop = Console.CursorTop;

            for (int i = 0; i < allItems.Count; i++)
            {
                DrawToggleLine(selectorTop + i, labelSelector(allItems[i]), selectedItems.Contains(allItems[i]), i == selectedIndex);
            }

            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + allItems.Count) % allItems.Count;
                        DrawToggleLine(selectorTop + previousUp, labelSelector(allItems[previousUp]), selectedItems.Contains(allItems[previousUp]), false);
                        DrawToggleLine(selectorTop + selectedIndex, labelSelector(allItems[selectedIndex]), selectedItems.Contains(allItems[selectedIndex]), true);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % allItems.Count;
                        DrawToggleLine(selectorTop + previousDown, labelSelector(allItems[previousDown]), selectedItems.Contains(allItems[previousDown]), false);
                        DrawToggleLine(selectorTop + selectedIndex, labelSelector(allItems[selectedIndex]), selectedItems.Contains(allItems[selectedIndex]), true);
                        break;
                    case ConsoleKey.Spacebar:
                        var item = allItems[selectedIndex];
                        if (selectedItems.Contains(item))
                        {
                            selectedItems.Remove(item);
                        }
                        else
                        {
                            selectedItems.Add(item);
                        }

                        DrawToggleLine(selectorTop + selectedIndex, labelSelector(item), selectedItems.Contains(item), true);
                        break;
                    case ConsoleKey.Enter:
                        return true;
                    case ConsoleKey.Escape:
                        selectedItems.Clear();
                        foreach (var originalItem in originalItems)
                        {
                            selectedItems.Add(originalItem);
                        }

                        return false;
                }
            }
        }

        public static bool RunAdjustableValueMenu(string title, string instructions, IReadOnlyList<AdjustableMenuOption> options, string saveLabel = "Save & Exit", string cancelLabel = "Cancel")
        {
            var originalValues = options.Select(option => option.GetValue()).ToArray();
            int selectedIndex = 0;

            Console.WriteLine(title);
            Console.WriteLine(instructions);
            int selectorTop = Console.CursorTop;

            DrawAdjustableMenu(selectorTop, options, selectedIndex, saveLabel, cancelLabel);

            while (true)
            {
                var key = Console.ReadKey(true);
                int menuItemCount = options.Count + 2;

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + menuItemCount) % menuItemCount;
                        UpdateAdjustableSelection(selectorTop, options, previousUp, selectedIndex, saveLabel, cancelLabel);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % menuItemCount;
                        UpdateAdjustableSelection(selectorTop, options, previousDown, selectedIndex, saveLabel, cancelLabel);
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        if (selectedIndex >= options.Count)
                        {
                            break;
                        }

                        var option = options[selectedIndex];
                        var nextValue = option.GetValue() + (key.Key == ConsoleKey.RightArrow ? option.Increment : -option.Increment);
                        option.SetValue(Math.Max(option.Minimum, nextValue));
                        DrawMenuItemLine(selectorTop + selectedIndex, option.LabelFactory(), true);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == options.Count)
                        {
                            return true;
                        }

                        if (selectedIndex == options.Count + 1)
                        {
                            RestoreAdjustableValues(options, originalValues);
                            return false;
                        }

                        break;
                    case ConsoleKey.Escape:
                        RestoreAdjustableValues(options, originalValues);
                        return false;
                }
            }
        }

        private static void DrawLine(int row, string text, bool isSelected, ConsoleColor selectedForeground, ConsoleColor? normalForeground = null)
        {
            Console.SetCursorPosition(0, row);
            ClearCurrentLine();
            Console.SetCursorPosition(0, row);

            if (isSelected)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Console.ForegroundColor = selectedForeground;
            }
            else if (normalForeground.HasValue)
            {
                Console.ForegroundColor = normalForeground.Value;
            }

            Console.Write(text);
            Console.ResetColor();
        }

        private static void ClearCurrentLine()
        {
            Console.Write(new string(' ', Math.Max(1, Console.WindowWidth - 1)));
            Console.CursorLeft = 0;
        }

        private static void DrawAdjustableMenu(int selectorTop, IReadOnlyList<AdjustableMenuOption> options, int selectedIndex, string saveLabel, string cancelLabel)
        {
            for (int i = 0; i < options.Count; i++)
            {
                DrawMenuItemLine(selectorTop + i, options[i].LabelFactory(), i == selectedIndex);
            }

            DrawMenuItemLine(selectorTop + options.Count, saveLabel, selectedIndex == options.Count);
            DrawMenuItemLine(selectorTop + options.Count + 1, cancelLabel, selectedIndex == options.Count + 1);
        }

        private static void UpdateAdjustableSelection(int selectorTop, IReadOnlyList<AdjustableMenuOption> options, int previousIndex, int selectedIndex, string saveLabel, string cancelLabel)
        {
            DrawAdjustableLine(selectorTop, options, previousIndex, false, saveLabel, cancelLabel);
            DrawAdjustableLine(selectorTop, options, selectedIndex, true, saveLabel, cancelLabel);
        }

        private static void DrawAdjustableLine(int selectorTop, IReadOnlyList<AdjustableMenuOption> options, int index, bool isSelected, string saveLabel, string cancelLabel)
        {
            if (index < options.Count)
            {
                DrawMenuItemLine(selectorTop + index, options[index].LabelFactory(), isSelected);
                return;
            }

            if (index == options.Count)
            {
                DrawMenuItemLine(selectorTop + index, saveLabel, isSelected);
                return;
            }

            DrawMenuItemLine(selectorTop + index, cancelLabel, isSelected);
        }

        private static void RestoreAdjustableValues(IReadOnlyList<AdjustableMenuOption> options, IReadOnlyList<double> originalValues)
        {
            for (int i = 0; i < options.Count; i++)
            {
                options[i].SetValue(originalValues[i]);
            }
        }
    }
}