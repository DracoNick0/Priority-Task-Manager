using System;
using System.Collections.Generic;
using System.Linq;
using PriorityTaskManager.Models;
using PriorityTaskManager.Services;

namespace PriorityTaskManager.CLI.Utils
{
    public static class ConsoleHelper
    {
        /// <summary>
        /// Clears the console and renders the current schedule dashboard from the cached snapshot.
        /// This wrapper ensures the schedule is always displayed at the top without rerunning the scheduler.
        /// </summary>
        public static void ClearAndRenderDashboard(ScheduleSnapshotProvider snapshotProvider, ITaskMetricsService taskMetricsService)
        {
            Console.Clear();

            if (snapshotProvider.TryGetLatestSnapshot(out var snapshot) && snapshot != null)
            {
                RenderSchedule(snapshot, taskMetricsService);
                Console.WriteLine("\n---");
            }
            else
            {
                Console.WriteLine("Warning: Schedule snapshot unavailable.");
            }
        }

        public static void RenderSchedule(ScheduleSnapshot snapshot, ITaskMetricsService taskMetricsService)
        {
            var result = snapshot.Result;
            var incompleteTasks = snapshot.IncompleteTasks;
            var userProfile = snapshot.UserProfile;
            var eventsForDay = snapshot.EventsForDay;
            var now = snapshot.Now;
            var targetDay = snapshot.TargetDay;
            var workStart = snapshot.WorkStart;
            var workEnd = snapshot.WorkEnd;

            var closestTask = FindClosestTaskToDueDate(incompleteTasks);
            if (closestTask != null && closestTask.ScheduledParts.Any())
            {
                var slack = taskMetricsService.CalculateRealisticSlack(closestTask, userProfile);

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

                var totalWorkDuration = workEnd - workStart;
                int meterWidth = (int)(totalWorkDuration.TotalMinutes / 15);
                if (meterWidth < 1)
                {
                    meterWidth = 1;
                }

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

                var dailyWorkMinutes = (userProfile.WorkEndTime - userProfile.WorkStartTime).TotalMinutes;
                if (dailyWorkMinutes <= 0)
                {
                    dailyWorkMinutes = 8 * 60;
                }

                var closestTaskSlackMin = slack.TotalMinutes;
                var closestTaskDurMin = closestTask.EstimatedDuration.TotalMinutes;

                ConsoleColor meterColor;
                if (closestTaskSlackMin < 0 || result.UnscheduledTasks.Any())
                {
                    meterColor = ConsoleColor.DarkGray;
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

                var taskColorMap = new Dictionary<int, ConsoleColor>();

                foreach (var task in scheduledTasksForDay)
                {
                    var taskSlack = taskMetricsService.CalculateRealisticSlack(task, userProfile);
                    var sMin = taskSlack.TotalMinutes;
                    var dMin = task.EstimatedDuration.TotalMinutes;

                    if (sMin < 0)
                    {
                        taskColorMap[task.Id] = ConsoleColor.DarkGray;
                    }
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdDire, dMin / 4.0))
                    {
                        taskColorMap[task.Id] = ConsoleColor.Red;
                    }
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdPressing, dMin / 2.0))
                    {
                        taskColorMap[task.Id] = ConsoleColor.DarkYellow;
                    }
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdFocus, dMin))
                    {
                        taskColorMap[task.Id] = ConsoleColor.Yellow;
                    }
                    else if (sMin <= Math.Max(dailyWorkMinutes * userProfile.SlackThresholdSafe, dMin * 3.0))
                    {
                        taskColorMap[task.Id] = ConsoleColor.Green;
                    }
                    else
                    {
                        taskColorMap[task.Id] = ConsoleColor.Cyan;
                    }
                }

                var blockOwners = new string[meterWidth];
                var blockChars = new char[meterWidth];
                Array.Fill(blockChars, ' ');

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

                var nowInSchedule = now > workStart && now < workEnd;
                int passedBlocks = 0;
                if (nowInSchedule)
                {
                    var minutesPassed = (now - workStart).TotalMinutes;
                    passedBlocks = (int)(minutesPassed / 15);
                }

                string headerText = targetDay.Date == now.Date
                    ? "Today's Schedule:"
                    : $"{targetDay:dddd}'s Schedule:";

                Console.WriteLine(headerText);
                Console.WriteLine($"          [{new string(timeline)}]");

                Console.ForegroundColor = now < workStart ? ConsoleColor.DarkGray : meterColor;
                Console.Write("          [");
                Console.ForegroundColor = meterColor;

                for (int i = 0; i < meterWidth; i++)
                {
                    if (i < passedBlocks)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("/");
                        Console.ForegroundColor = meterColor;
                    }
                    else if (blockOwners[i]?.StartsWith("event:") == true)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(blockChars[i]);
                        Console.ForegroundColor = meterColor;
                    }
                    else if (blockOwners[i]?.StartsWith("task:") == true)
                    {
                        var parts = blockOwners[i].Split(':');
                        if (parts.Length > 1 && int.TryParse(parts[1], out int tId) && taskColorMap.ContainsKey(tId))
                        {
                            Console.ForegroundColor = taskColorMap[tId];
                            Console.Write(blockChars[i]);
                            Console.ForegroundColor = meterColor;
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

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("]");
                Console.ResetColor();

                var realisticSlack = taskMetricsService.CalculateRealisticSlack(closestTask, userProfile);
                var actualSlack = taskMetricsService.CalculateActualSlack(closestTask);

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

            var criticalTasks = incompleteTasks.Where(t => GetEffectiveDueTime(t, userProfile) < now).ToList();
            if (criticalTasks.Any())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Critical Tasks ({criticalTasks.Count} tasks overdue by user preferences):");
                foreach (var task in criticalTasks)
                {
                    Console.WriteLine($"  [ID: {task.DisplayId}] {task.Title} (Due: {FormatDate(task.DueDate, now)})");
                }
                Console.ResetColor();
            }

            if (!result.Tasks.Any() && !result.UnscheduledTasks.Any())
            {
                Console.WriteLine("No tasks found in this list.");
                return;
            }

            var scheduledTasks = incompleteTasks.Where(t => t.ScheduledParts.Any()).ToList();

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
                        var realisticSlackForTask = taskMetricsService.CalculateRealisticSlack(task, userProfile);

                        var dailyWorkMinutes = (userProfile.WorkEndTime - userProfile.WorkStartTime).TotalMinutes;
                        if (dailyWorkMinutes <= 0)
                        {
                            dailyWorkMinutes = 8 * 60;
                        }

                        var slackMin = realisticSlackForTask.TotalMinutes;
                        var durMin = task.EstimatedDuration.TotalMinutes;

                        ConsoleColor taskColor;
                        if (slackMin < 0)
                        {
                            taskColor = ConsoleColor.DarkGray;
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
                        Console.Write(" ");
                    }

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

                            string idInfo = i == 0 ? $"[ID: {task.DisplayId}] " : $"{" ",-8}";
                            string titleInfo = i == 0 ? $"| {task.Title} (Due: {FormatDate(task.DueDate, now)})" : "";

                            Console.WriteLine($"{idInfo}{start} - {end} {titleInfo} (Chunk: {duration}h)");
                        }

                        if (futureChunks.Any())
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            foreach (var chunk in futureChunks)
                            {
                                var dateStr = chunk.StartTime.ToString("MM-dd");
                                var start = chunk.StartTime.ToString("HH:mm");
                                var end = chunk.EndTime.ToString("HH:mm");
                                var duration = chunk.Duration.TotalHours.ToString("0.##");

                                int idDigits = task.DisplayId.ToString().Length;
                                string indent = new string(' ', idDigits + 2);

                                Console.WriteLine($"{indent}-> {dateStr} {start} - {end} (Part: {duration}h)");
                            }
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        var nextChunk = task.ScheduledParts.OrderBy(p => p.StartTime).FirstOrDefault(p => p.StartTime > targetDay.Date);
                        var due = FormatDate(task.DueDate, now);

                        string scheduleInfo;
                        if (nextChunk != null)
                        {
                            scheduleInfo = $"{nextChunk.StartTime:MM-dd HH:mm}";
                        }
                        else
                        {
                            scheduleInfo = "--:--";
                        }

                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"[ID: {task.DisplayId}] {scheduleInfo,-11} | {task.Title} (Due: {due}) [Future]");
                        Console.ResetColor();
                    }
                }
            }

            var nowTime = now;
            var unscheduledTasksToDisplay = result.UnscheduledTasks ?? new List<TaskItem>();
            var scheduledTaskIds = scheduledTasks.Select(t => t.Id).ToHashSet();

            var unscheduledAndOverdueTasksQuery = unscheduledTasksToDisplay
                .Concat(result.Tasks.Where(t => !t.IsCompleted && t.DueDate < nowTime))
                .Where(t => !scheduledTaskIds.Contains(t.Id))
                .GroupBy(t => t.Id)
                .Select(g => g.First());

            var unscheduledAndOverdueTasks = snapshot.ActiveListSortOption switch
            {
                SortOption.Alphabetical => unscheduledAndOverdueTasksQuery.OrderBy(t => t.Title).ToList(),
                SortOption.Id => unscheduledAndOverdueTasksQuery.OrderBy(t => t.Id).ToList(),
                SortOption.DueDate => unscheduledAndOverdueTasksQuery.OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList(),
                _ => unscheduledAndOverdueTasksQuery.OrderBy(t => t.DueDate).ToList()
            };

            if (unscheduledAndOverdueTasks.Any())
            {
                Console.WriteLine("\nUnscheduled/Overdue Tasks:");
                foreach (var task in unscheduledAndOverdueTasks)
                {
                    var duration = task.EstimatedDuration.TotalHours.ToString("0.##");
                    var due = FormatDate(task.DueDate, now);
                    var labels = new List<string>();
                    if (task.DueDate < nowTime)
                    {
                        labels.Add("OVERDUE");
                    }
                    if (unscheduledTasksToDisplay.Any(u => u.Id == task.Id))
                    {
                        labels.Add("UNSCHEDULED");
                    }
                    var labelText = labels.Count > 0 ? $"[{string.Join(", ", labels)}] " : "";
                    Console.ForegroundColor = labels.Contains("OVERDUE") ? ConsoleColor.Red : ConsoleColor.Yellow;
                    Console.WriteLine($"[ID: {task.DisplayId}] {labelText}{task.Title} (Due: {due}, Duration: {duration}h)");
                    Console.ResetColor();
                }
            }
        }

        public static void DrawMenu(List<string> items, int selectedIndex, int startLine = -1)
        {
            if (startLine != -1)
            {
                Console.SetCursorPosition(0, startLine);
            }
            for (int i = 0; i < items.Count; i++)
            {
                // Clear the line
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.CursorLeft = 0;

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
            // Hint for Shift+Enter shortcut
            Console.WriteLine("\n[Use Arrows to Navigate, Enter to Select, Shift+Enter to Save/Confirm]");
        }

        private static TaskItem? FindClosestTaskToDueDate(IEnumerable<TaskItem> tasks)
        {
            return tasks
                .Where(t => t.DueDate.HasValue && t.ScheduledParts.Any() && !t.IsCompleted)
                .OrderBy(t => (t.DueDate!.Value - t.ScheduledParts.Min(p => p.StartTime)).Duration())
                .FirstOrDefault();
        }

        private static DateTime GetEffectiveDueTime(TaskItem task, UserProfile userProfile)
        {
            if (task.DueDate.HasValue)
            {
                var dueDate = task.DueDate.Value;
                var workEnd = dueDate.Date.Add(userProfile.WorkEndTime.ToTimeSpan());
                return dueDate < workEnd ? dueDate : workEnd;
            }
            return DateTime.MaxValue;
        }

        private static string FormatDate(DateTime? date, DateTime now)
        {
            if (!date.HasValue)
            {
                return "No date";
            }

            if (date.Value.Year == now.Year)
            {
                return date.Value.ToString("MM-dd");
            }

            return date.Value.ToString("MM-dd-yyyy");
        }
    }
}
