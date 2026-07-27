using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Utils;

namespace PriorityTaskManager.CLI.Handlers
{
    public class SettingsHandler : ICommandResultHandler
    {
        private readonly ITimeService _timeService;
        private readonly ScheduleSnapshotProvider _snapshotProvider;
        private readonly ITaskMetricsService _taskMetricsService;
        private readonly IInteractiveConsoleFacade _console;

        public SettingsHandler(ITimeService timeService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
            : this(timeService, snapshotProvider, taskMetricsService, null)
        {
        }

        public SettingsHandler(ITimeService timeService, ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService, IInteractiveConsoleFacade? console)
        {
            _timeService = timeService;
            _snapshotProvider = snapshotProvider;
            _taskMetricsService = taskMetricsService;
            _console = console ?? new InteractiveConsoleFacade();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The interactive <c>defaults</c> menu (no-arg invocation) already owns its console
        /// rendering end-to-end through <see cref="IInteractiveConsoleFacade"/>. It returns an
        /// inert <see cref="CommandResult"/> because no dashboard refresh or message is deferred
        /// to <c>Program.cs</c>. The flag-based (<c>--default-*</c>) path returns a real
        /// <see cref="CommandResult"/> built by <see cref="ParseArguments"/>.
        /// </remarks>
        public CommandResult ExecuteWithResult(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                InteractiveSettings(service);
                return new CommandResult
                {
                    Status = CommandResultStatus.Info,
                    Message = string.Empty,
                    ShouldRefreshDashboard = false
                };
            }

            return ParseArguments(service, args);
        }

        private CommandResult ParseArguments(TaskManagerService service, string[] args)
        {
            var userProfile = service.GetUserProfile();
            var messageBuilder = new StringBuilder();
            var appliedAny = false;
            var hadError = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--default-sort":
                        if (i + 1 < args.Length)
                        {
                            if (UpdateDefaultSortOption(userProfile, args[i + 1]))
                            {
                                appliedAny = true;
                            }
                            else
                            {
                                messageBuilder.AppendLine($"Error: '{args[i + 1]}' is not a valid sort option.");
                                hadError = true;
                            }
                            i++; // consume value
                        }
                        break;
                    case "--default-mode":
                    case "--default-scheduling":
                        if (i + 1 < args.Length && TryParseSchedulingMode(args[i + 1], out var schedulingMode))
                        {
                            userProfile.SchedulingMode = schedulingMode;
                            appliedAny = true;
                            i++; // consume value
                        }
                        else
                        {
                            messageBuilder.AppendLine("Error: --default-mode requires one of: gold, solver.");
                            hadError = true;
                            if (i + 1 < args.Length)
                            {
                                i++; // consume value
                            }
                        }
                        break;
                    case "--working-days":
                        if (i + 1 < args.Length)
                        {
                            if (UpdateWorkingDays(userProfile, args[i + 1]))
                            {
                                appliedAny = true;
                            }
                            else
                            {
                                messageBuilder.AppendLine($"Error: '{args[i + 1]}' contains no valid working days.");
                                hadError = true;
                            }
                            i++; // consume value
                        }
                        break;
                    case "--working-hours":
                        if (i + 1 < args.Length)
                        {
                            if (UpdateWorkingHours(userProfile, args[i + 1]))
                            {
                                appliedAny = true;
                            }
                            else
                            {
                                messageBuilder.AppendLine($"Error: '{args[i + 1]}' is not a valid working hours range.");
                                hadError = true;
                            }
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
                            appliedAny = true;
                            i += 4;
                        }
                        else
                        {
                            messageBuilder.AppendLine("Error: --set-slack requires 4 numeric arguments (Dire Pressing Focus Safe).");
                            hadError = true;
                        }
                        break;
                }
            }

            if (appliedAny)
            {
                service.UpdateUserProfile(userProfile);
                messageBuilder.AppendLine("Settings updated.");
                AppendCurrentSettings(messageBuilder, userProfile);
            }

            return new CommandResult
            {
                Status = hadError ? CommandResultStatus.Warning : CommandResultStatus.Success,
                Message = messageBuilder.ToString().TrimEnd(),
                ShouldRefreshDashboard = appliedAny
            };
        }


        private void InteractiveSettings(TaskManagerService service)
        {
            var userProfile = service.GetUserProfile();
            int selectedIndex = 0;

            _console.CursorVisible = false;
            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
            var menuItems = BuildSettingsMenuItems(userProfile);
            PrintCurrentSettings(userProfile);
            _console.WriteLine("\nSelect a default to edit:");
            int selectorTop = _console.CursorTop;
            _console.DrawMenuItems(menuItems, selectedIndex, selectorTop);

            while (true)
            {
                var key = _console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + menuItems.Count) % menuItems.Count;
                        _console.UpdateMenuSelection(menuItems, previousUp, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % menuItems.Count;
                        _console.UpdateMenuSelection(menuItems, previousDown, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == menuItems.Count - 2)
                        {
                            service.UpdateUserProfile(userProfile);
                            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            _console.WriteLine("Defaults saved.");
                            PrintCurrentSettings(userProfile);
                            _console.CursorVisible = true;
                            return;
                        }
                        if (selectedIndex == menuItems.Count - 1)
                        {
                            _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                            _console.WriteLine("Defaults update cancelled.");
                            _console.CursorVisible = true;
                            return;
                        }

                        HandleMenuSelection(userProfile, selectedIndex);
                        menuItems = BuildSettingsMenuItems(userProfile);
                        _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                        PrintCurrentSettings(userProfile);
                        _console.WriteLine("\nSelect a default to edit:");
                        selectorTop = _console.CursorTop;
                        _console.DrawMenuItems(menuItems, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Escape:
                        _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                        _console.WriteLine("Defaults update cancelled.");
                        _console.CursorVisible = true;
                        return;
                }
            }
        }

        private void HandleMenuSelection(UserProfile userProfile, int selectedIndex)
        {
            switch (selectedIndex)
            {
                case 0: // Working Days
                    _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    _console.RunToggleSelectionMenu(
                        "Select your working days:",
                        "Space to toggle, Enter to save, Esc to cancel.",
                        userProfile.WorkDays,
                        Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList(),
                        day => day.ToString());
                    break;
                case 1: // Working Hours
                    _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    _console.CursorVisible = true;
                    // Start Time
                    _console.WriteLine("Set Daily Work Start Time:");
                    var today = DateTime.Today;
                    var startDateTime = today.Add(userProfile.WorkStartTime.ToTimeSpan());
                    var newStart = ConsoleInputHelper.InteractiveTimeInput(startDateTime);
                    
                    // End Time
                    _console.WriteLine("\nSet Daily Work End Time:");
                    var endDateTime = today.Add(userProfile.WorkEndTime.ToTimeSpan());
                    var newEnd = ConsoleInputHelper.InteractiveTimeInput(endDateTime);

                    userProfile.WorkStartTime = TimeOnly.FromDateTime(newStart);
                    userProfile.WorkEndTime = TimeOnly.FromDateTime(newEnd);
                    
                    _console.CursorVisible = false;
                    break;
                case 2: // Default List Sort
                    userProfile.DefaultListSortOption = GetNextSortOption(userProfile.DefaultListSortOption);
                    break;
                case 3: // Default Scheduling Mode
                    userProfile.SchedulingMode = GetNextSchedulingMode(userProfile.SchedulingMode);
                    break;
                case 4: // Urgency Thresholds
                    _console.ClearAndRenderDashboard(_snapshotProvider, _taskMetricsService);
                    _console.RunAdjustableValueMenu(
                        "Adjust urgency thresholds:",
                        "Use Left/Right to adjust values.",
                        BuildSlackMenuOptions(userProfile));
                    break;
            }
        }

        private List<string> BuildSettingsMenuItems(UserProfile userProfile)
        {
            return new List<string>
            {
                "Working Days",
                "Working Hours",
                $"Default List Sort: [{userProfile.DefaultListSortOption}]",
                $"Default Scheduling Mode: [{userProfile.SchedulingMode}]",
                "Urgency Thresholds (Slack)",
                "Save & Exit",
                "Cancel"
            };
        }

        private List<ConsoleMenuHelper.AdjustableMenuOption> BuildSlackMenuOptions(UserProfile userProfile)
        {
            return new List<ConsoleMenuHelper.AdjustableMenuOption>
            {
                new(() => $"Dire     (< {userProfile.SlackThresholdDire:F1} days) [Red]", () => userProfile.SlackThresholdDire, value => userProfile.SlackThresholdDire = value),
                new(() => $"Pressing (< {userProfile.SlackThresholdPressing:F1} days) [DarkYellow]", () => userProfile.SlackThresholdPressing, value => userProfile.SlackThresholdPressing = value),
                new(() => $"Focus    (< {userProfile.SlackThresholdFocus:F1} days) [Yellow]", () => userProfile.SlackThresholdFocus, value => userProfile.SlackThresholdFocus = value),
                new(() => $"Safe     (< {userProfile.SlackThresholdSafe:F1} days) [Green]", () => userProfile.SlackThresholdSafe, value => userProfile.SlackThresholdSafe = value)
            };
        }

        private static SchedulingMode GetNextSchedulingMode(SchedulingMode currentMode)
        {
            return currentMode == SchedulingMode.GoldPanning
                ? SchedulingMode.ConstraintOptimization
                : SchedulingMode.GoldPanning;
        }

        private static SortOption GetNextSortOption(SortOption currentSortOption)
        {
            return currentSortOption switch
            {
                SortOption.Default => SortOption.Alphabetical,
                SortOption.Alphabetical => SortOption.DueDate,
                SortOption.DueDate => SortOption.Id,
                _ => SortOption.Default
            };
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

        private void PrintCurrentSettings(UserProfile userProfile)
        {
            var messageBuilder = new StringBuilder();
            AppendCurrentSettings(messageBuilder, userProfile);
            _console.Write(messageBuilder.ToString());
        }

        private static void AppendCurrentSettings(StringBuilder messageBuilder, UserProfile userProfile)
        {
            messageBuilder.AppendLine("Current Defaults:");
            messageBuilder.AppendLine($"  Working Days: {string.Join(", ", userProfile.WorkDays.OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d))}");
            messageBuilder.AppendLine($"  Working Hours: {userProfile.WorkStartTime} - {userProfile.WorkEndTime}");
            messageBuilder.AppendLine($"  Default List Sort: {userProfile.DefaultListSortOption}");
            messageBuilder.AppendLine($"  Default Scheduling Mode: {userProfile.SchedulingMode}");
        }
    }
}
