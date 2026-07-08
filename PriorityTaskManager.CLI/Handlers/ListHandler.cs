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
                        InteractiveSwitch(service);
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
                InteractiveListSettings(service);
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

        private void InteractiveListSettings(TaskManagerService service)
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
            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
            Console.WriteLine($"Editing List Settings: {workingList.Name}");
            Console.WriteLine("---------------------------------------------");
            int selectorTop = Console.CursorTop;

            var menuItems = BuildListSettingsMenuItems(workingList, service);
            ConsoleMenuHelper.DrawMenuItems(menuItems, selectedIndex, selectorTop);

            while (true)
            {
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
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + menuItems.Count) % menuItems.Count;
                        ConsoleMenuHelper.UpdateMenuSelection(menuItems, previousUp, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % menuItems.Count;
                        ConsoleMenuHelper.UpdateMenuSelection(menuItems, previousDown, selectedIndex, selectorTop);
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
                            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                            Console.CursorVisible = true;
                            Console.WriteLine("List settings update cancelled.");
                            return;
                        }
                        else
                        {
                            Console.CursorVisible = true;
                            EditListSettingField(service, workingList, selectedIndex, selectorTop);
                            Console.CursorVisible = false;

                            ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                            Console.WriteLine($"Editing List Settings: {workingList.Name}");
                            Console.WriteLine("---------------------------------------------");
                            selectorTop = Console.CursorTop;
                            menuItems = BuildListSettingsMenuItems(workingList, service);
                            ConsoleMenuHelper.DrawMenuItems(menuItems, selectedIndex, selectorTop);
                        }
                        break;
                    case ConsoleKey.Escape:
                        ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                        Console.CursorVisible = true;
                        Console.WriteLine("List settings update cancelled.");
                        return;
                }
            }
        }

        private void EditListSettingField(TaskManagerService service, TaskList workingList, int index, int selectorTop)
        {
            switch (index)
            {
                case 0:
                    if (ConsoleMenuHelper.TryPromptInlineInput(selectorTop + index, "> Name: ", workingList.Name, out var name) &&
                        !string.IsNullOrWhiteSpace(name))
                    {
                        workingList.Name = name.Trim();
                    }
                    break;
                case 1:
                    if (ConsoleMenuHelper.TryPromptInlineInput(selectorTop + index, "> Description: ", workingList.Description ?? string.Empty, out var description))
                    {
                        workingList.Description = description;
                    }
                    break;
                case 2:
                    workingList.SortOption = GetNextSortOption(workingList.SortOption ?? service.GetUserProfile().DefaultListSortOption);
                    break;
                case 3:
                    workingList.SchedulingMode = GetNextSchedulingMode(workingList.SchedulingMode ?? service.GetUserProfile().SchedulingMode);
                    break;
                case 4:
                    workingList.WorkDays ??= new List<DayOfWeek>();
                    ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                    ConsoleMenuHelper.RunToggleSelectionMenu(
                        $"Select working days for '{workingList.Name}':",
                        "Space to toggle, Enter to save, Esc to cancel.",
                        workingList.WorkDays,
                        Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList(),
                        day => day.ToString());
                    break;
                case 5:
                    ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                    Console.WriteLine($"Set work hours for '{workingList.Name}':");
                    Console.WriteLine("Set Daily Work Start Time:");
                    InteractiveListWorkHours(service, workingList);
                    break;
                case 6:
                    ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                    ConsoleMenuHelper.RunAdjustableValueMenu(
                        $"Adjust urgency thresholds for '{workingList.Name}':",
                        "Use Left/Right to adjust values.",
                        BuildSlackMenuOptions(workingList, service.GetUserProfile()));
                    break;
                case 7:
                    ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);
                    InteractiveListTimeSelector(workingList);
                    break;
            }
        }

        private List<string> BuildListSettingsMenuItems(TaskList workingList, TaskManagerService service)
        {
            return new List<string>
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
        }

        private List<ConsoleMenuHelper.AdjustableMenuOption> BuildSlackMenuOptions(TaskList workingList, UserProfile defaultProfile)
        {
            return new List<ConsoleMenuHelper.AdjustableMenuOption>
            {
                new(() => $"Dire     (< {GetEffectiveThreshold(workingList.SlackThresholdDire, defaultProfile.SlackThresholdDire):F1} days) [Red]", () => GetEffectiveThreshold(workingList.SlackThresholdDire, defaultProfile.SlackThresholdDire), value => workingList.SlackThresholdDire = value),
                new(() => $"Pressing (< {GetEffectiveThreshold(workingList.SlackThresholdPressing, defaultProfile.SlackThresholdPressing):F1} days) [DarkYellow]", () => GetEffectiveThreshold(workingList.SlackThresholdPressing, defaultProfile.SlackThresholdPressing), value => workingList.SlackThresholdPressing = value),
                new(() => $"Focus    (< {GetEffectiveThreshold(workingList.SlackThresholdFocus, defaultProfile.SlackThresholdFocus):F1} days) [Yellow]", () => GetEffectiveThreshold(workingList.SlackThresholdFocus, defaultProfile.SlackThresholdFocus), value => workingList.SlackThresholdFocus = value),
                new(() => $"Safe     (< {GetEffectiveThreshold(workingList.SlackThresholdSafe, defaultProfile.SlackThresholdSafe):F1} days) [Green]", () => GetEffectiveThreshold(workingList.SlackThresholdSafe, defaultProfile.SlackThresholdSafe), value => workingList.SlackThresholdSafe = value)
            };
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
                Console.WriteLine("List settings saved.");
                return true;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey(true);
                return false;
            }
        }

        private void InteractiveListWorkHours(TaskManagerService service, TaskList workingList)
        {
            Console.CursorVisible = true;
            var today = DateTime.Today;
            var startDateTime = today.Add((workingList.WorkStartTime ?? service.GetUserProfile().WorkStartTime).ToTimeSpan());
            var newStart = ConsoleInputHelper.InteractiveTimeInput(startDateTime);

            Console.WriteLine("\nSet Daily Work End Time:");
            var endDateTime = today.Add((workingList.WorkEndTime ?? service.GetUserProfile().WorkEndTime).ToTimeSpan());
            var newEnd = ConsoleInputHelper.InteractiveTimeInput(endDateTime);

            workingList.WorkStartTime = TimeOnly.FromDateTime(newStart);
            workingList.WorkEndTime = TimeOnly.FromDateTime(newEnd);
            Console.CursorVisible = false;
        }

        private void InteractiveListTimeSelector(TaskList workingList)
        {
            Console.CursorVisible = false;
            var selectedIndex = workingList.SimulatedTime.HasValue ? 1 : 0;
            var options = new List<string>
            {
                "Real-time",
                workingList.SimulatedTime.HasValue
                    ? $"Custom ({workingList.SimulatedTime.Value:yyyy-MM-dd HH:mm})"
                    : "Custom"
            };

            Console.WriteLine("\nSet list simulated time:");
            Console.WriteLine("Use Arrows to choose, Enter to select, Esc to cancel.");
            var selectorTop = Console.CursorTop;
            ConsoleMenuHelper.DrawMenuItems(options, selectedIndex, selectorTop);


            while (true)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        var previousUp = selectedIndex;
                        selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
                        ConsoleMenuHelper.UpdateMenuSelection(options, previousUp, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        var previousDown = selectedIndex;
                        selectedIndex = (selectedIndex + 1) % options.Count;
                        ConsoleMenuHelper.UpdateMenuSelection(options, previousDown, selectedIndex, selectorTop);
                        break;
                    case ConsoleKey.Enter:
                        if (selectedIndex == 0)
                        {
                            workingList.SimulatedTime = null;
                            Console.CursorVisible = true;
                            return;
                        }

                        ConsoleHelper.ClearAndRenderDashboard(_scheduleSnapshotProvider, _taskMetricsService);

                        Console.CursorVisible = true;
                        var defaultSimulatedTime = workingList.SimulatedTime ?? _timeService.GetCurrentTime();
                        var simulatedTime = ConsoleInputHelper.GetDateTimeFromUser("Enter the list's simulated date and time", defaultSimulatedTime);
                        if (simulatedTime.HasValue)
                        {
                            workingList.SimulatedTime = simulatedTime;
                        }

                        return;
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        return;
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

        private void InteractiveSwitch(TaskManagerService service)
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
