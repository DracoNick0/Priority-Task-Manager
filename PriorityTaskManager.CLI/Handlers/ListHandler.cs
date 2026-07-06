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
            // Ensure an active list is set. If not, default to the "General" list or the first available one.
            if (service.GetActiveListId() == 0)
            {
                var generalList = service.GetListByName("General");
                service.SetActiveListId(generalList?.Id ?? 1);
            }

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
                default:
                    Console.WriteLine("Usage: list [view|all|create|switch|delete|sort <option>]");
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

            if (listName.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: The 'General' list cannot be deleted.");
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

            var listToDelete = service.GetListByName(listName);
            if (listToDelete != null && listToDelete.Id == service.GetActiveListId())
            {
                var generalList = service.GetListByName("General");
                service.SetActiveListId(generalList != null ? generalList.Id : 1);
            }
            service.DeleteList(listName);
            Console.WriteLine($"List '{listName}' deleted successfully.");
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

            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }
            activeList.SortOption = targetSortOption.Value;
            service.UpdateList(activeList);
            Console.WriteLine($"Sort option for list '{activeList.Name}' updated to {targetSortOption.Value}.");
        }

        private void HandleInteractiveSwitch(TaskManagerService service)
        {
            _scheduleSnapshotProvider.RefreshActiveListSnapshot(out _);
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
                        Console.WriteLine($"Switched to list '{lists[selectedIndex].Name}'.");
                        return;
                    case ConsoleKey.Escape:
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
