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
            Program.ActiveListName ??= "General"; // Default to 'General' if null

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
            var activeListName = Program.ActiveListName;
            if (activeListName == null)
            {
                Console.WriteLine("Error: No active list is set.");
                return;
            }

            var activeList = service.GetListByName(activeListName);

            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list '{activeListName}' does not exist.");
                return;
            }

            var tasks = service.GetAllTasks(activeListName).ToList();

            switch (activeList.SortOption)
            {
                case SortOption.Default:
                    service.CalculateUrgencyForAllTasks();
                    tasks = tasks.OrderByDescending(t => t.UrgencyScore).ToList();
                    break;
                case SortOption.Alphabetical:
                    tasks = tasks.OrderBy(t => t.Title).ToList();
                    break;
                case SortOption.DueDate:
                    tasks = tasks.OrderBy(t => t.DueDate).ToList();
                    break;
                case SortOption.Id:
                    tasks = tasks.OrderBy(t => t.Id).ToList();
                    break;
            }

            if (!tasks.Any())
            {
                Console.WriteLine("No tasks found in this list.");
                return;
            }

            foreach (var task in tasks)
            {
                var checkbox = task.IsCompleted ? "[x]" : "[ ]";
                Console.WriteLine($"{checkbox} Id: {task.Id}, Title: {task.Title}, Urgency: {task.UrgencyScore:F2}");
            }
        }

        private void HandleViewAllLists(TaskManagerService service)
        {
            var lists = service.GetAllLists();
            foreach (var list in lists)
            {
                var activeIndicator = list.Name.Equals(Program.ActiveListName, StringComparison.OrdinalIgnoreCase) ? " (Active)" : string.Empty;
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
                Program.ActiveListName = listName;
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

            if (listName.Equals(Program.ActiveListName, StringComparison.OrdinalIgnoreCase))
            {
                Program.ActiveListName = "General";
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

            var activeListName = Program.ActiveListName;
            var activeList = service.GetListByName(activeListName);

            if (activeList == null)
            {
                Console.WriteLine($"Error: Active list '{activeListName}' does not exist.");
                return;
            }

            activeList.SortOption = sortOption;
            service.UpdateList(activeList);
            Console.WriteLine($"Sort option for list '{activeListName}' updated to {sortOption}.");
        }

        private void HandleInteractiveSwitch(TaskManagerService service)
        {
            var lists = service.GetAllLists().ToList();
            if (!lists.Any())
            {
                Console.WriteLine("No lists available to switch.");
                return;
            }

            int selectedIndex = lists.FindIndex(l => l.Name.Equals(Program.ActiveListName, StringComparison.OrdinalIgnoreCase));
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
                        Program.ActiveListName = lists[selectedIndex].Name;
                        Console.SetCursorPosition(0, initialTop + lists.Count);
                        Console.WriteLine($"Switched to list '{Program.ActiveListName}'.");
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
