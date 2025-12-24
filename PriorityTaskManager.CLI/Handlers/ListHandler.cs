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
        private readonly ITaskMetricsService _taskMetricsService;

        public ListHandler(ITaskMetricsService taskMetricsService)
        {
            _taskMetricsService = taskMetricsService;
        }

        /// <inheritdoc/>
        public void Execute(TaskManagerService service, string[] args)
        {
            if (service.GetActiveListId() == 0)
            {
                var generalList = service.GetListByName("General");
                if (generalList != null)
                    service.SetActiveListId(generalList.Id);
                else
                    service.SetActiveListId(1);
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
                .Where(t => t.ScheduledParts.Any() && !t.IsCompleted)
                .OrderBy(t => (t.DueDate - t.ScheduledParts.Min(p => p.StartTime)).Duration())
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
            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
                return;
            }
            var result = service.GetPrioritizedTasks(service.GetActiveListId());
            var tasksToDisplay = result.Tasks;
            var incompleteTasks = tasksToDisplay.Where(t => !t.IsCompleted).ToList();

            var userProfile = service.UserProfile;
            var events = service.GetAllEvents().ToList();
            var now = DateTime.Now;
            var targetDay = _taskMetricsService.FindTargetDayForSlackMeter(now, userProfile);
            var workStart = targetDay.Date.Add(userProfile.WorkStartTime.ToTimeSpan());
            var workEnd = targetDay.Date.Add(userProfile.WorkEndTime.ToTimeSpan());

            var eventsForDay = events
                    .Where(e => e.StartTime.Date == targetDay.Date)
                    .OrderBy(e => e.StartTime)
                    .ToList();

            var closestTask = FindClosestTaskToDueDate(incompleteTasks);
            if (closestTask != null && closestTask.ScheduledParts.Any())
            {
                var effectiveDueTime = GetEffectiveDueTime(closestTask, userProfile);
                var slack = _taskMetricsService.CalculateRealisticSlack(closestTask, userProfile);
                var slackPercentage = slack.TotalMinutes / closestTask.EstimatedDuration.TotalMinutes;

                // Calculate schedule pressure
                var totalWorkTime = (workEnd - workStart).TotalHours;
                var scheduledTime = incompleteTasks
                        .SelectMany(t => t.ScheduledParts.Where(p => p.StartTime.Date == targetDay.Date))
                        .Sum(p => p.Duration.TotalHours);

                var eventTime = eventsForDay.Sum(e => (e.EndTime - e.StartTime).TotalHours);

                // Adjust slackTime to reflect only hours free within the target day
                var slackTime = Math.Max(0, totalWorkTime - scheduledTime - eventTime);

                // --- Timeline and Task Letter Assignment ---
                var scheduledTasksForDay = incompleteTasks
                    .Where(t => t.ScheduledParts.Any(p => p.StartTime.Date == targetDay.Date))
                    .OrderBy(t => t.ScheduledParts.Min(p => p.StartTime))
                    .ToList();

                var taskLetterMapping = new Dictionary<int, char>();
                char currentLetter = 'A';
                foreach (var task in scheduledTasksForDay)
                {
                    taskLetterMapping[task.Id] = currentLetter++;
                }

                // --- Generate Timeline ---
                var totalWorkDuration = workEnd - workStart;
                int meterWidth = (int)(totalWorkDuration.TotalMinutes / 15);
                var timeline = new char[meterWidth];
                Array.Fill(timeline, ' ');

                for (int h = 0; h <= totalWorkDuration.TotalHours; h++)
                {
                    var hourTime = workStart.Date.Add(userProfile.WorkStartTime.ToTimeSpan()).AddHours(h);
                    if (hourTime <= workEnd)
                    {
                        var position = (int)((hourTime - workStart).TotalMinutes / 15);
                        if (position < meterWidth)
                        {
                            string hourString = hourTime.ToString("%h");
                            if (position + hourString.Length < meterWidth)
                            {
                                for (int i = 0; i < hourString.Length; i++)
                                {
                                    timeline[position + i] = hourString[i];
                                }
                            }
                        }
                    }
                }

                // Determine color
                ConsoleColor meterColor;
                if (slack.TotalMinutes < 0 || result.UnscheduledTasks.Any())
                    meterColor = ConsoleColor.Black;
                else if (slackPercentage > 0.25)
                    meterColor = ConsoleColor.Green;
                else if (slackPercentage > 0.10)
                    meterColor = ConsoleColor.Yellow;
                else
                    meterColor = ConsoleColor.Red;

                // --- Meter bar with task letters ---
                var taskBlocks = new char[meterWidth];
                Array.Fill(taskBlocks, ' ');

                // --- Add events to the meter bar ---
                foreach (var ev in eventsForDay)
                {
                    var startBlock = (int)((ev.StartTime - workStart).TotalMinutes / 15);
                    var durationInMinutes = (ev.EndTime - ev.StartTime).TotalMinutes;
                    var eventDurationInBlocks = (int)(durationInMinutes / 15);

                    if (startBlock < 0) // Event starts before work day starts
                    {
                        eventDurationInBlocks -= (0 - startBlock);
                        startBlock = 0;
                    }

                    for (int i = 0; i < eventDurationInBlocks && startBlock + i < meterWidth; i++)
                    {
                        if (startBlock + i >= 0)
                        {
                            taskBlocks[startBlock + i] = '■'; // Using a block character for events
                        }
                    }
                }

                var nowInSchedule = now > workStart && now < workEnd;
                int passedBlocks = 0;
                if (nowInSchedule)
                {
                    var minutesPassed = (now - workStart).TotalMinutes;
                    passedBlocks = (int)(minutesPassed / 15);
                }

                foreach (var task in scheduledTasksForDay)
                {
                    foreach (var chunk in task.ScheduledParts.Where(p => p.StartTime.Date == targetDay.Date))
                    {
                        var startBlock = (int)((chunk.StartTime - workStart).TotalMinutes / 15);
                        var chunkDurationInBlocks = (int)(chunk.Duration.TotalMinutes / 15);
                        if (startBlock < meterWidth)
                        {
                            char taskChar = taskLetterMapping[task.Id];
                            string representation = taskChar.ToString();
                            if (chunkDurationInBlocks > 1)
                            {
                                representation += new string('=', chunkDurationInBlocks - 1);
                            }
                            for (int i = 0; i < representation.Length && startBlock + i < meterWidth; i++)
                            {
                                // Only draw task if the block is not already taken by an event
                                if (taskBlocks[startBlock + i] == ' ')
                                {
                                    taskBlocks[startBlock + i] = representation[i];
                                }
                            }
                        }
                    }
                }

                // --- Display combined output ---
                string headerText = targetDay.Date == DateTime.Today.Date
                    ? "Today's Schedule:"
                    : $"{targetDay:dddd}'s Schedule:";
                
                Console.WriteLine(headerText);
                Console.WriteLine($"          [{new string(timeline)}]");

                if (now < workStart)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    Console.ForegroundColor = meterColor;
                }
                Console.Write($"          [");
                Console.ForegroundColor = meterColor; // Ensure meterColor is set for the bar content

                for (int i = 0; i < meterWidth; i++)
                {
                    if (i < passedBlocks)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("/");
                        Console.ForegroundColor = meterColor;
                    }
                    else
                    {
                        if (taskBlocks[i] == '■')
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta; // Color for events
                            Console.Write(taskBlocks[i]);
                            Console.ForegroundColor = meterColor; // Revert to meter color
                        }
                        else
                        {
                            Console.Write(taskBlocks[i]);
                        }
                    }
                }

                Console.WriteLine($"] {slackTime:F1} hours free");
                Console.ResetColor();

                // Calculate realistic slack
                var realisticSlack = _taskMetricsService.CalculateRealisticSlack(closestTask, userProfile);

                // Calculate actual slack
                var actualSlack = _taskMetricsService.CalculateActualSlack(closestTask);

                Console.ForegroundColor = meterColor;
                Console.WriteLine($"Task with least slack: '{closestTask.Title}'");
                Console.WriteLine($"Realistic Slack: {realisticSlack.Days} days {realisticSlack.Hours} hours {realisticSlack.Minutes} minutes");
                Console.WriteLine($"Actual Slack: {actualSlack.Days} days {actualSlack.Hours} hours {actualSlack.Minutes} minutes");
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
            var scheduledTasks = incompleteTasks.Where(t => t.ScheduledParts.Any()).ToList();

            // Show scheduled events
            if (eventsForDay.Any())
            {
                Console.WriteLine("\nScheduled Events:");
                foreach (var ev in eventsForDay)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"      {ev.StartTime:HH:mm} - {ev.EndTime:HH:mm} | {ev.Name}");
                    Console.ResetColor();
                }
            }


            // Show scheduled/incomplete tasks (prettier format)
            if (scheduledTasks.Any())
            {
                Console.WriteLine("\nScheduled Tasks:");
                var scheduledTasksForDay = scheduledTasks
                    .Where(t => t.ScheduledParts.Any(p => p.StartTime.Date == targetDay.Date))
                    .OrderBy(t => t.ScheduledParts.Min(p => p.StartTime))
                    .ToList();
                
                var taskLetterMapping = new Dictionary<int, char>();
                char currentLetter = 'A';
                foreach (var task in scheduledTasksForDay)
                {
                    taskLetterMapping[task.Id] = currentLetter++;
                }

                foreach (var task in scheduledTasks.OrderBy(t => t.ScheduledParts.Min(p => p.StartTime)))
                {
                    var letter = taskLetterMapping.ContainsKey(task.Id) ? $"({taskLetterMapping[task.Id]})" : "";

                    if (!string.IsNullOrEmpty(letter))
                    {
                        var realisticSlackForTask = _taskMetricsService.CalculateRealisticSlack(task, userProfile);
                        var slackPercentageForTask = task.EstimatedDuration.TotalMinutes > 0 ? realisticSlackForTask.TotalMinutes / task.EstimatedDuration.TotalMinutes : 0;

                        ConsoleColor taskColor;
                        if (realisticSlackForTask.TotalMinutes <= 0)
                            taskColor = ConsoleColor.Red;
                        else if (slackPercentageForTask > 0.25)
                            taskColor = ConsoleColor.Green;
                        else if (slackPercentageForTask > 0.10)
                            taskColor = ConsoleColor.Yellow;
                        else
                            taskColor = ConsoleColor.Red;

                        Console.ForegroundColor = taskColor;
                        Console.Write(letter);
                        Console.ResetColor();
                        Console.Write(" "); // Add a space after the colored letter
                    }

                    // Show all scheduled chunks for this task on the target day
                    var chunksForDay = task.ScheduledParts.Where(p => p.StartTime.Date == targetDay.Date).OrderBy(p => p.StartTime).ToList();
                    if (chunksForDay.Any())
                    {
                        foreach (var chunk in chunksForDay)
                        {
                            var start = chunk.StartTime.ToString("HH:mm");
                            var end = chunk.EndTime.ToString("HH:mm");
                            var duration = chunk.Duration.TotalHours.ToString("0.##");
                            var due = FormatDate(task.DueDate);
                            Console.WriteLine($"[ID: {task.DisplayId}] {start} - {end} | {task.Title} (Chunk: {duration}h, Due: {due})");
                        }
                    }
                    else
                    {
                        // Fallback if no chunk for today (shouldn't happen for scheduledTasksForDay)
                        var due = FormatDate(task.DueDate);
                        Console.WriteLine($"[ID: {task.DisplayId}] --:-- - --:-- | {task.Title} (Due: {due})");
                    }
                }
            }

            // Show unschedulable/overdue tasks (incomplete, no scheduled start)
            // Show all overdue tasks (scheduled and unscheduled)
            var unscheduledTasksToDisplay = result.UnscheduledTasks ?? new List<TaskItem>();
            var overdueTasksToDisplay = tasksToDisplay
                .Where(t => !t.IsCompleted && t.DueDate < DateTime.Now)
                .ToList();

            // Combine both sets, avoid duplicates
            var unscheduledAndOverdueTasks = unscheduledTasksToDisplay
                .Concat(overdueTasksToDisplay)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .OrderBy(t => t.DueDate)
                .ToList();

            if (unscheduledAndOverdueTasks.Any())
            {
                Console.WriteLine("\nUnscheduled/Overdue Tasks:");
                foreach (var task in unscheduledAndOverdueTasks)
                {
                    var duration = task.EstimatedDuration.TotalHours.ToString("0.##");
                    var due = FormatDate(task.DueDate);
                    var labels = new List<string>();
                    if (task.DueDate < DateTime.Now) labels.Add("OVERDUE");
                    if (unscheduledTasksToDisplay.Any(u => u.Id == task.Id)) labels.Add("UNSCHEDULED");
                    var labelText = labels.Count > 0 ? $"[{string.Join(", ", labels)}] " : "";
                    Console.ForegroundColor = labels.Contains("OVERDUE") ? ConsoleColor.Red : ConsoleColor.Yellow;
                    Console.WriteLine($"[ID: {task.DisplayId}] {labelText}{task.Title} (Due: {due}, Duration: {duration}h)");
                    Console.ResetColor();
                }
            }

            // Show completed tasks at the bottom
            var completedTasks = tasksToDisplay.Where(t => t.IsCompleted).ToList();
            if (completedTasks.Any())
            {
                Console.WriteLine("\nCompleted Tasks:");
                foreach (var task in completedTasks.OrderByDescending(t => t.ScheduledParts.Any() ? t.ScheduledParts.Max(p => p.EndTime) : DateTime.MinValue))
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
                var activeIndicator = list.Id == service.GetActiveListId() ? " (Active)" : string.Empty;
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

            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list ID '{service.GetActiveListId()}' does not exist.");
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
    }
}
