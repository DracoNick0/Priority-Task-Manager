using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Handlers
{
    public class SettingsHandler : ICommandHandler
    {
        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                RunInteractiveSettings(service);
            }
            else
            {
                ParseArguments(service, args);
            }
        }

        private void ParseArguments(TaskManagerService service, string[] args)
        {
            var userProfile = service.GetUserProfile();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--working-days":
                        if (i + 1 < args.Length)
                        {
                            UpdateWorkingDays(userProfile, args[i + 1]);
                            i++; // consume value
                        }
                        break;
                    case "--working-hours":
                        if (i + 1 < args.Length)
                        {
                            UpdateWorkingHours(userProfile, args[i + 1]);
                            i++; // consume value
                        }
                        break;
                }
            }

            service.SaveUserProfile(userProfile);
            Console.WriteLine("Settings updated.");
            PrintCurrentSettings(userProfile);
        }

        private void RunInteractiveSettings(TaskManagerService service)
        {
            var userProfile = service.GetUserProfile();
            var menuItems = new List<string> { "Working Days", "Working Hours", "Save and Exit" };
            int selectedIndex = 0;

            Console.CursorVisible = false;
            while (true)
            {
                Console.Clear();
                PrintCurrentSettings(userProfile);
                DrawMenu(menuItems, selectedIndex);

                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + menuItems.Count) % menuItems.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % menuItems.Count;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == menuItems.Count - 1) // Save and Exit
                        {
                            service.SaveUserProfile(userProfile);
                            Console.Clear();
                            Console.WriteLine("Settings saved.");
                            PrintCurrentSettings(userProfile);
                            Console.CursorVisible = true;
                            return;
                        }
                        HandleMenuSelection(selectedIndex, userProfile);
                        break;
                    case ConsoleKey.Escape:
                        Console.Clear();
                        Console.WriteLine("Settings update cancelled.");
                        Console.CursorVisible = true;
                        return;
                }
            }
        }

        private void HandleMenuSelection(int selectedIndex, UserProfile userProfile)
        {
            switch (selectedIndex)
            {
                case 0: // Working Days
                    RunInteractiveDaySelector(userProfile);
                    break;
                case 1: // Working Hours
                    Console.CursorVisible = true;
                    Console.SetCursorPosition(0, Console.CursorTop + 3);
                    Console.Write("Enter new working hours (e.g., 09:00-17:00): ");
                    var hoursInput = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(hoursInput))
                    {
                        UpdateWorkingHours(userProfile, hoursInput);
                    }
                    Console.CursorVisible = false;
                    break;
            }
        }

        private void RunInteractiveDaySelector(UserProfile userProfile)
        {
            var allDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList();
            int selectedDayIndex = 0;
            var originalWorkDays = new List<DayOfWeek>(userProfile.WorkDays);

            Console.CursorVisible = false;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Select your working days (Space to toggle, Enter to save, Esc to cancel):");
                DrawDaySelector(allDays, userProfile.WorkDays, selectedDayIndex);

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedDayIndex = (selectedDayIndex - 1 + allDays.Count) % allDays.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedDayIndex = (selectedDayIndex + 1) % allDays.Count;
                        break;
                    case ConsoleKey.Spacebar:
                        var dayToToggle = allDays[selectedDayIndex];
                        if (userProfile.WorkDays.Contains(dayToToggle))
                        {
                            userProfile.WorkDays.Remove(dayToToggle);
                        }
                        else
                        {
                            userProfile.WorkDays.Add(dayToToggle);
                        }
                        break;
                    case ConsoleKey.Enter:
                        return; // Return to main settings menu
                    case ConsoleKey.Escape:
                        userProfile.WorkDays = originalWorkDays; // Revert changes
                        return; // Return to main settings menu
                }
            }
        }

        private void DrawDaySelector(List<DayOfWeek> allDays, List<DayOfWeek> selectedDays, int selectedIndex)
        {
            for (int i = 0; i < allDays.Count; i++)
            {
                var day = allDays[i];
                bool isSelected = selectedDays.Contains(day);

                if (i == selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = isSelected ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = isSelected ? ConsoleColor.Green : ConsoleColor.Gray;
                }

                Console.WriteLine($"  [{(isSelected ? 'X' : ' ')}] {day}");
                Console.ResetColor();
            }
        }


        private bool UpdateWorkingDays(UserProfile profile, string days)
        {
            var dayList = new List<DayOfWeek>();
            foreach (var day in days.Split(','))
            {
                if (Enum.TryParse(day.Trim(), true, out DayOfWeek dayOfWeek))
                {
                    dayList.Add(dayOfWeek);
                }
            }
            if (dayList.Any())
            {
                profile.WorkDays = dayList;
                return true;
            }
            return false;
        }

        private bool UpdateWorkingHours(UserProfile profile, string hours)
        {
            var parts = hours.Split('-');
            if (parts.Length == 2 &&
                TimeOnly.TryParse(parts[0].Trim(), out TimeOnly start) &&
                TimeOnly.TryParse(parts[1].Trim(), out TimeOnly end))
            {
                profile.WorkStartTime = start;
                profile.WorkEndTime = end;
                return true;
            }
            return false;
        }

        private void DrawMenu(List<string> items, int selectedIndex)
        {
            Console.WriteLine("\nSelect a setting to edit:");
            for (int i = 0; i < items.Count; i++)
            {
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

        private void PrintCurrentSettings(UserProfile userProfile)
        {
            Console.WriteLine("Current Settings:");
            Console.WriteLine($"  Working Days: {string.Join(", ", userProfile.WorkDays)}");
            Console.WriteLine($"  Working Hours: {userProfile.WorkStartTime} - {userProfile.WorkEndTime}");
        }
    }
}
