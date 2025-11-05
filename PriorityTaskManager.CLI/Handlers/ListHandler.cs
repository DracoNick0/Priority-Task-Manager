using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
using PriorityTaskManager.Models;

namespace PriorityTaskManager.CLI.Handlers
{
    /// <summary>
    /// Handles the 'list' command, displaying all tasks sorted by urgency.
    /// </summary>
    public class ListHandler : ICommandHandler
    {
        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (Program.ActiveListId == 0)
            {
                var generalList = service.GetListByName("General");
                if (generalList != null)
                    Program.ActiveListId = generalList.Id;
                else
                    Program.ActiveListId = 1;
            }

            if (args.Length == 0 || args[0].Equals("view", StringComparison.OrdinalIgnoreCase))
            {
                HandleViewTasksInActiveList(service);
            }
            else if (args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                HandleViewAllLists(service);
            }
            else if (args[0].Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                HandleCreateList(service, args.Skip(1).ToArray());
            }
            else if (args[0].Equals("switch", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length > 1)
                {
                    HandleSwitchList(service, args.Skip(1).ToArray());
                }
                else
                {
                    HandleInteractiveSwitch(service);
                }
            }
            else if (args[0].Equals("delete", StringComparison.OrdinalIgnoreCase))
            {
                HandleDeleteList(service, args.Skip(1).ToArray());
            }
            else if (args[0].Equals("sort", StringComparison.OrdinalIgnoreCase))
            {
                HandleSetSortOption(service, args.Skip(1).ToArray());
            }
            else
            {
                Console.WriteLine("Usage: list [view|all|create|switch|delete|sort <option>]");
            }
        }

        private void HandleViewTasksInActiveList(TaskManagerService service)
        {
            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == Program.ActiveListId);
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{Program.ActiveListId}' does not exist.");
                return;
            }
            var result = service.GetPrioritizedTasks(Program.ActiveListId);
            var tasksToDisplay = result.Tasks;
            var userProfile = service.UserProfile;
            if (userProfile.ActiveUrgencyMode == UrgencyMode.MultiAgent)
            {
                // Show agent logs ("thinking" effect)
                Console.WriteLine("\nRunning multi-agent scheduler...");
                foreach (var message in result.History)
                {
                    Console.WriteLine(message);
                    System.Threading.Thread.Sleep(500);
                }
                Console.WriteLine("Scheduling complete.\n");

                // Use new helper to determine which day to analyze
                var targetDay = FindTargetDayForSlackMeter(DateTime.Now, userProfile);
                var workStart = targetDay.Date.Add(userProfile.WorkStartTime.ToTimeSpan());
                var workEnd = targetDay.Date.Add(userProfile.WorkEndTime.ToTimeSpan());
                var totalWorkTime = (workEnd - workStart).TotalHours;
                var tasksForTargetDay = tasksToDisplay
                    .Where(t => t.ScheduledStartTime.HasValue && t.ScheduledEndTime.HasValue && t.ScheduledStartTime.Value.Date == targetDay.Date)
                    .ToList();

                if (!tasksForTargetDay.Any())
                {
                    Console.WriteLine($"\nNo tasks scheduled for {targetDay:dddd, MMM dd}.");
                }
                else
                {
                    Console.WriteLine($"\nTasks scheduled for {targetDay:dddd, MMM dd}:");
                    foreach (var t in tasksForTargetDay.OrderBy(t => t.ScheduledStartTime))
                    {
                        var start = t.ScheduledStartTime.HasValue ? t.ScheduledStartTime.Value.ToString("HH:mm") : "--:--";
                        var end = t.ScheduledEndTime.HasValue ? t.ScheduledEndTime.Value.ToString("HH:mm") : "--:--";
                        Console.WriteLine($"- [ID: {t.DisplayId}] {start} - {end} ({t.EstimatedDuration.TotalHours:F1}h) {t.Title}");
                    }
                    Console.WriteLine();
                }

                var scheduledTimeTargetDay = tasksForTargetDay.Sum(t => t.EstimatedDuration.TotalHours);
                var slackTime = totalWorkTime - scheduledTimeTargetDay;
                double slackRatio = scheduledTimeTargetDay > 0 ? slackTime / scheduledTimeTargetDay : 1.0;

                // Meter color
                if (slackRatio > 0.5)
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (slackRatio > 0.25)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (slackRatio >= 0)
                    Console.ForegroundColor = ConsoleColor.Red;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                // Meter bar
                int meterWidth = 30;
                double busyFraction = scheduledTimeTargetDay / (totalWorkTime > 0 ? totalWorkTime : 1);
                busyFraction = Math.Clamp(busyFraction, 0, 1);
                int busyBlocks = (int)(meterWidth * busyFraction);
                int slackBlocks = meterWidth - busyBlocks;
                string meter = new string('=', busyBlocks) + new string('-', slackBlocks);

                string headerText;
                if (targetDay.Date == DateTime.Today.Date)
                {
                    headerText = "Today's Schedule Pressure:";
                }
                else
                {
                    headerText = $"{targetDay:dddd}'s Schedule Pressure:";
                }
                Console.WriteLine($"{headerText} [{meter}] - {slackTime:F1} hours free");
                Console.ResetColor();
            }

            if (!tasksToDisplay.Any())
            {
                Console.WriteLine("No tasks found in this list.");
                return;
            }

            var mode = service.UserProfile.ActiveUrgencyMode;
            foreach (var task in tasksToDisplay)
            {
                if (mode == UrgencyMode.MultiAgent)
                {
                    if (task.ScheduledStartTime.HasValue)
                    {
                        Console.WriteLine($"[ID: {task.DisplayId}] {task.Title} (Recommended Start: {task.ScheduledStartTime:ddd, MMM dd HH:mm})");
                    }
                    else
                    {
                        Console.WriteLine($"[ID: {task.DisplayId}] {task.Title} (Unscheduled)");
                    }
                }
                else // SingleAgent
                {
                    Console.WriteLine($"[ID: {task.DisplayId}] {task.Title} (Urgency: {task.UrgencyScore:F2})");
                }
            }
        }

        private void HandleViewAllLists(TaskManagerService service)
        {
            var lists = service.GetAllLists();
            foreach (var list in lists)
            {
                var activeIndicator = list.Id == Program.ActiveListId ? " (Active)" : string.Empty;
                Console.WriteLine($"- {list.Name}{activeIndicator}");
            }
        }

        private void HandleCreateList(TaskManagerService service, string[] args)
        {
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
            var listName = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(listName))
            {
                Console.WriteLine("Error: List name cannot be empty.");
                return;
            }

            var list = service.GetListByName(listName);
            if (list != null)
            {
                Program.ActiveListId = list.Id;
                Console.WriteLine($"Switched to list '{listName}'.");
            }
            else
            {
                Console.WriteLine($"Error: List '{listName}' does not exist.");
            }
        }

        private void HandleDeleteList(TaskManagerService service, string[] args)
        {
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
            if (listToDelete != null && listToDelete.Id == Program.ActiveListId)
            {
                // If deleting the active list, switch to General
                var generalList = service.GetListByName("General");
                Program.ActiveListId = generalList != null ? generalList.Id : 1;
            }
            service.DeleteList(listName);
            Console.WriteLine($"List '{listName}' deleted successfully.");
        }

        private void HandleSetSortOption(TaskManagerService service, string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Error: Missing sort option. Valid options are: Default, Alphabetical, DueDate, Id.");
                return;
            }

            if (!Enum.TryParse<SortOption>(args[0], true, out var sortOption))
            {
                Console.WriteLine("Error: Invalid sort option. Valid options are: Default, Alphabetical, DueDate, Id.");
                return;
            }

            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == Program.ActiveListId);
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{Program.ActiveListId}' does not exist.");
                return;
            }
            activeList.SortOption = sortOption;
            service.UpdateList(activeList);
            Console.WriteLine($"Sort option for list '{activeList.Name}' updated to {sortOption}.");
        }

        private void HandleInteractiveSwitch(TaskManagerService service)
        {
            var lists = service.GetAllLists().ToList();
            if (!lists.Any())
            {
                Console.WriteLine("No lists available to switch.");
                return;
            }

            int selectedIndex = lists.FindIndex(l => l.Id == Program.ActiveListId);
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
                        Program.ActiveListId = lists[selectedIndex].Id;
                        Console.SetCursorPosition(0, initialTop + lists.Count);
                        Console.WriteLine($"Switched to list '{lists[selectedIndex].Name}'.");
                        return;
                    case ConsoleKey.Escape:
                        Console.SetCursorPosition(0, initialTop + lists.Count);
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

        /// <summary>
        /// Determines the target day for the slack meter (public for testing; make private after TDD).
        /// </summary>
        public DateTime FindTargetDayForSlackMeter(DateTime currentTime, UserProfile profile)
        {
            // Get end of workday for the current day
            var endOfWorkday = currentTime.Date.Add(profile.WorkEndTime.ToTimeSpan());
            var isWorkday = profile.WorkDays.Contains(currentTime.DayOfWeek);
            if (isWorkday && currentTime <= endOfWorkday)
            {
                // During work hours on a workday
                return currentTime.Date;
            }
            // Otherwise, find the next workday
            var checkDate = currentTime.Date.AddDays(1);
            for (int i = 0; i < 14; i++) // Safety: max 2 weeks
            {
                if (profile.WorkDays.Contains(checkDate.DayOfWeek))
                {
                    return checkDate;
                }
                checkDate = checkDate.AddDays(1);
            }
            // Fallback: just return today
            return currentTime.Date;
        }
    }
}
