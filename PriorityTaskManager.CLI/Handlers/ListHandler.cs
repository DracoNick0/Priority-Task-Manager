using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.CLI.Utils;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'list' command and its sub-commands.
    /// Manages task lists, including viewing, creating, switching, deleting, and sorting.
    /// </summary>
    public class ListHandler : ICommandHandler
    {
        private readonly ITaskMetricsService _taskMetricsService;
        private readonly ITimeService _timeService;
        private readonly ScheduleSnapshotProvider _scheduleSnapshotProvider;

        public ListHandler(
            ITaskMetricsService taskMetricsService,
            ITimeService timeService,
            ScheduleSnapshotProvider scheduleSnapshotProvider)
        {
            _taskMetricsService = taskMetricsService;
            _timeService = timeService;
            _scheduleSnapshotProvider = scheduleSnapshotProvider;
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            // Command routing
            var command = args.Length > 0 ? args[0].ToLower() : "view";
            var commandArgs = args.Skip(1).ToArray();

            switch (command)
            {
                case "view":
                    HandleViewTasksInActiveList(service);
                    break;
                case "all":
                    HandleViewAllLists(service);
                    break;
                case "create":
                    HandleCreateList(service, commandArgs);
                    break;
                case "switch":
                    if (commandArgs.Length > 0)
                        HandleSwitchList(service, commandArgs);
                    else
                        HandleInteractiveSwitch(service);
                    break;
                case "delete":
                    HandleDeleteList(service, commandArgs);
                    break;
                case "sort":
                    HandleSetSortOption(service, commandArgs);
                    break;
                case "settings":
                    HandleListSettings(service, commandArgs);
                    break;
                default:
                    Console.WriteLine("Usage: list [view|all|create|switch|delete|sort <option>|settings]");
                    break;
            }
        }

        /// <summary>
        /// Finds the incomplete task that is closest to its due date among a collection of tasks.
        /// This is used to identify the most immediate deadline.
        /// </summary>
        /// <param name="tasks">A collection of tasks to search through.</param>
        /// <returns>The <see cref="TaskItem"/> closest to its due date, or null if no suitable task is found.</returns>
        public TaskItem? FindClosestTaskToDueDate(IEnumerable<TaskItem> tasks)
        {
            return tasks
                .Where(t => t.DueDate.HasValue && t.ScheduledParts.Any() && !t.IsCompleted)
                .OrderBy(t => (t.DueDate!.Value - t.ScheduledParts.Min(p => p.StartTime)).Duration())
                .FirstOrDefault();
        }

        private DateTime GetEffectiveDueTime(TaskItem task, UserProfile userProfile)
        {
            if (task.DueDate.HasValue)
            {
                var dueDate = task.DueDate.Value;
                var workEnd = dueDate.Date.Add(userProfile.WorkEndTime.ToTimeSpan());
                return dueDate < workEnd ? dueDate : workEnd;
            }
            return DateTime.MaxValue;
        }

        private void HandleViewTasksInActiveList(TaskManagerService service)
        {
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);

            if (!_scheduleSnapshotProvider.RefreshActiveListSnapshot(out var refreshError))
            {
                Console.WriteLine(refreshError);
                return;
            }
        }

        // Helper to format dates as MM-dd or MM-dd-yyyy if not current year
        private string FormatDate(DateTime? date)
        {
            if (!date.HasValue)
            {
                return "No date";
            }
            var now = _timeService.GetCurrentTime();
            if (date.Value.Year == now.Year)
                return date.Value.ToString("MM-dd");
            else
                return date.Value.ToString("MM-dd-yyyy");
        }

        private void HandleViewAllLists(TaskManagerService service)
        {
            var lists = service.GetAllLists();
            foreach (var list in lists)
            {
                var activeIndicator = list.Id == service.GetActiveListId() ? " (Active)" : string.Empty;
                Console.WriteLine($"- {list.Name}{activeIndicator}");
            }
        }

        private void HandleCreateList(TaskManagerService service, string[] args)
        {
            _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);

            var listName = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(listName))
            {
                Console.WriteLine("Error: List name cannot be empty.");
                return;
            }

            try
            {
                service.AddList(new TaskList { Name = listName });
                Console.WriteLine($"List '{listName}' created successfully.");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void HandleSwitchList(TaskManagerService service, string[] args)
        {
            _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);

            var listName = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(listName))
            {
                Console.WriteLine("Error: List name cannot be empty.");
                return;
            }

            var list = service.GetListByName(listName);
            if (list != null)
            {
                service.SetActiveListId(list.Id);
                service.ApplyListTimePreference(list.Id, _timeService);
                Console.WriteLine($"Switched to list '{listName}'.");
            }
            else
            {
                Console.WriteLine($"Error: List '{listName}' does not exist.");
            }
        }

        private void HandleDeleteList(TaskManagerService service, string[] args)
        {
            _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);

            var listName = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(listName))
            {
                Console.WriteLine("Error: List name cannot be empty.");
                return;
            }

            Console.Write("Are you sure you want to delete this list and all its tasks? (y/n): ");
            string? confirmation = null;

            while (string.IsNullOrWhiteSpace(confirmation))
            {
                confirmation = Console.ReadLine();
            }

            if (!confirmation.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Deletion cancelled.");
                return;
            }

            bool wasLastList = service.DeleteList(listName);
            _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
            if (wasLastList)
            {
                Console.WriteLine($"List '{listName}' was the last list. A new 'General' list has been automatically created.");
            }
            else
            {
                Console.WriteLine($"List '{listName}' deleted successfully.");
            }
        }

        private void HandleSetSortOption(TaskManagerService service, string[] args)
        {
            _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
            
            if (args.Length < 1)
            {
                Console.WriteLine("Error: Missing sort option. Valid options are: Default, Alphabetical, DueDate, Id.");
                return;
            }

            // Map user-friendly strings to enum
            var sortStr = args[0].ToLowerInvariant();
            var targetSortOption = sortStr switch
            {
                "alpha" or "alphabetical" => SortOption.Alphabetical,
                "due" or "duedate" => SortOption.DueDate,
                "id" => SortOption.Id,
                "default" => SortOption.Default,
                _ => default(SortOption?)
            };

            if (targetSortOption == null)
            {
                Console.WriteLine("Error: Invalid sort option. Valid options are: Default, Alphabetical, DueDate, Id.");
                return;
            }

            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }
            activeList.SortOption = targetSortOption == SortOption.Default
                ? service.GetUserProfile().DefaultListSortOption
                : targetSortOption.Value;
            service.UpdateList(activeList);
            Console.WriteLine($"Sort option for list '{activeList.Name}' updated to {activeList.SortOption}.");
        }

        private void HandleListSettings(TaskManagerService service, string[] args)
        {
            if (args.Length == 0)
            {
                RunInteractiveListSettings(service);
                return;
            }

            var subCommand = args[0].ToLowerInvariant();
            var subArgs = args.Skip(1).ToArray();

            switch (subCommand)
            {
                case "show":
                    HandleShowListSettings(service);
                    break;
                case "name":
                    HandleSetName(service, subArgs);
                    break;
                case "description":
                case "desc":
                    HandleSetDescription(service, subArgs);
                    break;
                case "sort":
                    HandleSetSortOption(service, subArgs);
                    break;
                case "strategy":
                    HandleSetStrategy(service, subArgs);
                    break;
                case "hours":
                    HandleSetWorkHours(service, subArgs);
                    break;
                case "days":
                    HandleSetWorkDays(service, subArgs);
                    break;
                case "slack":
                    HandleSetSlackThresholds(service, subArgs);
                    break;
                case "time":
                    HandleSetSimulatedTime(service, subArgs);
                    break;
                default:
                    Console.WriteLine("Usage: list settings [show|sort|strategy|hours|days|slack|time|desc]");
                    break;
            }
        }

        private void RunInteractiveListSettings(TaskManagerService service)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            var workingList = CloneList(activeList);
            int selectedIndex = 0;

            Console.CursorVisible = false;
            while (true)
            {
                ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                Console.WriteLine($"Editing List Settings: {workingList.Name}");
                Console.WriteLine("---------------------------------------------");

                var menuItems = new List<string>
                {
                    $"Name: {workingList.Name}",
                    $"Description: {(string.IsNullOrWhiteSpace(workingList.Description) ? "(none)" : workingList.Description)}",
                    $"Sort Option: {workingList.SortOption ?? service.GetUserProfile().DefaultListSortOption}",
                    $"Scheduling Mode: {workingList.SchedulingMode ?? service.GetUserProfile().SchedulingMode}",
                    $"Work Days: {FormatWorkDays(workingList.WorkDays ?? new List<DayOfWeek>(service.GetUserProfile().WorkDays))}",
                    $"Work Hours: {FormatWorkHours(workingList.WorkStartTime ?? service.GetUserProfile().WorkStartTime, workingList.WorkEndTime ?? service.GetUserProfile().WorkEndTime)}",
                    $"Slack Thresholds: Dire {GetEffectiveThreshold(workingList.SlackThresholdDire, service.GetUserProfile().SlackThresholdDire):F1}, Pressing {GetEffectiveThreshold(workingList.SlackThresholdPressing, service.GetUserProfile().SlackThresholdPressing):F1}, Focus {GetEffectiveThreshold(workingList.SlackThresholdFocus, service.GetUserProfile().SlackThresholdFocus):F1}, Safe {GetEffectiveThreshold(workingList.SlackThresholdSafe, service.GetUserProfile().SlackThresholdSafe):F1}",
                    workingList.SimulatedTime.HasValue ? $"Simulated Time: {workingList.SimulatedTime.Value:yyyy-MM-dd HH:mm}" : "Simulated Time: real-time",
                    "Save & Exit",
                    "Cancel"
                };

                ConsoleHelper.DrawMenu(menuItems, selectedIndex);

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    if (TrySaveListSettings(service, workingList))
                    {
                        Console.CursorVisible = true;
                        return;
                    }
                    continue;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + menuItems.Count) % menuItems.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % menuItems.Count;
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == menuItems.Count - 2)
                        {
                            if (TrySaveListSettings(service, workingList))
                            {
                                Console.CursorVisible = true;
                                return;
                            }
                        }
                        else if (selectedIndex == menuItems.Count - 1)
                        {
                            Console.CursorVisible = true;
                            Console.WriteLine("List settings update cancelled.");
                            return;
                        }
                        else
                        {
                            Console.CursorVisible = true;
                            Console.WriteLine();
                            EditListSettingField(service, workingList, selectedIndex);
                            Console.CursorVisible = false;
                        }
                        break;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        Console.WriteLine("List settings update cancelled.");
                        return;
                }
            }
        }

        private void EditListSettingField(TaskManagerService service, TaskList workingList, int index)
        {
            switch (index)
            {
                case 0:
                    Console.Write($"New Name (current: {workingList.Name}): ");
                    var name = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        workingList.Name = name.Trim();
                    }
                    break;
                case 1:
                    Console.Write($"New Description (current: {workingList.Description ?? string.Empty}): ");
                    workingList.Description = Console.ReadLine() ?? workingList.Description;
                    break;
                case 2:
                    Console.Write("Sort option (default, alphabetical, due, id): ");
                    var sortInput = Console.ReadLine() ?? string.Empty;
                    if (TryParseSortOption(sortInput, out var sortOption))
                    {
                        workingList.SortOption = sortOption == SortOption.Default
                            ? service.GetUserProfile().DefaultListSortOption
                            : sortOption;
                    }
                    else
                    {
                        Console.WriteLine("Invalid sort option.");
                        Console.ReadKey(true);
                    }
                    break;
                case 3:
                    Console.Write("Scheduling mode (gold/solver): ");
                    var modeInput = Console.ReadLine() ?? string.Empty;
                    if (TryParseSchedulingMode(modeInput, out var schedulingMode))
                    {
                        workingList.SchedulingMode = schedulingMode;
                    }
                    else
                    {
                        Console.WriteLine("Invalid scheduling mode.");
                        Console.ReadKey(true);
                    }
                    break;
                case 4:
                    RunInteractiveListDaySelector(service, workingList);
                    break;
                case 5:
                    RunInteractiveListWorkHours(service, workingList);
                    break;
                case 6:
                    RunInteractiveListSlackSelector(service, workingList);
                    break;
                case 7:
                    RunInteractiveListTimeSelector(workingList);
                    break;
            }
        }

        private bool TrySaveListSettings(TaskManagerService service, TaskList workingList)
        {
            if (string.IsNullOrWhiteSpace(workingList.Name))
            {
                Console.WriteLine("Error: List name cannot be empty.");
                Console.ReadKey(true);
                return false;
            }

            try
            {
                service.UpdateList(workingList);
                service.ApplyListTimePreference(workingList.Id, _timeService);
                _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
                ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                Console.WriteLine($"List '{workingList.Name}' updated successfully.");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey(true);
                return false;
            }
        }

        private void RunInteractiveListDaySelector(TaskManagerService service, TaskList workingList)
        {
            var allDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList();
            int selectedDayIndex = 0;
            var originalWorkDays = workingList.WorkDays == null
                ? new List<DayOfWeek>()
                : new List<DayOfWeek>(workingList.WorkDays);

            workingList.WorkDays ??= new List<DayOfWeek>();

            Console.CursorVisible = false;
            while (true)
            {
                ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                Console.WriteLine($"Select working days for '{workingList.Name}' (Space to toggle, Enter to save, Esc to cancel):");
                DrawDaySelector(allDays, workingList.WorkDays, selectedDayIndex);

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
                        if (workingList.WorkDays.Contains(dayToToggle))
                        {
                            workingList.WorkDays.Remove(dayToToggle);
                        }
                        else
                        {
                            workingList.WorkDays.Add(dayToToggle);
                        }
                        break;
                    case ConsoleKey.Enter:
                        return;
                    case ConsoleKey.Escape:
                        workingList.WorkDays = originalWorkDays;
                        return;
                }
            }
        }

        private void RunInteractiveListWorkHours(TaskManagerService service, TaskList workingList)
        {
            Console.CursorVisible = true;
            var today = DateTime.Today;
            var startDateTime = today.Add((workingList.WorkStartTime ?? service.GetUserProfile().WorkStartTime).ToTimeSpan());
            var newStart = ConsoleInputHelper.HandleInteractiveTimeInput(startDateTime);

            Console.WriteLine("\nSet Daily Work End Time:");
            var endDateTime = today.Add((workingList.WorkEndTime ?? service.GetUserProfile().WorkEndTime).ToTimeSpan());
            var newEnd = ConsoleInputHelper.HandleInteractiveTimeInput(endDateTime);

            workingList.WorkStartTime = TimeOnly.FromDateTime(newStart);
            workingList.WorkEndTime = TimeOnly.FromDateTime(newEnd);
            Console.CursorVisible = false;
        }

        private void RunInteractiveListSlackSelector(TaskManagerService service, TaskList workingList)
        {
            int selectedIndex = 0;
            const double increment = 0.5;

            while (true)
            {
                ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                Console.WriteLine($"Adjust urgency thresholds for '{workingList.Name}':");
                Console.WriteLine("Use Left/Right to adjust values, Enter to save.");

                var defaultProfile = service.GetUserProfile();
                var items = new List<string>
                {
                    $"Dire     (< {GetEffectiveThreshold(workingList.SlackThresholdDire, defaultProfile.SlackThresholdDire):F1} days) [Red]",
                    $"Pressing (< {GetEffectiveThreshold(workingList.SlackThresholdPressing, defaultProfile.SlackThresholdPressing):F1} days) [DarkYellow]",
                    $"Focus    (< {GetEffectiveThreshold(workingList.SlackThresholdFocus, defaultProfile.SlackThresholdFocus):F1} days) [Yellow]",
                    $"Safe     (< {GetEffectiveThreshold(workingList.SlackThresholdSafe, defaultProfile.SlackThresholdSafe):F1} days) [Green]",
                    "Back to List Settings"
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
                    return;
                }
                else if (selectedIndex < 4)
                {
                    double currentVal = selectedIndex switch
                    {
                        0 => GetEffectiveThreshold(workingList.SlackThresholdDire, defaultProfile.SlackThresholdDire),
                        1 => GetEffectiveThreshold(workingList.SlackThresholdPressing, defaultProfile.SlackThresholdPressing),
                        2 => GetEffectiveThreshold(workingList.SlackThresholdFocus, defaultProfile.SlackThresholdFocus),
                        3 => GetEffectiveThreshold(workingList.SlackThresholdSafe, defaultProfile.SlackThresholdSafe),
                        _ => 0
                    };

                    if (key.Key == ConsoleKey.LeftArrow)
                    {
                        currentVal = Math.Max(0.1, currentVal - increment);
                    }
                    else if (key.Key == ConsoleKey.RightArrow)
                    {
                        currentVal += increment;
                    }

                    switch (selectedIndex)
                    {
                        case 0:
                            workingList.SlackThresholdDire = currentVal;
                            break;
                        case 1:
                            workingList.SlackThresholdPressing = currentVal;
                            break;
                        case 2:
                            workingList.SlackThresholdFocus = currentVal;
                            break;
                        case 3:
                            workingList.SlackThresholdSafe = currentVal;
                            break;
                    }
                }
            }
        }

        private void RunInteractiveListTimeSelector(TaskList workingList)
        {
            Console.CursorVisible = true;
            Console.WriteLine("\nSet list simulated time: [1] Real-time [2] Custom");
            var key = Console.ReadKey(true);
            if (key.KeyChar == '1')
            {
                workingList.SimulatedTime = null;
            }
            else if (key.KeyChar == '2')
            {
                var simulatedTime = ConsoleInputHelper.GetDateTimeFromUser("Enter the list's simulated date and time");
                if (simulatedTime.HasValue)
                {
                    workingList.SimulatedTime = simulatedTime;
                }
            }
        }

        private TaskList CloneList(TaskList list)
        {
            return new TaskList
            {
                Id = list.Id,
                Name = list.Name,
                Description = list.Description,
                SortOption = list.SortOption,
                SchedulingMode = list.SchedulingMode,
                WorkStartTime = list.WorkStartTime,
                WorkEndTime = list.WorkEndTime,
                WorkDays = list.WorkDays == null ? null : new List<DayOfWeek>(list.WorkDays),
                SlackThresholdDire = list.SlackThresholdDire,
                SlackThresholdPressing = list.SlackThresholdPressing,
                SlackThresholdFocus = list.SlackThresholdFocus,
                SlackThresholdSafe = list.SlackThresholdSafe,
                SimulatedTime = list.SimulatedTime
            };
        }

        private static string FormatWorkDays(List<DayOfWeek> days)
        {
            return string.Join(", ", days.OrderBy(d => d == DayOfWeek.Sunday ? 7 : (int)d));
        }

        private static string FormatWorkHours(TimeOnly start, TimeOnly end)
        {
            return $"{start:HH:mm}-{end:HH:mm}";
        }

        private static double GetEffectiveThreshold(double? listThreshold, double globalThreshold)
        {
            return listThreshold ?? globalThreshold;
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

        private void HandleSetName(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            var newName = string.Join(" ", args).Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                Console.WriteLine("Error: List name cannot be empty.");
                return;
            }

            activeList.Name = newName;
            service.UpdateList(activeList);
            Console.WriteLine($"List renamed to '{newName}'.");
        }

        private void HandleShowListSettings(TaskManagerService service)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            var profile = service.BuildEffectiveUserProfile(activeList);
            Console.WriteLine($"List: {activeList.Name}");
            Console.WriteLine($"Description: {activeList.Description ?? string.Empty}");
            Console.WriteLine($"Sort Option: {activeList.SortOption ?? service.GetUserProfile().DefaultListSortOption}");
            Console.WriteLine($"Scheduling Mode: {activeList.SchedulingMode ?? service.GetUserProfile().SchedulingMode}");
            Console.WriteLine($"Work Days: {string.Join(", ", profile.WorkDays)}");
            Console.WriteLine($"Work Hours: {profile.WorkStartTime:HH\\:mm}-{profile.WorkEndTime:HH\\:mm}");
            Console.WriteLine($"Slack Thresholds: Dire {profile.SlackThresholdDire}, Pressing {profile.SlackThresholdPressing}, Focus {profile.SlackThresholdFocus}, Safe {profile.SlackThresholdSafe}");
            Console.WriteLine(activeList.SimulatedTime.HasValue
                ? $"Simulated Time: {activeList.SimulatedTime.Value:yyyy-MM-dd HH:mm}"
                : "Simulated Time: real-time");
        }

        private void HandleSetStrategy(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            if (args.Length < 1)
            {
                Console.WriteLine("Error: Missing strategy. Valid options are: gold, solver.");
                return;
            }

            var strategy = args[0].ToLowerInvariant() switch
            {
                "gold" or "goldpanning" => SchedulingMode.GoldPanning,
                "solver" or "constraint" or "constraintoptimization" => SchedulingMode.ConstraintOptimization,
                _ => (SchedulingMode?)null
            };

            if (strategy == null)
            {
                Console.WriteLine("Error: Invalid strategy. Valid options are: gold, solver.");
                return;
            }

            activeList.SchedulingMode = strategy.Value;
            service.UpdateList(activeList);
            Console.WriteLine($"Scheduling mode for list '{activeList.Name}' updated to {activeList.SchedulingMode}.");
        }

        private void HandleSetWorkHours(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            if (args.Length < 1 || !TryParseTimeRange(args[0], out var start, out var end))
            {
                Console.WriteLine("Error: Provide hours as HH:mm-HH:mm.");
                return;
            }

            activeList.WorkStartTime = start;
            activeList.WorkEndTime = end;
            service.UpdateList(activeList);
            Console.WriteLine($"Work hours for list '{activeList.Name}' updated to {start:HH\\:mm}-{end:HH\\:mm}.");
        }

        private void HandleSetWorkDays(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            if (args.Length < 1)
            {
                Console.WriteLine("Error: Provide at least one day separated by commas.");
                return;
            }

            var parsedDays = new List<DayOfWeek>();
            foreach (var token in string.Join(" ", args).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse(token.Trim(), true, out DayOfWeek dayOfWeek))
                {
                    parsedDays.Add(dayOfWeek);
                }
            }

            if (!parsedDays.Any())
            {
                Console.WriteLine("Error: No valid days were provided.");
                return;
            }

            activeList.WorkDays = parsedDays;
            service.UpdateList(activeList);
            Console.WriteLine($"Work days for list '{activeList.Name}' updated.");
        }

        private void HandleSetSlackThresholds(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            if (args.Length < 4 ||
                !double.TryParse(args[0], out var dire) ||
                !double.TryParse(args[1], out var pressing) ||
                !double.TryParse(args[2], out var focus) ||
                !double.TryParse(args[3], out var safe))
            {
                Console.WriteLine("Error: Provide four numbers: dire pressing focus safe.");
                return;
            }

            activeList.SlackThresholdDire = dire;
            activeList.SlackThresholdPressing = pressing;
            activeList.SlackThresholdFocus = focus;
            activeList.SlackThresholdSafe = safe;
            service.UpdateList(activeList);
            Console.WriteLine($"Slack thresholds for list '{activeList.Name}' updated.");
        }

        private void HandleSetSimulatedTime(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            if (args.Length == 0 || args[0].Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                activeList.SimulatedTime = null;
                service.UpdateList(activeList);
                _timeService.ClearSimulatedTime();
                Console.WriteLine($"List '{activeList.Name}' will now use real-time.");
                return;
            }

            if (DateTime.TryParse(string.Join(" ", args), out var simulatedTime))
            {
                activeList.SimulatedTime = simulatedTime;
                service.UpdateList(activeList);
                _timeService.SetSimulatedTime(simulatedTime);
                Console.WriteLine($"List '{activeList.Name}' simulated time set to {simulatedTime:yyyy-MM-dd HH:mm}.");
                return;
            }

            Console.WriteLine("Error: Provide a valid date/time or 'now'.");
        }

        private void HandleSetDescription(TaskManagerService service, string[] args)
        {
            var activeList = service.GetListById(service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }

            activeList.Description = string.Join(" ", args).Trim();
            service.UpdateList(activeList);
            Console.WriteLine($"Description for list '{activeList.Name}' updated.");
        }

        private bool TryParseTimeRange(string input, out TimeOnly start, out TimeOnly end)
        {
            start = default;
            end = default;

            var parts = input.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            return TimeOnly.TryParse(parts[0].Trim(), out start) && TimeOnly.TryParse(parts[1].Trim(), out end);
        }

        private void HandleInteractiveSwitch(TaskManagerService service)
        {
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);

            var lists = service.GetAllLists().ToList();
            if (!lists.Any())
            {
                Console.WriteLine("No lists available to switch.");
                return;
            }

            int selectedIndex = lists.FindIndex(l => l.Id == service.GetActiveListId());
            if (selectedIndex == -1) selectedIndex = 0;

            int initialTop = Console.CursorTop;
            int maxTop = Console.BufferHeight - lists.Count - 1;
            if (initialTop > maxTop)
            {
                initialTop = maxTop > 0 ? maxTop : 0;
            }

            while (true)
            {
                DrawListMenu(lists, selectedIndex, initialTop);

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % lists.Count;
                        break;
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + lists.Count) % lists.Count;
                        break;
                    case ConsoleKey.Enter:
                        service.SetActiveListId(lists[selectedIndex].Id);
                        _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
                        ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                        Console.WriteLine($"Switched to list '{lists[selectedIndex].Name}'.");
                        return;
                    case ConsoleKey.Escape:
                        ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                        Console.WriteLine("Switch cancelled.");
                        return;
                }
            }
        }

        private void DrawListMenu(List<TaskList> lists, int selectedIndex, int initialTop)
        {
            for (int i = 0; i < lists.Count; i++)
            {
                Console.SetCursorPosition(0, initialTop + i);
                var prefix = i == selectedIndex ? "> " : "  ";

                // Write and pad with spaces to clear leftovers
                string line = (prefix + lists[i].Name).PadRight(Console.WindowWidth - 1);
                Console.Write(line);
            }
        }
    }
}
