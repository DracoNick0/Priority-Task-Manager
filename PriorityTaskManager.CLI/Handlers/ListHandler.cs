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

        /// <summary>
        /// Finds the task closest to its due date across all tasks.
        /// </summary>
        public TaskItem? FindClosestTaskToDueDate(IEnumerable<TaskItem> tasks)
        {
            return tasks
                .Where(t => t.ScheduledStartTime.HasValue && !t.IsCompleted)
                .OrderBy(t => (t.DueDate - t.ScheduledStartTime.Value).Duration())
                .FirstOrDefault();
        }

        private DateTime GetEffectiveDueTime(TaskItem task, UserProfile userProfile)
        {
            var workEnd = task.DueDate.Date.Add(userProfile.WorkEndTime.ToTimeSpan());
            return task.DueDate < workEnd ? task.DueDate : workEnd;
        }

        private void HandleViewTasksInActiveList(TaskManagerService service)
        {
            Console.Clear();
            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == Program.ActiveListId);
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{Program.ActiveListId}' does not exist.");
                return;
            }
            var result = service.GetPrioritizedTasks(Program.ActiveListId);
            var tasksToDisplay = result.Tasks;
            var incompleteTasks = tasksToDisplay.Where(t => !t.IsCompleted).ToList();

            var userProfile = service.UserProfile;
            var now = DateTime.Now;
            var targetDay = FindTargetDayForSlackMeter(now, userProfile);
            var workStart = targetDay.Date.Add(userProfile.WorkStartTime.ToTimeSpan());
            var workEnd = targetDay.Date.Add(userProfile.WorkEndTime.ToTimeSpan());

            var closestTask = FindClosestTaskToDueDate(incompleteTasks);
            if (closestTask != null && closestTask.ScheduledStartTime.HasValue)
            {
                var effectiveDueTime = GetEffectiveDueTime(closestTask, userProfile);
                var slack = (effectiveDueTime - closestTask.ScheduledStartTime!.Value) - closestTask.EstimatedDuration;
                var slackPercentage = slack.TotalMinutes / closestTask.EstimatedDuration.TotalMinutes;

                // Calculate schedule pressure
                var totalWorkTime = (workEnd - workStart).TotalHours;
                var scheduledTime = incompleteTasks
                        .Where(t => t.ScheduledStartTime.HasValue && t.ScheduledStartTime.Value.Date == targetDay.Date)
                        .Sum(t => t.EstimatedDuration.TotalHours);

                // Adjust slackTime to reflect only hours free within the target day
                var slackTime = Math.Max(0, totalWorkTime - scheduledTime);

                // Meter bar with task numbers
                int meterWidth = 32;
                double totalWorkMinutes = (workEnd - workStart).TotalMinutes;
                var taskBlocks = new char[meterWidth];
                Array.Fill(taskBlocks, ' ');

                foreach (var task in incompleteTasks.Where(t => t.ScheduledStartTime.HasValue))
                {
                    var startMinutes = (task.ScheduledStartTime.Value - workStart).TotalMinutes;
                    var endMinutes = ((task.ScheduledEndTime ?? workEnd) - workStart).TotalMinutes;

                    if (startMinutes >= 0 && startMinutes < totalWorkMinutes)
                    {
                        int startBlock = (int)(meterWidth * (startMinutes / totalWorkMinutes));
                        int endBlock = (int)(meterWidth * (Math.Min(endMinutes, totalWorkMinutes) / totalWorkMinutes));

                        if (startBlock < meterWidth)
                        {
                            taskBlocks[startBlock] = '|';
                        }

                        int taskWidth = endBlock - startBlock - 1;
                        if (taskWidth > 0)
                        {
                            string taskRepresentation = new string('=', taskWidth);
                            int idPosition = taskWidth / 2;
                            taskRepresentation = taskRepresentation.Remove(idPosition, 1).Insert(idPosition, task.DisplayId.ToString());

                            for (int i = 0; i < taskWidth; i++)
                            {
                                if (startBlock + 1 + i < meterWidth)
                                {
                                    taskBlocks[startBlock + 1 + i] = taskRepresentation[i];
                                }
                            }
                        }

                        if (endBlock < meterWidth)
                        {
                            taskBlocks[endBlock] = '|';
                        }
                    }
                }

                string meter = new string(taskBlocks);

                // Determine color
                ConsoleColor meterColor;
                if (slack.TotalMinutes < 0)
                    meterColor = ConsoleColor.Black;
                else if (slackPercentage > 0.25)
                    meterColor = ConsoleColor.Green;
                else if (slackPercentage > 0.10)
                    meterColor = ConsoleColor.Yellow;
                else
                    meterColor = ConsoleColor.Red;

                // Display combined output
                Console.ForegroundColor = meterColor;
                string headerText = targetDay.Date == DateTime.Today.Date
                    ? "Today's Schedule:"
                    : $"{targetDay:dddd}'s Schedule:";
                Console.WriteLine($"{headerText} [{meter}] {slackTime:F1} hours free");
                Console.WriteLine($"Task with least slack: '{closestTask.Title}' - Slack: {slack.Hours} hours {slack.Minutes} minutes");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("No tasks available to calculate slack.");
            }

            // Critical tasks meter
            var criticalTasks = incompleteTasks.Where(t => GetEffectiveDueTime(t, userProfile) < now).ToList();
            if (criticalTasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Critical Tasks: {criticalTasks.Count} tasks overdue by user preferences");
                Console.ResetColor();
            }

            if (!tasksToDisplay.Any() && !result.UnscheduledTasks.Any())
            {
                Console.WriteLine("No tasks found in this list.");
                return;
            }

            var mode = service.UserProfile.ActiveUrgencyMode;
            var scheduledTasks = incompleteTasks.Where(t => t.ScheduledStartTime.HasValue).ToList();


            // Show scheduled/incomplete tasks (prettier format)
            if (scheduledTasks.Any())
            {
                Console.WriteLine("\nScheduled Tasks:");
                foreach (var task in scheduledTasks.OrderBy(t => t.ScheduledStartTime))
                {
                    var start = task.ScheduledStartTime?.ToString("HH:mm") ?? "--:--";
                    var end = task.ScheduledEndTime?.ToString("HH:mm") ?? "--:--";
                    var duration = task.EstimatedDuration.TotalHours.ToString("0.##");
                    var due = FormatDate(task.DueDate);
                    Console.WriteLine($"[ID: {task.DisplayId}] {start} - {end} | {task.Title} (Duration: {duration}h, Due: {due})");
                }
            }

            // Show unschedulable/overdue tasks (incomplete, no scheduled start)
            var unscheduledTasksToDisplay = result.UnscheduledTasks ?? new List<TaskItem>();
            if (unscheduledTasksToDisplay.Any())
            {
                Console.WriteLine("\nUnscheduled/Overdue Tasks:");
                foreach (var task in unscheduledTasksToDisplay.OrderBy(t => t.DueDate))
                {
                    var duration = task.EstimatedDuration.TotalHours.ToString("0.##");
                    var due = FormatDate(task.DueDate);
                    Console.WriteLine($"[ID: {task.DisplayId}] {task.Title} (Due: {due}, Duration: {duration}h)");
                }
            }

            // Show completed tasks at the bottom
            var completedTasks = tasksToDisplay.Where(t => t.IsCompleted).ToList();
            if (completedTasks.Any())
            {
                Console.WriteLine("\nCompleted Tasks:");
                foreach (var task in completedTasks.OrderByDescending(t => t.ScheduledEndTime ?? DateTime.MinValue))
                {
                    Console.WriteLine($"[ID: {task.DisplayId}] {task.Title} (Completed)");
                }
            }
        }

        // Helper to format dates as MM-dd or MM-dd-yyyy if not current year
        private string FormatDate(DateTime date)
        {
            var now = DateTime.Now;
            if (date.Year == now.Year)
                return date.ToString("MM-dd");
            else
                return date.ToString("MM-dd-yyyy");
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
