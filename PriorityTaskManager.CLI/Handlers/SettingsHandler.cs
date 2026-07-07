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
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;

        public SettingsHandler(ITimeService timeService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            _timeService = timeService;
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
        }

        public void Execute(TaskManagerService service, string[] args)
        {
            if (args.Length > 0 && args[0].Equals("defaults", StringComparison.OrdinalIgnoreCase))
            {
                ParseArguments(service, args.Skip(1).ToArray());
                return;
            }

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

            _snapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--default-sort":
                        if (i + 1 < args.Length)
                        {
                            UpdateDefaultSortOption(userProfile, args[i + 1]);
                            i++; // consume value
                        }
                        break;
                    case "--default-mode":
                    case "--default-scheduling":
                        if (i + 1 < args.Length && TryParseSchedulingMode(args[i + 1], out var schedulingMode))
                        {
                            userProfile.SchedulingMode = schedulingMode;
                            i++; // consume value
                        }
                        else
                        {
                            Console.WriteLine("Error: --default-mode requires one of: gold, solver.");
                        }
                        break;
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
                    case "--set-slack":
                        if (i + 4 < args.Length &&
                            double.TryParse(args[i + 1], out double dire) &&
                            double.TryParse(args[i + 2], out double pressing) &&
                            double.TryParse(args[i + 3], out double focus) &&
                            double.TryParse(args[i + 4], out double safe))
                        {
                            userProfile.SlackThresholdDire = dire;
                            userProfile.SlackThresholdPressing = pressing;
                            userProfile.SlackThresholdFocus = focus;
                            userProfile.SlackThresholdSafe = safe;
                            i += 4;
                        }
                        else
                        {
                            Console.WriteLine("Error: --set-slack requires 4 numeric arguments (Dire Pressing Focus Safe).");
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
            int selectedIndex = 0;

            Console.CursorVisible = false;
            while (true)
            {
                ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                var menuItems = new List<string> 
                { 
                    "Working Days", 
                    "Working Hours", 
                    $"Default List Sort: [{userProfile.DefaultListSortOption}]",
                    $"Default Scheduling Mode: [{userProfile.SchedulingMode}]",
                    "Urgency Thresholds (Slack)", // New Menu Item
                    "Save and Exit" 
                };

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
                            ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            Console.WriteLine("Defaults saved.");
                            PrintCurrentSettings(userProfile);
                            Console.CursorVisible = true;
                            return;
                        }
                        HandleMenuSelection(selectedIndex, service, userProfile);
                        break;
                    case ConsoleKey.Escape:
                        ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
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
                case 2: // Default List Sort
                    Console.CursorVisible = true;
                    Console.Write("Enter default list sort (default, alphabetical, due, id): ");
                    var defaultSortInput = Console.ReadLine() ?? string.Empty;
                    if (TryParseSortOption(defaultSortInput, out var defaultSortOption))
                    {
                        userProfile.DefaultListSortOption = defaultSortOption;
                    }
                    Console.CursorVisible = false;
                    break;
                case 3: // Default Scheduling Mode
                    // Toggle between modes
                    userProfile.SchedulingMode = userProfile.SchedulingMode == SchedulingMode.GoldPanning 
                        ? SchedulingMode.ConstraintOptimization 
                        : SchedulingMode.GoldPanning;
                    break;
                case 4: // Urgency Thresholds
                    RunInteractiveSlackSelector(userProfile);
                    break;
            }
        }

        private bool TryParseSchedulingMode(string input, out SchedulingMode mode)
        {
            mode = SchedulingMode.GoldPanning;
            switch (input.Trim().ToLowerInvariant())
            {
                case "gold":
                case "goldpanning":
                    mode = SchedulingMode.GoldPanning;
                    return true;
                case "solver":
                case "constraint":
                case "constraintoptimization":
                    mode = SchedulingMode.ConstraintOptimization;
                    return true;
                default:
                    return false;
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
                ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
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

        private void RunInteractiveSlackSelector(UserProfile userProfile)
        {
            int selectedIndex = 0;
            double increment = 0.5;

            while (true)
            {
                ConsoleHelper.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                Console.WriteLine("Adjust Urgency Thresholds (Multipliers of Average Work Day):");
                Console.WriteLine("Use Left/Right to adjust values, Enter to save.");

                // Map current values to display
                var items = new List<string>
                {
                    $"Dire     (< {userProfile.SlackThresholdDire:F1} days) [Red]",
                    $"Pressing (< {userProfile.SlackThresholdPressing:F1} days) [DarkYellow]",
                    $"Focus    (< {userProfile.SlackThresholdFocus:F1} days) [Yellow]",
                    $"Safe     (< {userProfile.SlackThresholdSafe:F1} days) [Green]",
                    "Back to Settings"
                };

                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"> {items[i]}");
                    }
                    else
                    {
                        Console.ResetColor();
                        Console.WriteLine($"  {items[i]}");
                    }
                    Console.ResetColor();
                }

                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex = (selectedIndex - 1 + items.Count) % items.Count;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex = (selectedIndex + 1) % items.Count;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                     // Return regardless of selection (values are live-updated in memory)
                    return;
                }
                else if (selectedIndex < 4) // Adjust numeric values
                {
                    double currentVal = 0;
                    if (selectedIndex == 0) currentVal = userProfile.SlackThresholdDire;
                    if (selectedIndex == 1) currentVal = userProfile.SlackThresholdPressing;
                    if (selectedIndex == 2) currentVal = userProfile.SlackThresholdFocus;
                    if (selectedIndex == 3) currentVal = userProfile.SlackThresholdSafe;

                    if (key.Key == ConsoleKey.LeftArrow) currentVal = Math.Max(0.1, currentVal - increment);
                    if (key.Key == ConsoleKey.RightArrow) currentVal = currentVal + increment;
                    
                    if (selectedIndex == 0) userProfile.SlackThresholdDire = currentVal;
                    if (selectedIndex == 1) userProfile.SlackThresholdPressing = currentVal;
                    if (selectedIndex == 2) userProfile.SlackThresholdFocus = currentVal;
                    if (selectedIndex == 3) userProfile.SlackThresholdSafe = currentVal;
                }
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

        private bool TryParseSortOption(string input, out SortOption sortOption)
        {
            sortOption = SortOption.Default;
            var normalized = input.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "default":
                    sortOption = SortOption.Default;
                    return true;
                case "alpha":
                case "alphabetical":
                    sortOption = SortOption.Alphabetical;
                    return true;
                case "due":
                case "duedate":
                    sortOption = SortOption.DueDate;
                    return true;
                case "id":
                    sortOption = SortOption.Id;
                    return true;
                default:
                    return false;
            }
        }

        private bool UpdateDefaultSortOption(UserProfile profile, string sort)
        {
            if (TryParseSortOption(sort, out var sortOption))
            {
                profile.DefaultListSortOption = sortOption;
                return true;
            }
            return false;
        }

        private void DrawMenu(List<string> items, int selectedIndex)
        {
            Console.WriteLine("\nSelect a default to edit:");
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
            Console.WriteLine("Current Defaults:");
            Console.WriteLine($"  Working Days: {string.Join(", ", userProfile.WorkDays.OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d))}");
            Console.WriteLine($"  Working Hours: {userProfile.WorkStartTime} - {userProfile.WorkEndTime}");
            Console.WriteLine($"  Default List Sort: {userProfile.DefaultListSortOption}");
            Console.WriteLine($"  Default Scheduling Mode: {userProfile.SchedulingMode}");
        }
    }
}
