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
            var tasksToDisplay = service.GetPrioritizedTasks(Program.ActiveListId).ToList();

            // Show colored slack meter if in MultiAgent mode
            var userProfile = service.UserProfile;
            if (userProfile.ActiveUrgencyMode == UrgencyMode.MultiAgent)
            {
                var today = DateTime.Today;
                var workStart = today.Add(userProfile.WorkStartTime.ToTimeSpan());
                var workEnd = today.Add(userProfile.WorkEndTime.ToTimeSpan());
                var totalWorkTime = (workEnd - workStart).TotalHours;
                var tasksForToday = tasksToDisplay
                    .Where(t => t.ScheduledStartTime.HasValue && t.ScheduledEndTime.HasValue && t.ScheduledStartTime.Value.Date == today)
                    .ToList();
                var scheduledTimeToday = tasksForToday.Sum(t => t.EstimatedDuration.TotalHours);
                var slackTime = totalWorkTime - scheduledTimeToday;
                double slackRatio = scheduledTimeToday > 0 ? slackTime / scheduledTimeToday : 1.0;

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
                double busyFraction = scheduledTimeToday / (totalWorkTime > 0 ? totalWorkTime : 1);
                busyFraction = Math.Clamp(busyFraction, 0, 1);
                int busyBlocks = (int)(meterWidth * busyFraction);
                int slackBlocks = meterWidth - busyBlocks;
                string meter = new string('=', busyBlocks) + new string('-', slackBlocks);
                Console.WriteLine($"Today's Schedule Pressure: [{meter}] - {slackTime:F1} hours free");
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

    }
}
