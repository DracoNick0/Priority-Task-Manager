using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class SettingsHandler : ICommandHandler
    {
        private readonly ITimeService _timeService;

        public SettingsHandler(ITimeService timeService)
        {
            _timeService = timeService;
        }

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

            service.UpdateUserProfile(userProfile);
            Console.WriteLine("Settings updated.");
            PrintCurrentSettings(userProfile);
        }

        private void RunInteractiveSettings(TaskManagerService service)
        {
            var userProfile = service.GetUserProfile();
            var menuItems = new List<string> 
            { 
                "Working Days", 
                "Working Hours", 
                $"Scheduling Strategy: [{userProfile.SchedulingMode}]",
                "Set Simulated Time (Use 'Time' command for more options)",
                "Run Cleanup (Archive/Re-Index)",
                "Save and Exit" 
            };
            int selectedIndex = 0;

            Console.CursorVisible = false;
            while (true)
            {
                Console.Clear();
                // Refresh dynamic menu items
                menuItems[2] = $"Scheduling Strategy: [{userProfile.SchedulingMode}]";

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
                        if (selectedIndex == 5) // Save and Exit
                        {
                            service.UpdateUserProfile(userProfile);
                            Console.Clear();
                            Console.WriteLine("Settings saved.");
                            PrintCurrentSettings(userProfile);
                            Console.CursorVisible = true;
                            return;
                        }
                        HandleMenuSelection(selectedIndex, service, userProfile);
                        break;
                    case ConsoleKey.Escape:
                        Console.Clear();
                        Console.WriteLine("Settings update cancelled.");
                        Console.CursorVisible = true;
                        return;
                }
            }
        }

        private void HandleMenuSelection(int selectedIndex, TaskManagerService service, UserProfile userProfile)
        {
            switch (selectedIndex)
            {
                case 0: // Working Days
                    RunInteractiveDaySelector(userProfile);
                    break;
                case 1: // Working Hours
                    Console.CursorVisible = true;
                    // Start Time
                    Console.WriteLine("\nSet Daily Work Start Time:");
                    var today = DateTime.Today;
                    var startDateTime = today.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var newStart = ConsoleInputHelper.HandleInteractiveTimeInput(startDateTime);
                    
                    // End Time
                    Console.WriteLine("\nSet Daily Work End Time:");
                    var endDateTime = today.Add(userProfile.WorkEndTime.ToTimeSpan());
                    var newEnd = ConsoleInputHelper.HandleInteractiveTimeInput(endDateTime);

                    userProfile.WorkStartTime = TimeOnly.FromDateTime(newStart);
                    userProfile.WorkEndTime = TimeOnly.FromDateTime(newEnd);
                    
                    Console.CursorVisible = false;
                    break;
                case 2: // Scheduling Strategy
                    // Toggle between modes
                    userProfile.SchedulingMode = userProfile.SchedulingMode == SchedulingMode.GoldPanning 
                        ? SchedulingMode.ConstraintOptimization 
                        : SchedulingMode.GoldPanning;
                    break;
                case 3: // Set Simulated Time
                    // New Submenu for Time
                    Console.WriteLine("\nSet Time: [1] Now [2] Custom");
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == '1')
                    {
                        var timeHandler = new TimeHandler(_timeService);
                        timeHandler.Execute(service, new[] { "now" });
                        Console.ReadKey(true); // Pause to see result
                    }
                    else if (key.KeyChar == '2')
                    {
                        var timeHandler = new TimeHandler(_timeService);
                        timeHandler.Execute(service, new[] { "custom" });
                        Console.ReadKey(true); 
                    }
                    break;
                case 4: // Run Cleanup
                    var cleanupHandler = new CleanupHandler(service);
                    cleanupHandler.Execute(service, new string[0]);
                    Console.ReadKey(true);
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
            Console.WriteLine($"  Scheduling Strategy: {userProfile.SchedulingMode}");
        }
    }
}
