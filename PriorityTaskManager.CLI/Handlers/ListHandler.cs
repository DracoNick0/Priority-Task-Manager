using PriorityTaskManager.Services;
using PriorityTaskManager.CLI.Interfaces;
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

        public ListHandler(ITaskMetricsService taskMetricsService, ITimeService timeService)
        {
            _taskMetricsService = taskMetricsService;
            _timeService = timeService;
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
            var activeList = service.GetAllLists().FirstOrDefault(l => l.Id == service.GetActiveListId());
            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list with ID '{service.GetActiveListId()}' could not be found.");
                return;
            }

            var result = service.GetPrioritizedTasks(activeList.Id, _timeService);
            var incompleteTasks = result.Tasks.Where(t => !t.IsCompleted).ToList();

            var userProfile = service.UserProfile;
            var events = service.GetAllEvents().ToList();
            var now = _timeService.GetCurrentTime();
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
                var slackPercentage = closestTask.EstimatedDuration.TotalMinutes > 0 ? slack.TotalMinutes / closestTask.EstimatedDuration.TotalMinutes : 0;

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

                var eventLetterMapping = new Dictionary<int, char>();
                char currentEventLetter = 'a';
                foreach (var ev in eventsForDay)
                {
                    eventLetterMapping[ev.Id] = currentEventLetter++;
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

                // Calculate User's Average Daily Capacity
                var dailyWorkMinutes = (userProfile.WorkEndTime - userProfile.WorkStartTime).TotalMinutes;
                if (dailyWorkMinutes <= 0) dailyWorkMinutes = 8 * 60; // Fallback to 8h

                var closestTaskSlackMin = slack.TotalMinutes;
                var closestTaskDurMin = closestTask.EstimatedDuration.TotalMinutes;

                // Determine meter color (based on the task with least slack/highest pressure)
                ConsoleColor meterColor;
                if (closestTaskSlackMin < 0 || result.UnscheduledTasks.Any())
                {
                    meterColor = ConsoleColor.DarkGray; // Overdue or broken schedule
                }
                else if (closestTaskSlackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdDire, closestTaskDurMin / 4.0))
                {
                    meterColor = ConsoleColor.Red;
                }
                else if (closestTaskSlackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdPressing, closestTaskDurMin / 2.0))
                {
                    meterColor = ConsoleColor.DarkYellow;
                }
                else if (closestTaskSlackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdFocus, closestTaskDurMin))
                {
                    meterColor = ConsoleColor.Yellow;
                }
                else if (closestTaskSlackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdSafe, closestTaskDurMin * 3.0))
                {
                    meterColor = ConsoleColor.Green;
                }
                else
                {
                    meterColor = ConsoleColor.Cyan;
                }

                // Pre-calculate colors for tasks on the timeline
                var taskColorMap = new Dictionary<int, ConsoleColor>();
                
                foreach (var task in scheduledTasksForDay)
                {
                    var taskSlack = _taskMetricsService.CalculateRealisticSlack(task, userProfile);
                    var sMin = taskSlack.TotalMinutes;
                    var dMin = task.EstimatedDuration.TotalMinutes;
                    
                    if (sMin < 0) taskColorMap[task.Id] = ConsoleColor.DarkGray;
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdDire, dMin / 4.0)) taskColorMap[task.Id] = ConsoleColor.Red;
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdPressing, dMin / 2.0)) taskColorMap[task.Id] = ConsoleColor.DarkYellow;
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdFocus, dMin)) taskColorMap[task.Id] = ConsoleColor.Yellow;
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdSafe, dMin * 3.0)) taskColorMap[task.Id] = ConsoleColor.Green;
                    else taskColorMap[task.Id] = ConsoleColor.Cyan;
                }

                // --- Meter bar with task letters ---
                // --- Robust per-block ownership and transition detection ---
                var blockOwners = new string[meterWidth]; // "task:<id>", "event:<id>", or null
                var blockChars = new char[meterWidth];
                Array.Fill(blockChars, ' ');

                // Assign events to blocks
                foreach (var ev in eventsForDay)
                {
                    char eventChar = eventLetterMapping.ContainsKey(ev.Id) ? eventLetterMapping[ev.Id] : '?';
                    bool firstBlockOfEvent = false;
                    var eventStart = ev.StartTime;
                    var eventEnd = ev.EndTime;
                    for (int b = 0; b < meterWidth; b++)
                    {
                        var blockStart = workStart + TimeSpan.FromMinutes(b * 15);
                        var blockEnd = blockStart + TimeSpan.FromMinutes(15);
                        if (eventStart < blockEnd && eventEnd > blockStart)
                        {
                            blockOwners[b] = $"event:{ev.Id}";
                            if (!firstBlockOfEvent)
                            {
                                blockChars[b] = eventChar;
                                firstBlockOfEvent = true;
                            }
                            else
                            {
                                blockChars[b] = '=';
                            }
                        }
                    }
                }

                // Assign tasks to blocks (tasks take precedence over empty blocks, not over events)
                foreach (var task in scheduledTasksForDay)
                {
                    char taskChar = taskLetterMapping[task.Id];
                    bool firstBlockOfTaskSet = false;
                    foreach (var chunk in task.ScheduledParts.Where(p => p.StartTime.Date == targetDay.Date))
                    {
                        var chunkStart = chunk.StartTime;
                        var chunkEnd = chunk.StartTime + chunk.Duration;
                        bool firstBlockOfChunkSet = false;
                        for (int b = 0; b < meterWidth; b++)
                        {
                            var blockStart = workStart + TimeSpan.FromMinutes(b * 15);
                            var blockEnd = blockStart + TimeSpan.FromMinutes(15);
                            if (chunkStart < blockEnd && chunkEnd > blockStart)
                            {
                                // Only assign if not already an event
                                if (blockOwners[b] == null)
                                {
                                    blockOwners[b] = $"task:{task.Id}";
                                    if (!firstBlockOfTaskSet)
                                    {
                                        blockChars[b] = taskChar;
                                        firstBlockOfTaskSet = true;
                                        firstBlockOfChunkSet = true;
                                    }
                                    else if (!firstBlockOfChunkSet)
                                    {
                                        blockChars[b] = '=';
                                        firstBlockOfChunkSet = true;
                                    }
                                    else
                                    {
                                        blockChars[b] = '=';
                                    }
                                }
                            }
                        }
                    }
                }

                // Insert split marker '=' at transitions from task to event
                for (int b = 1; b < meterWidth; b++)
                {
                    if (blockOwners[b - 1]?.StartsWith("task:") == true && blockOwners[b]?.StartsWith("event:") == true)
                    {
                        // Only overwrite with '=' if not the first block of the chunk (i.e., not a letter)
                        if (blockChars[b - 1] == '=')
                            blockChars[b - 1] = '=';
                        // else leave the task letter as is
                    }
                }

                var nowInSchedule = now > workStart && now < workEnd;
                int passedBlocks = 0;
                if (nowInSchedule)
                {
                    var minutesPassed = (now - workStart).TotalMinutes;
                    passedBlocks = (int)(minutesPassed / 15);
                }

                // --- Display combined output ---
                string headerText = targetDay.Date == now.Date
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
                        if (blockOwners[i]?.StartsWith("event:") == true)
                        {
                            Console.ForegroundColor = ConsoleColor.Magenta; // Color for events
                            Console.Write(blockChars[i]);
                            Console.ForegroundColor = meterColor; // Revert to meter color
                        }
                        else if (blockOwners[i]?.StartsWith("task:") == true)
                        {
                            var parts = blockOwners[i].Split(':');
                            if (parts.Length > 1 && int.TryParse(parts[1], out int tId) && taskColorMap.ContainsKey(tId))
                            {
                                Console.ForegroundColor = taskColorMap[tId];
                                Console.Write(blockChars[i]);
                                Console.ForegroundColor = meterColor; // Revert to meter color
                            }
                            else
                            {
                                Console.Write(blockChars[i]);
                            }
                        }
                        else
                        {
                            Console.Write(blockChars[i]);
                        }
                    }
                }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"]");
                Console.ForegroundColor = meterColor;
                Console.WriteLine($" {slackTime:F1} hours free");
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

            var scheduledTasks = incompleteTasks.Where(t => t.ScheduledParts.Any()).ToList();

            // Show scheduled events
            if (eventsForDay.Any())
            {
                Console.WriteLine("\nScheduled Events:");
                
                var eventLetterMapping = new Dictionary<int, char>();
                char currentEventLetter = 'a';
                foreach (var ev in eventsForDay)
                {
                    eventLetterMapping[ev.Id] = currentEventLetter++;
                }

                foreach (var ev in eventsForDay)
                {
                    var letter = eventLetterMapping.ContainsKey(ev.Id) ? $"({eventLetterMapping[ev.Id]})" : "";
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"{letter,4} [ID: {ev.Id}] {ev.StartTime:HH:mm} - {ev.EndTime:HH:mm} | {ev.Name}");
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
                        
                        // Calculate User's Average Daily Capacity
                        var dailyWorkMinutes = (userProfile.WorkEndTime - userProfile.WorkStartTime).TotalMinutes;
                        if (dailyWorkMinutes <= 0) dailyWorkMinutes = 8 * 60; // Fallback to 8h

                        var slackMin = realisticSlackForTask.TotalMinutes;
                        var durMin = task.EstimatedDuration.TotalMinutes;

                        ConsoleColor taskColor;
                        if (slackMin < 0)
                        {
                            taskColor = ConsoleColor.DarkGray; // Overdue
                        }
                        else if (slackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdDire, durMin / 4.0))
                        {
                            taskColor = ConsoleColor.Red;
                        }
                        else if (slackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdPressing, durMin / 2.0))
                        {
                            taskColor = ConsoleColor.DarkYellow;
                        }
                        else if (slackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdFocus, durMin))
                        {
                            taskColor = ConsoleColor.Yellow;
                        }
                        else if (slackMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdSafe, durMin * 3.0))
                        {
                            taskColor = ConsoleColor.Green;
                        }
                        else
                        {
                            taskColor = ConsoleColor.Cyan;
                        }

                        Console.ForegroundColor = taskColor;
                        Console.Write(letter);
                        Console.ResetColor();
                        Console.Write(" "); // Add a space after the colored letter
                    }

                    // Show all scheduled chunks for this task on the target day
                    var chunksForDay = task.ScheduledParts.Where(p => p.StartTime.Date == targetDay.Date).OrderBy(p => p.StartTime).ToList();
                    var futureChunks = task.ScheduledParts.Where(p => p.StartTime.Date > targetDay.Date).OrderBy(p => p.StartTime).ToList();
                    
                    if (chunksForDay.Any())
                    {
                        for (int i = 0; i < chunksForDay.Count; i++)
                        {
                            var chunk = chunksForDay[i];
                            var start = chunk.StartTime.ToString("HH:mm");
                            var end = chunk.EndTime.ToString("HH:mm");
                            var duration = chunk.Duration.TotalHours.ToString("0.##");
                            
                            // Only show ID and Title for the first chunk to reduce noise
                            string idInfo = i == 0 ? $"[ID: {task.DisplayId}] " : $"{" ",-8}";
                            string titleInfo = i == 0 ? $"| {task.Title} (Due: {FormatDate(task.DueDate)})" : "";
                            
                            Console.WriteLine($"{idInfo}{start} - {end} {titleInfo} (Chunk: {duration}h)");
                        }

                        // Check for future chunks to provide context on where the rest of the task is
                        if (futureChunks.Any())
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            foreach (var chunk in futureChunks)
                            {
                                var dateStr = chunk.StartTime.ToString("MM-dd");
                                var start = chunk.StartTime.ToString("HH:mm");
                                var end = chunk.EndTime.ToString("HH:mm");
                                var duration = chunk.Duration.TotalHours.ToString("0.##");
                                
                                // Indent based on ID length: (digits + 2) spaces
                                int idDigits = task.DisplayId.ToString().Length;
                                string indent = new string(' ', idDigits + 2);
                                
                                Console.WriteLine($"{indent}-> {dateStr} {start} - {end} (Part: {duration}h)");
                            }
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        // Task is scheduled, but not for today
                        var nextChunk = task.ScheduledParts.OrderBy(p => p.StartTime).FirstOrDefault(p => p.StartTime > targetDay.Date);
                        var due = FormatDate(task.DueDate);
                        
                        string scheduleInfo;
                        if (nextChunk != null)
                        {
                            scheduleInfo = $"{nextChunk.StartTime:MM-dd HH:mm}";
                        }
                        else
                        {
                             // Should be impossible for 'scheduledTasks' list, but fallback safe
                            scheduleInfo = "--:--";
                        }
                        
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"[ID: {task.DisplayId}] {scheduleInfo,-11} | {task.Title} (Due: {due}) [Future]");
                        Console.ResetColor();
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

            // Show completed tasks at the bottom (Max 3)
            var completedTasks = tasksToDisplay.Where(t => t.IsCompleted).ToList();
            if (completedTasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"\nCompleted Tasks (Last 3 of {completedTasks.Count}):");
                
                foreach (var task in completedTasks.OrderByDescending(t => t.Id).Take(3))
                {
                    Console.WriteLine($"[ID: {task.DisplayId}] {task.Title} (Completed)");
                }
                Console.ResetColor();
            }
        }

        // Helper to format dates as MM-dd or MM-dd-yyyy if not current year
        private string FormatDate(DateTime? date)
        {
            if (!date.HasValue)
            {
                return "No date";
            }
            var now = DateTime.Now;
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
